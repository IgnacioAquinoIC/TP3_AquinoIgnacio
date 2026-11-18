using UnityEngine;
namespace ContainerShip
{
    [CreateAssetMenu(menuName = "Container Ship/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string displayName = "Weapon";
        public bool automatic = true;
        [Header("Damage")]
        public float damage = 24f;
        public float range = 90f;
        public float impactForce = 6f;
        [Header("Fire")]
        public float roundsPerMinute = 650f;
        [Header("Ammunition")]
        public int magazineSize = 30;
        public int startingReserveMagazines = 3;
        public float reloadTime = 2.5f;
        public float equipTime = 0.5f;
        [Header("Accuracy (degrees of cone spread)")]
        public float hipSpread = 3f;
        public float adsSpread = 0.25f;
        public float movingSpreadAdd = 2.5f;
        [Header("Recoil")]
        public float recoilPitch = 0.9f;
        public float recoilYaw = 0.4f;
        public float recoilRecovery = 7f;
        [Range(0f, 1f)] public float adsRecoilMultiplier = 0.6f;
        public float kickback = 0.035f;
        [Header("Aiming")]
        public float adsFov = 52f;
        [Header("Viewmodel (local to camera)")]
        public Vector3 hipPosition = new Vector3(0.17f, -0.2f, 0.42f);
        public Vector3 adsPosition = new Vector3(0f, -0.068f, 0.3f);
        public Vector3 sprintPosition = new Vector3(0.25f, -0.3f, 0.3f);
        public Vector3 sprintRotation = new Vector3(15f, -40f, 0f);
        [Header("Noise")]
        [Tooltip("Suppressed: how far the shot can be heard by enemies.")]
        public float shotNoiseRadius = 9f;
        [Header("Audio (optional; procedural sound is used when empty)")]
        public AudioClip fireClip;
        public AudioClip reloadClip;
        public AudioClip dryClip;
        public float fireVolume = 0.6f;
    }
}
