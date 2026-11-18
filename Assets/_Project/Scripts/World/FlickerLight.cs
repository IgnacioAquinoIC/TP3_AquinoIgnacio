using UnityEngine;
namespace ContainerShip
{
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [Range(0f, 1f)] public float minFactor = 0.25f;
        public float speed = 6f;
        public float blackoutChancePerSecond = 0.03f;
        public float blackoutDuration = 0.4f;
        Light _l; float _base, _seed, _blackout;

        void Awake() { _l = GetComponent<Light>(); _base = _l.intensity; _seed = Random.value * 100f; }

        void Update()
        {
            if (_blackout > 0f) { _blackout -= Time.deltaTime; _l.intensity = 0f; return; }
            if (Random.value < blackoutChancePerSecond * Time.deltaTime) { _blackout = blackoutDuration; return; }
            _l.intensity = _base * Mathf.Lerp(minFactor, 1f, Mathf.PerlinNoise(Time.time * speed, _seed));
        }
    }
}
