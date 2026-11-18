using UnityEngine;
namespace ContainerShip
{
    public enum GrenadeType { Fragmentation, Flashbang }

    [CreateAssetMenu(menuName = "Container Ship/Grenade Data", fileName = "GrenadeData")]
    public class GrenadeData : ScriptableObject
    {
        public string displayName = "Grenade";
        public GrenadeType type = GrenadeType.Fragmentation;
        public int startingCount = 2;
        public float fuseTime = 3.5f;
        public float throwForce = 11f;
        [Header("Blast")]
        public float radius = 9f;
        [Tooltip("Frag only: full damage inside this radius.")]
        public float lethalRadius = 4.5f;
        public float maxDamage = 160f;
        public float explosionForce = 600f;
        [Header("Flashbang")]
        public float flashMaxDuration = 7f;
        [Header("Noise")]
        public float detonationNoiseRadius = 35f;
        public float bounceNoiseRadius = 7f;
    }
}
