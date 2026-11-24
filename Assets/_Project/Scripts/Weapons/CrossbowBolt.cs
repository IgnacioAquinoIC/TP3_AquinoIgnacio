using UnityEngine;
namespace ContainerShip
{
    /// <summary>Physical explosive bolt (Rigidbody + Collider). Flies ballistically, then on impact:
    ///  - deals direct damage to whatever it hit (HitZone multipliers apply, so headshots count),
    ///  - sticks into it: static geometry = frozen in place, enemies/cameras = parented to them (moves with them),
    ///    free physics props = welded with a FixedJoint (so it is carried by the prop),
    ///  - arms a blinking fuse and detonates with exactly the frag-grenade blast.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CrossbowBolt : MonoBehaviour
    {
        public Renderer indicator;
        public Light indicatorLight;
        [Tooltip("Total length of the bolt along its local Z axis (used to embed the tip).")]
        public float length = 0.3f;

        CrossbowData _d; GameObject _owner;
        Rigidbody _rb; Collider _col;
        Vector3 _lastVel, _hitNormal = Vector3.up;
        bool _impacted, _stuck, _detonated;
        float _age, _fuse, _blinkT; bool _blinkOn;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();
            SetIndicator(false);
        }

        public void Launch(Vector3 velocity, CrossbowData data, GameObject owner)
        {
            _d = data; _owner = owner;
            _rb.freezeRotation = true;
            _rb.linearVelocity = velocity;
            _lastVel = velocity;
            if (velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        void FixedUpdate()
        {
            if (_impacted) return;
            // Velocity BEFORE the physics step resolves a collision: gives us the true impact direction/speed.
            _lastVel = _rb.linearVelocity;
            if (_lastVel.sqrMagnitude > 1f) _rb.rotation = Quaternion.LookRotation(_lastVel); // nose follows the arc
        }

        void Update()
        {
            if (_d == null || _detonated) return;
            _age += Time.deltaTime;
            if (!_impacted)
            {
                if (_age > _d.maxFlightTime || transform.position.y < -100f) Destroy(gameObject);
                return;
            }
            _fuse -= Time.deltaTime;
            Blink(Time.deltaTime);
            if (_fuse <= 0f) Detonate();
        }

        void OnCollisionEnter(Collision c)
        {
            if (_impacted || _d == null) return;
            _impacted = true;

            ContactPoint cp = c.GetContact(0);
            Vector3 dir = _lastVel.sqrMagnitude > 0.01f ? _lastVel.normalized : transform.forward;
            float speed = _lastVel.magnitude;
            _hitNormal = cp.normal;
            if (Vector3.Dot(_hitNormal, dir) > 0f) _hitNormal = -_hitNormal; // always face away from the surface

            // Direct hit damage. GetComponentInParent finds a HitZone first (head), else the body Health.
            var dmg = c.collider.GetComponentInParent<IDamageable>();
            bool flesh = c.collider.GetComponentInParent<EnemyAI>() != null;
            if (dmg != null)
                dmg.TakeDamage(new DamageInfo { amount = _d.damage, point = cp.point, direction = dir, source = _owner, type = DamageType.Bullet });
            Effects.Impact(cp.point, _hitNormal, flesh);
            NoiseSystem.Emit(cp.point, _d.stickNoiseRadius, NoiseType.Impact, gameObject);

            _fuse = _d.fuseTime;
            SetIndicator(true);

            if (speed >= _d.stickMinSpeed) Stick(c, cp, dir);
            else _rb.freezeRotation = false; // too slow to embed: let it tumble and settle physically
        }

        void Stick(Collision c, ContactPoint cp, Vector3 dir)
        {
            _stuck = true;
            Rigidbody other = c.rigidbody;
            var health = c.collider.GetComponentInParent<Health>();

            _rb.linearVelocity = Vector3.zero; _rb.angularVelocity = Vector3.zero;
            _rb.interpolation = RigidbodyInterpolation.None;
            _rb.useGravity = false;
            _col.isTrigger = true; // embedded bolt must not block bullets, LOS rays, movement or other bolts

            Vector3 pos = cp.point - dir * (length * 0.5f - _d.embedDepth);
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.SetPositionAndRotation(pos, rot);

            if (other != null && !other.isKinematic)
            {
                // Free physics prop: weld to it so the bolt is carried along by the prop.
                var joint = gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = other;
            }
            else
            {
                _rb.isKinematic = true;
                if (health != null) AttachTo(health.transform);          // enemies, cameras, destructibles
                else if (other != null) AttachTo(other.transform);       // kinematic movers
                // else: static world geometry, stays frozen where it embedded
            }
        }

        void AttachTo(Transform parent)
        {
            transform.SetParent(parent, true);
            Vector3 s = parent.lossyScale; // cancel parent scale so the bolt is never stretched
            transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f / Mathf.Max(0.0001f, s.z));
        }

        void Blink(float dt)
        {
            float k = Mathf.Clamp01(_fuse / Mathf.Max(0.01f, _d.fuseTime)); // 1 = just armed, 0 = about to blow
            float period = Mathf.Lerp(0.07f, 0.28f, k);
            _blinkT += dt;
            if (_blinkT < period) return;
            _blinkT = 0f; _blinkOn = !_blinkOn;
            SetIndicator(_blinkOn);
            if (_blinkOn) SoundFX.Play3D(SoundFX.Beep, transform.position, 0.5f, 20f, Mathf.Lerp(1.6f, 1f, k));
        }

        void SetIndicator(bool on)
        {
            if (indicator != null) indicator.enabled = on;
            if (indicatorLight != null) indicatorLight.enabled = on;
        }

        void Detonate()
        {
            _detonated = true;
            Vector3 pos = transform.position + _hitNormal * 0.2f; // pull the blast origin out of the surface it is stuck in
            Grenade.FragBlast(_d.blast, pos, _owner, gameObject);
            Destroy(gameObject);
        }
    }
}
