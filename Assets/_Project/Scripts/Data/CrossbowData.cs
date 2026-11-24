using UnityEngine;
namespace ContainerShip
{
    /// <summary>Crossbow = a normal WeaponData (ammo, reload, recoil, viewmodel poses) plus explosive-bolt settings.
    /// WeaponData.damage is the direct impact damage of the bolt itself (headshots use the usual HitZone multiplier).</summary>
    [CreateAssetMenu(menuName = "Container Ship/Crossbow Data", fileName = "CrossbowData")]
    public class CrossbowData : WeaponData
    {
        [Header("Explosive bolt")]
        [Tooltip("Blast settings (radius, lethal radius, damage, force, noise). Point this at GD_Fragmentation so the bolt blast always equals the frag grenade.")]
        public GrenadeData blast;
        public float boltSpeed = 70f;
        [Tooltip("Seconds between the bolt hitting something and detonating.")]
        public float fuseTime = 1.5f;
        [Tooltip("Below this impact speed (m/s) the bolt does not embed; it just drops and rolls (fuse still starts).")]
        public float stickMinSpeed = 6f;
        [Tooltip("How far the tip is buried in the surface (meters).")]
        public float embedDepth = 0.06f;
        [Tooltip("A bolt that never touches anything is removed after this many seconds.")]
        public float maxFlightTime = 8f;
        public float stickNoiseRadius = 5f;
    }
}
