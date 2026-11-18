using UnityEngine;
namespace ContainerShip
{
    [CreateAssetMenu(menuName = "Container Ship/Enemy Stats", fileName = "EnemyStats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")] public float maxHealth = 100f;
        [Header("Movement")]
        public float walkSpeed = 1.4f;
        public float searchSpeed = 2.2f;
        public float chaseSpeed = 3.8f;
        public float turnSpeed = 240f;
        [Header("Vision")]
        public float viewRange = 24f;
        public float viewAngle = 100f;
        public float closeSenseRange = 2.5f;
        [Range(0f, 1f)] [Tooltip("Fraction of view range remaining when the player is in total darkness.")]
        public float darkVisibilityFactor = 0.3f;
        [Header("Awareness")]
        public float awarenessGainPerSecond = 1.4f;
        public float awarenessDecayPerSecond = 0.25f;
        [Header("Hearing")]
        public float hearingMultiplier = 1f;
        [Header("Behaviour timing")]
        public float suspiciousLookTime = 4f;
        public float searchDuration = 14f;
        public float searchRadius = 7f;
        public float loseTrackTime = 4f;
        public float alertShareRadius = 15f;
        [Header("Attack")]
        public float attackRange = 18f;
        public float reactionTime = 0.5f;
        public float fireInterval = 0.3f;
        public float damage = 9f;
        [Range(0f, 1f)] public float accuracy = 0.55f;
        public float gunshotNoiseRadius = 35f;
        [Header("Flashbang")]
        public float flashSusceptibility = 1f;
    }
}
