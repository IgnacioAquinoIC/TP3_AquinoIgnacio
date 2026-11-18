using UnityEngine;
namespace ContainerShip
{
    public enum DamageType { Bullet, Explosion, Other }

    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public GameObject source;
        public DamageType type;
    }

    public interface IDamageable { void TakeDamage(DamageInfo info); }
}
