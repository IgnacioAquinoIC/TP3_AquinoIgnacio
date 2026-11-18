using UnityEngine;
namespace ContainerShip
{
    /// <summary>Line-of-sight vision. Range shrinks when the player stands in the dark; sprinting makes the player easier to notice.</summary>
    public class EnemyPerception : MonoBehaviour
    {
        public EnemyStats stats;
        public Transform eyes;
        public bool CanSeePlayer { get; private set; }
        public float Visibility { get; private set; }
        public Vector3 LastSeenPos { get; private set; }

        public void Tick()
        {
            var p = PlayerController.Instance;
            if (p == null || stats == null) { CanSeePlayer = false; Visibility = 0f; return; }
            Transform e = eyes != null ? eyes : transform;
            Vector3 eyePos = e.position;
            Vector3 target = p.transform.position + Vector3.up * 1.2f;
            Vector3 to = target - eyePos;
            float dist = to.magnitude;

            float exposure = PlayerVisibility.Instance != null ? PlayerVisibility.Instance.Exposure : 1f;
            float range = stats.viewRange * Mathf.Lerp(stats.darkVisibilityFactor, 1f, exposure);
            if (p.IsSprinting) range *= 1.15f;

            bool close = dist <= stats.closeSenseRange;
            bool inCone = dist <= range && Vector3.Angle(e.forward, to) <= stats.viewAngle * 0.5f;
            CanSeePlayer = (close || inCone) && Ballistics.HasLineOfSight(eyePos, target, transform, p.transform);

            if (CanSeePlayer)
            {
                LastSeenPos = p.transform.position;
                float vis = Mathf.Max(0.25f, 1f - dist / Mathf.Max(range, 0.1f));
                if (p.IsSprinting) vis *= 1.6f; else if (p.SpeedFraction < 0.05f) vis *= 0.6f;
                if (close) vis = 3f;
                Visibility = vis;
            }
            else Visibility = 0f;
        }
    }
}
