using System;
using UnityEngine;
namespace ContainerShip
{
    /// <summary>Shared health for player, enemies, cameras and destructible props.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        bool _init;

        void Awake() { if (!_init) { Current = maxHealth; _init = true; } }

        public void SetMax(float max) { maxHealth = max; Current = max; IsDead = false; _init = true; }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || info.amount <= 0f) return;
            if (!_init) { Current = maxHealth; _init = true; }
            Current = Mathf.Max(0f, Current - info.amount);
            Damaged?.Invoke(info);
            if (Current <= 0f) { IsDead = true; Died?.Invoke(info); }
        }

        public void Heal(float amount) { if (!IsDead) Current = Mathf.Min(maxHealth, Current + amount); }
    }
}
