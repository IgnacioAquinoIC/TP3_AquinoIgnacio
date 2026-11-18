using UnityEngine;
namespace ContainerShip
{
    /// <summary>Engine hum + ventilation loops and random distant metallic clanks.</summary>
    public class LevelAmbience : MonoBehaviour
    {
        public Transform[] clankPoints;
        public Vector2 clankInterval = new Vector2(6f, 16f);
        public float humVolume = 0.12f, ventVolume = 0.08f;
        float _next;

        void Start()
        {
            Loop(SoundFX.Hum, humVolume);
            Loop(SoundFX.Vent, ventVolume);
            _next = Time.time + Random.Range(clankInterval.x, clankInterval.y);
        }

        void Loop(AudioClip clip, float vol)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = clip; s.loop = true; s.volume = vol; s.spatialBlend = 0f; s.Play();
        }

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + Random.Range(clankInterval.x, clankInterval.y);
            Vector3 pos;
            if (clankPoints != null && clankPoints.Length > 0) pos = clankPoints[Random.Range(0, clankPoints.Length)].position;
            else if (PlayerController.Instance != null)
            {
                Vector2 r = Random.insideUnitCircle.normalized * Random.Range(12f, 25f);
                pos = PlayerController.Instance.transform.position + new Vector3(r.x, 1f, r.y);
            }
            else return;
            SoundFX.Play3D(SoundFX.Clank, pos, Random.Range(0.4f, 0.9f), 60f, Random.Range(0.7f, 1.2f));
        }
    }
}
