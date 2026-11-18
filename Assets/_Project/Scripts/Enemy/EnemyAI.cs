using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace ContainerShip
{
    public enum EnemyState { Patrolling, Suspicious, Alerted, Pursuing, Attacking, Returning, Stunned, Dead }

    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(EnemyPerception))]
    public class EnemyAI : MonoBehaviour
    {
        public static readonly List<EnemyAI> All = new List<EnemyAI>();

        public EnemyStats stats;
        public Transform[] patrolPoints;
        public Transform eyes;
        public Transform muzzle;
        public float patrolWaitTime = 2.5f;

        public EnemyState State { get; private set; } = EnemyState.Patrolling;
        public bool IsDead => State == EnemyState.Dead;
        public Vector3 EyePosition => eyes != null ? eyes.position : transform.position + Vector3.up * 1.6f;

        NavMeshAgent _agent; Health _health; EnemyPerception _perception;
        float _awareness, _stateAge, _wait, _stun, _lostTimer, _reaction, _fireTimer, _repath, _stepAcc;
        Vector3 _investigate, _lastKnown, _home;
        bool _hasLastKnown;
        int _patrolIndex;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _perception = GetComponent<EnemyPerception>();
            if (_perception.stats == null) _perception.stats = stats;
            if (_perception.eyes == null) _perception.eyes = eyes;
            _health.SetMax(stats.maxHealth);
            _agent.stoppingDistance = 0.3f; _agent.angularSpeed = 280f; _agent.acceleration = 10f;
            _home = transform.position;
        }

        void OnEnable()
        {
            All.Add(this);
            NoiseSystem.NoiseEmitted += OnNoise;
            GetComponent<Health>().Damaged += OnDamaged;
            GetComponent<Health>().Died += OnDied;
        }

        void OnDisable()
        {
            All.Remove(this);
            NoiseSystem.NoiseEmitted -= OnNoise;
            var h = GetComponent<Health>();
            if (h != null) { h.Damaged -= OnDamaged; h.Died -= OnDied; }
        }

        void Start() { SetState(EnemyState.Patrolling); }

        // ---------------- helpers ----------------
        bool Arrived => _agent.enabled && _agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f;

        bool MoveTo(Vector3 p)
        {
            if (!_agent.enabled || !_agent.isOnNavMesh) return false;
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
                return true;
            }
            return false;
        }

        void StopAgent() { if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); } }

        Vector3 PlayerPos => PlayerController.Instance != null ? PlayerController.Instance.transform.position : transform.position;

        int ClosestPatrolIndex()
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                float d = (patrolPoints[i].position - transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        // ---------------- state machine ----------------
        void SetState(EnemyState s)
        {
            if (State == EnemyState.Attacking) _agent.updateRotation = true;
            State = s; _stateAge = 0f; _wait = 0f; _lostTimer = 0f;
            switch (s)
            {
                case EnemyState.Patrolling:
                    _agent.speed = stats.walkSpeed;
                    if (patrolPoints != null && patrolPoints.Length > 0) MoveTo(patrolPoints[_patrolIndex].position);
                    break;
                case EnemyState.Suspicious:
                    _agent.speed = stats.walkSpeed * 1.2f; MoveTo(_investigate); break;
                case EnemyState.Alerted:
                    _agent.speed = stats.searchSpeed; MoveTo(_investigate); break;
                case EnemyState.Pursuing:
                    _agent.speed = stats.chaseSpeed; _repath = 0f; MoveTo(_lastKnown);
                    GameEvents.RaiseDetected("Enemy");
                    AlertOthers(); break;
                case EnemyState.Attacking:
                    StopAgent(); _agent.updateRotation = false; _reaction = stats.reactionTime; _fireTimer = 0f; break;
                case EnemyState.Returning:
                    _agent.speed = stats.walkSpeed;
                    if (patrolPoints != null && patrolPoints.Length > 0) { _patrolIndex = ClosestPatrolIndex(); MoveTo(patrolPoints[_patrolIndex].position); }
                    else MoveTo(_home);
                    break;
                case EnemyState.Stunned:
                    StopAgent(); break;
                case EnemyState.Dead:
                    if (_agent.enabled) _agent.enabled = false; break;
            }
        }

        void Update()
        {
            if (State == EnemyState.Dead) return;
            float dt = Time.deltaTime;
            _stateAge += dt;

            if (State == EnemyState.Stunned)
            {
                _stun -= dt;
                if (_stun <= 0f)
                {
                    _awareness = 0.6f;
                    _investigate = _hasLastKnown ? _lastKnown : transform.position + transform.forward * 2f;
                    SetState(EnemyState.Alerted);
                }
                return;
            }

            _perception.Tick();
            UpdateAwareness(dt);

            switch (State)
            {
                case EnemyState.Patrolling: TickPatrol(dt); break;
                case EnemyState.Suspicious: TickSuspicious(dt); break;
                case EnemyState.Alerted: TickAlerted(dt); break;
                case EnemyState.Pursuing: TickPursuing(dt); break;
                case EnemyState.Attacking: TickAttacking(dt); break;
                case EnemyState.Returning: TickReturning(dt); break;
            }
            TickFootsteps(dt);
        }

        void UpdateAwareness(float dt)
        {
            if (_perception.CanSeePlayer)
            {
                _lastKnown = _perception.LastSeenPos; _hasLastKnown = true;
                _awareness = Mathf.Min(1f, _awareness + stats.awarenessGainPerSecond * _perception.Visibility * dt);
                _lostTimer = 0f;
            }
            else if (State != EnemyState.Pursuing && State != EnemyState.Attacking)
                _awareness = Mathf.Max(0f, _awareness - stats.awarenessDecayPerSecond * dt);

            if (State == EnemyState.Pursuing || State == EnemyState.Attacking) return;

            if (_awareness >= 1f) SetState(EnemyState.Pursuing);
            else if (_awareness >= 0.5f && (State == EnemyState.Patrolling || State == EnemyState.Suspicious || State == EnemyState.Returning))
            { _investigate = _lastKnown; SetState(EnemyState.Alerted); }
            else if (_awareness >= 0.2f && (State == EnemyState.Patrolling || State == EnemyState.Returning) && _perception.CanSeePlayer)
            { _investigate = _lastKnown; SetState(EnemyState.Suspicious); }
        }

        void TickPatrol(float dt)
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            if (!Arrived) return;
            _wait += dt;
            if (_wait >= patrolWaitTime)
            {
                _wait = 0f;
                _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
                MoveTo(patrolPoints[_patrolIndex].position);
            }
        }

        void TickSuspicious(float dt)
        {
            if (Arrived)
            {
                _wait += dt;
                transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * 70f * dt, 0f);
                if (_wait >= stats.suspiciousLookTime) { _awareness = Mathf.Min(_awareness, 0.15f); SetState(EnemyState.Returning); }
            }
            if (_stateAge > 15f) SetState(EnemyState.Returning);
        }

        void TickAlerted(float dt)
        {
            if (Arrived)
            {
                _wait += dt;
                transform.Rotate(0f, Mathf.Sin(Time.time * 3f) * 90f * dt, 0f);
                if (_wait > 1.5f)
                {
                    _wait = 0f;
                    Vector3 center = _hasLastKnown ? _lastKnown : _investigate;
                    Vector3 r = Random.insideUnitSphere * stats.searchRadius; r.y = 0f;
                    MoveTo(center + r);
                }
            }
            if (_stateAge > stats.searchDuration) { _awareness = 0.1f; SetState(EnemyState.Returning); }
        }

        void TickPursuing(float dt)
        {
            _repath -= dt;
            if (_perception.CanSeePlayer)
            {
                if (_repath <= 0f) { MoveTo(_lastKnown); _repath = 0.25f; }
                float dist = Vector3.Distance(transform.position, PlayerPos);
                if (dist <= stats.attackRange * 0.9f) SetState(EnemyState.Attacking);
            }
            else
            {
                _lostTimer += dt;
                if (_lostTimer > stats.loseTrackTime || (Arrived && _lostTimer > 1.5f))
                { _awareness = 0.55f; _investigate = _lastKnown; SetState(EnemyState.Alerted); }
            }
        }

        void TickAttacking(float dt)
        {
            if (!_perception.CanSeePlayer)
            {
                _lostTimer += dt;
                if (_lostTimer > 0.6f) SetState(EnemyState.Pursuing);
                return;
            }
            _lostTimer = 0f;
            Vector3 toP = PlayerPos - transform.position;
            if (toP.magnitude > stats.attackRange * 1.15f) { SetState(EnemyState.Pursuing); return; }
            toP.y = 0f;
            if (toP.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toP), stats.turnSpeed * dt);

            _reaction -= dt;
            if (_reaction > 0f) return;
            _fireTimer -= dt;
            if (_fireTimer <= 0f) { Fire(); _fireTimer = stats.fireInterval; }
        }

        void TickReturning(float dt)
        {
            if (Arrived || _stateAge > 25f) SetState(EnemyState.Patrolling);
        }

        void TickFootsteps(float dt)
        {
            if (!_agent.enabled || _agent.velocity.sqrMagnitude < 0.3f) return;
            _stepAcc += dt * _agent.velocity.magnitude;
            if (_stepAcc > 1.6f)
            {
                _stepAcc = 0f;
                SoundFX.Play3D(SoundFX.Step, transform.position, State == EnemyState.Pursuing ? 0.8f : 0.45f, 25f, Random.Range(0.85f, 1.05f));
            }
        }

        // ---------------- combat ----------------
        void Fire()
        {
            var p = PlayerController.Instance; if (p == null) return;
            Vector3 origin = muzzle != null ? muzzle.position : EyePosition;
            Vector3 target = p.transform.position + Vector3.up * Random.Range(0.8f, 1.4f);
            Vector3 dir = (target - origin).normalized;
            float dist = Vector3.Distance(origin, target);
            float acc = Mathf.Lerp(stats.accuracy, stats.accuracy * 0.35f, dist / stats.attackRange);
            if (p.IsSprinting) acc *= 0.7f;
            if (Random.value > acc) dir = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0f) * dir;

            float maxD = stats.attackRange * 1.5f;
            Vector3 end = origin + dir * maxD;
            if (Ballistics.Raycast(origin, dir, maxD, transform, out RaycastHit hit))
            {
                end = hit.point;
                var d = hit.collider.GetComponentInParent<IDamageable>();
                if (d != null) d.TakeDamage(new DamageInfo { amount = stats.damage, point = hit.point, direction = dir, source = gameObject, type = DamageType.Bullet });
                Effects.Impact(hit.point, hit.normal, d != null);
            }
            Effects.Tracer(origin, end, new Color(1f, 0.85f, 0.4f, 0.8f));
            Effects.FlashLight(origin, new Color(1f, 0.7f, 0.3f), 2f, 5f, 0.05f);
            SoundFX.Play3D(SoundFX.EnemyShot, origin, 1f, 70f);
            NoiseSystem.Emit(origin, stats.gunshotNoiseRadius, NoiseType.Gunshot, gameObject);
        }

        void AlertOthers()
        {
            foreach (var e in All)
            {
                if (e == this || e.IsDead) continue;
                if (Vector3.Distance(e.transform.position, transform.position) > stats.alertShareRadius) continue;
                e.Investigate(_hasLastKnown ? _lastKnown : transform.position, true);
            }
        }

        // ---------------- external stimuli ----------------
        /// <summary>Used by cameras, noise and allies.</summary>
        public void Investigate(Vector3 pos, bool alert)
        {
            if (IsDead || State == EnemyState.Stunned) return;
            if (State == EnemyState.Pursuing || State == EnemyState.Attacking) { if (alert) { _lastKnown = pos; _hasLastKnown = true; } return; }
            _investigate = pos; _lastKnown = pos; _hasLastKnown = true;
            if (alert) { _awareness = Mathf.Max(_awareness, 0.55f); SetState(EnemyState.Alerted); }
            else
            {
                _awareness = Mathf.Max(_awareness, 0.25f);
                if (State == EnemyState.Alerted) MoveTo(pos); else SetState(EnemyState.Suspicious);
            }
        }

        public void Flash(float seconds)
        {
            if (IsDead) return;
            _stun = Mathf.Max(_stun, seconds * stats.flashSusceptibility);
            if (State != EnemyState.Stunned) SetState(EnemyState.Stunned);
        }

        void OnNoise(Vector3 pos, float radius, NoiseType type, GameObject source)
        {
            if (IsDead || State == EnemyState.Stunned || source == gameObject) return;
            float dist = Vector3.Distance(transform.position, pos);
            float eff = radius * stats.hearingMultiplier;
            if (dist > eff) return;
            if (!Ballistics.HasLineOfSight(EyePosition, pos + Vector3.up, transform) && dist > eff * 0.55f) return; // muffled by walls

            bool loud = type == NoiseType.Gunshot || type == NoiseType.Explosion;
            bool fromPlayer = PlayerController.Instance != null && source == PlayerController.Instance.gameObject;
            if (State == EnemyState.Pursuing || State == EnemyState.Attacking) { if (fromPlayer) _lastKnown = pos; return; }
            Investigate(pos, loud);
        }

        void OnDamaged(DamageInfo info)
        {
            if (IsDead || State == EnemyState.Stunned) return;
            var p = PlayerController.Instance;
            if (p == null || info.source != p.gameObject) return;
            if (info.type == DamageType.Explosion) { Investigate(info.point, true); return; }

            _lastKnown = p.transform.position; _hasLastKnown = true;
            if (_perception.CanSeePlayer)
            {
                _awareness = 1f;
                if (State != EnemyState.Pursuing && State != EnemyState.Attacking) SetState(EnemyState.Pursuing);
            }
            else if (State != EnemyState.Pursuing && State != EnemyState.Attacking)
            {
                _awareness = Mathf.Max(_awareness, 0.6f);
                _investigate = _lastKnown + Random.insideUnitSphere * 3f;
                SetState(EnemyState.Alerted);
            }
        }

        void OnDied(DamageInfo info)
        {
            SetState(EnemyState.Dead);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            transform.Rotate(88f, 0f, 0f);
            transform.position += Vector3.up * 0.15f;
            SoundFX.Play3D(SoundFX.Step, transform.position, 1f, 25f, 0.6f);
        }

        void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.yellow;
            Vector3 o = eyes != null ? eyes.position : transform.position + Vector3.up * 1.6f;
            float h = stats.viewAngle * 0.5f;
            Gizmos.DrawRay(o, Quaternion.Euler(0, h, 0) * transform.forward * stats.viewRange);
            Gizmos.DrawRay(o, Quaternion.Euler(0, -h, 0) * transform.forward * stats.viewRange);
            if (patrolPoints != null)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolPoints.Length; i++)
                    if (patrolPoints[i] != null) Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
            }
        }
    }
}
