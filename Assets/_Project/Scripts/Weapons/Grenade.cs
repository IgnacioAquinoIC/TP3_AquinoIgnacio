using System.Collections.Generic;
using UnityEngine;
namespace ContainerShip
{
    /// <summary>Physical grenade (Rigidbody + collider). Frag: line-of-sight area damage. Flash: distance/LOS/facing based stun.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Grenade : MonoBehaviour
    {
        public GrenadeData data;
        Rigidbody _rb; float _timer; GameObject _thrower; bool _exploded;

        void Awake() { _rb = GetComponent<Rigidbody>(); _timer = data != null ? data.fuseTime : 3f; }

        public void Launch(Vector3 velocity, GameObject thrower)
        {
            _thrower = thrower;
            _timer = data.fuseTime;
            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Random.insideUnitSphere * 8f;
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f && !_exploded) Explode();
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.relativeVelocity.magnitude < 1.5f) return;
            SoundFX.Play3D(SoundFX.GrenadeBounce, transform.position, 0.8f, 30f);
            NoiseSystem.Emit(transform.position, data.bounceNoiseRadius, NoiseType.Impact, gameObject);
        }

        void Explode()
        {
            _exploded = true;
            if (data.type == GrenadeType.Fragmentation) Frag(); else Flash();
            Destroy(gameObject);
        }

        void Frag() { FragBlast(data, transform.position + Vector3.up * 0.1f, _thrower, gameObject); }

        /// <summary>Fragmentation blast: line-of-sight area damage + physics force. Shared by frag grenades and crossbow bolts.</summary>
        public static void FragBlast(GrenadeData data, Vector3 pos, GameObject source, GameObject self)
        {
            Transform selfT = self != null ? self.transform : null;
            Effects.Explosion(pos, data.radius);
            SoundFX.Play3D(SoundFX.Explosion, pos, 1f, 90f);
            NoiseSystem.Emit(pos, data.detonationNoiseRadius, NoiseType.Explosion, self);

            var cols = Physics.OverlapSphere(pos, data.radius, ~0, QueryTriggerInteraction.Ignore);
            var done = new HashSet<Health>();
            foreach (var c in cols)
            {
                if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic)
                    c.attachedRigidbody.AddExplosionForce(data.explosionForce, pos, data.radius, 0.5f);

                var h = c.GetComponentInParent<Health>();
                if (h == null || h.IsDead || done.Contains(h)) continue;
                Vector3 p = c.ClosestPoint(pos);
                if (!Ballistics.HasLineOfSight(pos, p, h.transform, selfT)) continue; // cover blocks the blast
                float d = Vector3.Distance(pos, p);
                float dmg = d <= data.lethalRadius
                    ? data.maxDamage
                    : Mathf.Lerp(data.maxDamage, data.maxDamage * 0.1f, (d - data.lethalRadius) / Mathf.Max(0.01f, data.radius - data.lethalRadius));
                h.TakeDamage(new DamageInfo { amount = dmg, point = pos, direction = (p - pos).normalized, source = source, type = DamageType.Explosion });
                done.Add(h);
            }
        }

        void Flash()
        {
            Vector3 pos = transform.position + Vector3.up * 0.1f;
            Effects.FlashBang(pos);
            SoundFX.Play3D(SoundFX.Flashbang, pos, 1f, 90f);
            NoiseSystem.Emit(pos, data.detonationNoiseRadius, NoiseType.Explosion, gameObject);

            foreach (var e in EnemyAI.All.ToArray())
            {
                if (e == null || e.IsDead) continue;
                Vector3 eye = e.EyePosition;
                float d = Vector3.Distance(pos, eye);
                if (d > data.radius) continue;
                if (!Ballistics.HasLineOfSight(pos, eye, e.transform, transform)) continue;
                float distFactor = Mathf.Lerp(1f, 0.25f, d / data.radius);
                Vector3 toGrenade = (pos - eye).normalized;
                float facing = (Vector3.Dot(e.transform.forward, toGrenade) + 1f) * 0.5f; // 1 = looking at it
                float dur = data.flashMaxDuration * distFactor * Mathf.Lerp(0.35f, 1f, facing);
                if (dur > 0.3f) e.Flash(dur);
            }

            var p = PlayerController.Instance;
            if (p != null)
            {
                Vector3 eye = p.playerCamera.transform.position;
                float d = Vector3.Distance(pos, eye);
                if (d < data.radius * 0.7f && Ballistics.HasLineOfSight(pos, eye, p.transform, transform))
                {
                    float facing = (Vector3.Dot(p.playerCamera.transform.forward, (pos - eye).normalized) + 1f) * 0.5f;
                    GameEvents.RaiseFlashed(Mathf.Clamp01((1f - d / (data.radius * 0.7f)) * Mathf.Lerp(0.3f, 1f, facing)));
                }
            }
        }
    }
}
