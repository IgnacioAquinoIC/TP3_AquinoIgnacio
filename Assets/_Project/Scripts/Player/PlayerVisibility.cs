using UnityEngine;
namespace ContainerShip
{
    /// <summary>Estimates how well lit the player is (0 dark .. 1 fully lit). Enemies and cameras use this:
    /// standing in shadow shrinks their effective vision range.</summary>
    public class PlayerVisibility : MonoBehaviour
    {
        public static PlayerVisibility Instance { get; private set; }
        [Range(0f, 1f)] public float ambientExposure = 0.12f;
        [Tooltip("Light.intensity that counts as fully illuminating at point-blank.")]
        public float fullExposureIntensity = 3f;
        public float Exposure { get; private set; }

        Light[] _lights; float _timer;

        void Awake() { Instance = this; Exposure = ambientExposure; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            var all = FindObjectsByType<Light>(FindObjectsSortMode.None);
            var list = new System.Collections.Generic.List<Light>();
            foreach (var l in all)
            {
                if (l.type == LightType.Directional || l.transform.IsChildOf(transform)) continue;
                list.Add(l);
            }
            _lights = list.ToArray();
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f || _lights == null) return;
            _timer = 0.15f;
            Vector3 p = transform.position + Vector3.up * 1.0f;
            float best = 0f;
            foreach (var l in _lights)
            {
                if (l == null || !l.enabled || !l.gameObject.activeInHierarchy) continue;
                Vector3 lp = l.transform.position;
                float d = Vector3.Distance(lp, p);
                if (d > l.range) continue;
                if (l.type == LightType.Spot && Vector3.Angle(l.transform.forward, p - lp) > l.spotAngle * 0.5f) continue;
                float att = 1f - Mathf.Pow(d / l.range, 2f);
                float c = Mathf.Clamp01(att) * Mathf.Clamp01(l.intensity / fullExposureIntensity);
                if (c <= best) continue;
                if (!Ballistics.HasLineOfSight(lp, p, transform)) continue;
                best = c;
            }
            Exposure = Mathf.Max(ambientExposure, best);
        }
    }
}
