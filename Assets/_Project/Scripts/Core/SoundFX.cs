using System.Collections.Generic;
using UnityEngine;
namespace ContainerShip
{
    /// <summary>Procedurally generated placeholder audio, so the prototype is audible with zero imported assets.
    /// Assign real AudioClips on WeaponData to override weapon sounds.</summary>
    public static class SoundFX
    {
        const int SR = 44100;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Cache.Clear(); }

        static AudioClip Make(string name, float dur, System.Func<float, float, float> gen)
        {
            if (Cache.TryGetValue(name, out var c) && c != null) return c;
            int n = Mathf.Max(1, (int)(dur * SR));
            var data = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; data[i] = Mathf.Clamp(gen(t, t / dur), -1f, 1f); }
            c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(data, 0);
            Cache[name] = c;
            return c;
        }

        static AudioClip Burst(string name, float dur, float lp, float decay, float toneHz, float toneAmt, float vol)
        {
            float y = 0f;
            var rng = new System.Random(name.GetHashCode());
            float gain = 0.6f / Mathf.Sqrt(lp / (2f - lp));
            return Make(name, dur, (t, u) =>
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                y += lp * (x - y);
                float env = Mathf.Pow(1f - u, decay);
                float s = y * gain * env;
                if (toneHz > 0f) s += Mathf.Sin(2f * Mathf.PI * toneHz * t) * toneAmt * env;
                return s * vol;
            });
        }

        public static AudioClip Shot => Burst("shot", 0.14f, 0.28f, 4f, 95f, 0.5f, 0.55f);
        public static AudioClip PistolShot => Burst("pshot", 0.10f, 0.40f, 5f, 140f, 0.35f, 0.5f);
        public static AudioClip EnemyShot => Burst("eshot", 0.25f, 0.8f, 3f, 70f, 0.6f, 0.9f);
        public static AudioClip Step => Burst("step", 0.14f, 0.06f, 3f, 55f, 0.5f, 0.6f);
        public static AudioClip Explosion => Burst("boom", 1.4f, 0.04f, 2f, 40f, 0.9f, 1f);
        public static AudioClip Hurt => Burst("hurt", 0.25f, 0.1f, 3f, 70f, 0.7f, 0.8f);

        public static AudioClip Flashbang
        {
            get
            {
                var rng = new System.Random(7);
                return Make("bang", 1.0f, (t, u) =>
                {
                    float n = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Pow(1f - u, 5f);
                    float ring = Mathf.Sin(2f * Mathf.PI * 3200f * t) * 0.3f * Mathf.Pow(1f - u, 1.5f);
                    return n + ring;
                });
            }
        }

        public static AudioClip Reload
        {
            get
            {
                var rng = new System.Random(3);
                float[] times = { 0f, 0.22f, 0.6f };
                return Make("reload", 0.75f, (t, u) =>
                {
                    float s = 0f;
                    foreach (float ct in times)
                    {
                        float k = t - ct;
                        if (k >= 0f && k < 0.05f) s += (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-k * 110f) * 0.7f;
                    }
                    return s;
                });
            }
        }

        public static AudioClip Dry
        {
            get
            {
                var rng = new System.Random(5);
                return Make("dry", 0.08f, (t, u) => (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 130f) * 0.5f);
            }
        }

        public static AudioClip Click => Make("click", 0.06f, (t, u) => Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 110f) * 0.4f);

        public static AudioClip Clank => Make("clank", 0.9f, (t, u) =>
            (Mathf.Sin(2f * Mathf.PI * 430f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 913f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 1470f * t)) * Mathf.Exp(-t * 6f) * 0.35f);

        public static AudioClip GrenadeBounce => Make("gbounce", 0.25f, (t, u) =>
            (Mathf.Sin(2f * Mathf.PI * 1800f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2700f * t)) * Mathf.Exp(-t * 30f) * 0.4f);

        public static AudioClip Beep => Make("beep", 0.12f, (t, u) => Mathf.Sin(2f * Mathf.PI * 1000f * t) * (1f - u) * 0.4f);

        public static AudioClip NVOn => Make("nvon", 0.35f, (t, u) => Mathf.Sin(2f * Mathf.PI * (800f + 3000f * u) * t) * (1f - u) * 0.25f);
        public static AudioClip NVOff => Make("nvoff", 0.3f, (t, u) => Mathf.Sin(2f * Mathf.PI * (3000f - 2400f * u) * t) * (1f - u) * 0.25f);

        // Loops (integer cycle counts over the length so the hum tiles cleanly)
        public static AudioClip Hum => Make("hum", 4f, (t, u) =>
            (Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.3f + Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.1f) * 0.6f);

        public static AudioClip Vent
        {
            get
            {
                float y = 0f; var rng = new System.Random(11);
                return Make("vent", 6f, (t, u) =>
                {
                    float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                    y += 0.03f * (x - y);
                    return y * 3f * Mathf.Sin(Mathf.PI * u);
                });
            }
        }

        public static void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("SFX2D");
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.volume = volume; s.pitch = pitch; s.spatialBlend = 0f;
            s.Play();
            UnityEngine.Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        public static void Play3D(AudioClip clip, Vector3 pos, float volume = 1f, float maxDistance = 40f, float pitch = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("SFX3D");
            go.transform.position = pos;
            var s = go.AddComponent<AudioSource>();
            s.clip = clip; s.volume = volume; s.pitch = pitch; s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear; s.minDistance = 2f; s.maxDistance = maxDistance;
            s.Play();
            UnityEngine.Object.Destroy(go, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }
    }
}
