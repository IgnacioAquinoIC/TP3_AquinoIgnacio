using UnityEngine;
namespace ContainerShip
{
    public static class Ballistics
    {
        static readonly RaycastHit[] Buf = new RaycastHit[48];

        /// <summary>Closest non-trigger hit, skipping anything under ignoreRoot.</summary>
        public static bool Raycast(Vector3 origin, Vector3 dir, float range, Transform ignoreRoot, out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(origin, dir, Buf, range, ~0, QueryTriggerInteraction.Ignore);
            float min = float.MaxValue; bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Buf[i];
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (h.distance < min) { min = h.distance; best = h; found = true; }
            }
            return found;
        }

        public static bool HasLineOfSight(Vector3 from, Vector3 to, Transform ignoreA, Transform ignoreB = null)
        {
            Vector3 d = to - from; float dist = d.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics.RaycastNonAlloc(from, d / dist, Buf, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var t = Buf[i].collider.transform;
                if (ignoreA != null && t.IsChildOf(ignoreA)) continue;
                if (ignoreB != null && t.IsChildOf(ignoreB)) continue;
                return false;
            }
            return true;
        }
    }
}
