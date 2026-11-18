using UnityEngine;
namespace ContainerShip
{
    /// <summary>Put on a child collider (e.g. head) to scale damage forwarded to the parent Health.</summary>
    public class HitZone : MonoBehaviour, IDamageable
    {
        public Health health;
        public float multiplier = 2.5f;
        public void TakeDamage(DamageInfo info)
        {
            if (health == null) health = GetComponentInParent<Health>();
            if (health == null) return;
            info.amount *= multiplier;
            health.TakeDamage(info);
        }
    }
}
