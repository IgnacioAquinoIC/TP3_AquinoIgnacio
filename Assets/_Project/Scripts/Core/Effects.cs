using UnityEngine;
namespace ContainerShip
{
    /// <summary>Runtime-generated placeholder VFX (sparks, decals, tracers, lights). Swap for prefabs later.</summary>
    public static class Effects
    {
        public static Material UnlitColor(Color c)
        {
            var m = new Material(Shader.Find("Sprites/Default"));
            m.color = c;
            return m;
        }

        public static void FlashLight(Vector3 pos, Color c, float intensity, float range, float duration)
        {
            var go = new GameObject("TempLight");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            go.AddComponent<TimedLight>().Init(l, duration);
        }

        static ParticleSystem MakeBurst(Vector3 pos, Quaternion rot, int count, float life, float speedMin, float speedMax,
                                        float size, Color color, float gravity, ParticleSystemShapeType shape, float angle, float radius)
        {
            var go = new GameObject("FX");
            go.transform.SetPositionAndRotation(pos, rot);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();
            var main = ps.main;
            main.duration = 0.5f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = life;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = size; main.startColor = color; main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = shape; sh.angle = angle; sh.radius = radius;
            go.GetComponent<ParticleSystemRenderer>().material = UnlitColor(Color.white);
            ps.Play();
            UnityEngine.Object.Destroy(go, life + 1f);
            return ps;
        }

        public static void Impact(Vector3 point, Vector3 normal, bool flesh)
        {
            Color c = flesh ? new Color(0.7f, 0.05f, 0.05f) : new Color(1f, 0.8f, 0.4f);
            MakeBurst(point, Quaternion.LookRotation(normal), 8, 0.35f, 1f, 3f, 0.025f, c, 1f, ParticleSystemShapeType.Cone, 25f, 0.01f);
            if (!flesh)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                UnityEngine.Object.Destroy(q.GetComponent<Collider>());
                q.transform.position = point + normal * 0.004f;
                q.transform.rotation = Quaternion.LookRotation(-normal);
                q.transform.localScale = Vector3.one * 0.07f;
                q.GetComponent<Renderer>().material = UnlitColor(new Color(0.03f, 0.03f, 0.03f, 0.9f));
                UnityEngine.Object.Destroy(q, 25f);
            }
        }

        public static void Explosion(Vector3 pos, float radius)
        {
            FlashLight(pos + Vector3.up * 0.5f, new Color(1f, 0.6f, 0.25f), 8f, radius * 1.5f, 0.35f);
            MakeBurst(pos, Quaternion.identity, 60, 0.7f, radius * 0.4f, radius * 1.2f, 0.18f, new Color(1f, 0.55f, 0.2f), 0.3f, ParticleSystemShapeType.Sphere, 0f, 0.2f);
            MakeBurst(pos, Quaternion.identity, 18, 2.2f, 0.5f, 2f, 0.9f, new Color(0.25f, 0.25f, 0.25f, 0.6f), -0.05f, ParticleSystemShapeType.Sphere, 0f, 0.3f);
        }

        public static void FlashBang(Vector3 pos)
        {
            FlashLight(pos + Vector3.up * 0.3f, Color.white, 14f, 25f, 0.5f);
            MakeBurst(pos, Quaternion.identity, 30, 0.4f, 2f, 6f, 0.12f, Color.white, 0f, ParticleSystemShapeType.Sphere, 0f, 0.1f);
        }

        public static void Tracer(Vector3 from, Vector3 to, Color c)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2; lr.SetPosition(0, from); lr.SetPosition(1, to);
            lr.widthMultiplier = 0.03f; lr.material = UnlitColor(c);
            lr.startColor = c; lr.endColor = c;
            UnityEngine.Object.Destroy(go, 0.06f);
        }
    }

    public class TimedLight : MonoBehaviour
    {
        Light _l; float _dur, _t, _start;
        public void Init(Light l, float duration) { _l = l; _dur = Mathf.Max(0.01f, duration); _start = l.intensity; }
        void Update()
        {
            _t += Time.deltaTime;
            if (_l != null) _l.intensity = _start * Mathf.Clamp01(1f - _t / _dur);
            if (_t >= _dur) Destroy(gameObject);
        }
    }
}
