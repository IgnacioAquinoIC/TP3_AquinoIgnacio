using UnityEngine;
namespace ContainerShip
{
    [CreateAssetMenu(menuName = "Container Ship/Player Stats", fileName = "PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Health")] public float maxHealth = 100f;
        [Header("Movement")]
        public float walkSpeed = 2.3f;
        public float sprintSpeed = 5.0f;
        public float aimWalkSpeed = 1.2f;
        public float acceleration = 8f;
        public float deceleration = 11f;
        public float gravity = -20f;
        [Header("Look")]
        public float lookSensitivity = 0.09f;
        public float aimSensitivityMultiplier = 0.6f;
        public float maxPitch = 80f;
        [Header("Camera")]
        public float eyeHeight = 1.62f;
        public float baseFov = 70f;
        public float aimTime = 0.18f;
        [Header("Lean")]
        public float leanDistance = 0.45f;
        public float leanAngle = 14f;
        public float leanSpeed = 8f;
        public float leanCollisionRadius = 0.2f;
        [Range(0f, 1f)] public float aimLeanMultiplier = 0.7f;
        [Header("Noise (radius in metres that enemies can hear)")]
        public float walkNoiseRadius = 3f;
        public float sprintNoiseRadius = 14f;
        public float walkStepInterval = 0.65f;
        public float sprintStepInterval = 0.36f;
        [Header("Sprint penalties")]
        [Tooltip("Seconds after releasing sprint before the player can aim or fire.")]
        public float sprintRecoveryTime = 0.5f;
    }
}
