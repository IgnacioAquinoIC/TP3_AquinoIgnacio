using UnityEngine;
namespace ContainerShip
{
    /// <summary>Sweeping surveillance camera. Detects the player via cone + line of sight. Its cone is drawn only
    /// while night vision is active, clipped by walls. Can be shot out (Health).</summary>
    [RequireComponent(typeof(Health))]
    public class SecurityCamera : MonoBehaviour
    {
        public Transform head;
        public Renderer indicator;
        public Light indicatorLight;
        [Header("Sweep")]
        public float sweepAngle = 45f;
        public float sweepSpeed = 18f;
        public float endPause = 1.5f;
        [Header("Detection")]
        public float viewRange = 14f;
        public float fieldOfView = 55f;
        public float detectionTime = 1.2f;
        public float alertRadius = 25f;
        public float retriggerCooldown = 8f;
        [Header("Cone visual")]
        public int coneSegments = 20;

        float _angle, _dir = 1f, _pause, _detect, _cool;
        Quaternion _baseRot; Mesh _mesh; GameObject _cone; MeshRenderer _coneRend;
        Vector3[] _verts, _ring; Health _health; bool _dead;

        void Awake()
        {
            _health = GetComponent<Health>();
            if (head == null) head = transform;
            _baseRot = head.localRotation;
            BuildCone();
        }

        void OnEnable() { GetComponent<Health>().Died += OnDied; }
        void OnDisable() { var h = GetComponent<Health>(); if (h != null) h.Died -= OnDied; }

        void BuildCone()
        {
            int n = Mathf.Max(8, coneSegments);
            _ring = new Vector3[n]; _verts = new Vector3[n + 1];
            float half = fieldOfView * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * i / n;
                _ring[i] = new Vector3(Mathf.Sin(half) * Mathf.Cos(a), Mathf.Sin(half) * Mathf.Sin(a), Mathf.Cos(half)).normalized;
            }
            var tris = new int[n * 3];
            for (int i = 0; i < n; i++) { tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = (i + 1) % n + 1; }
            _mesh = new Mesh { name = "CameraCone" };
            _mesh.MarkDynamic();
            _mesh.vertices = _verts; _mesh.triangles = tris;
            _cone = new GameObject("SurveillanceCone");
            _cone.transform.SetParent(head, false);
            _cone.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _coneRend = _cone.AddComponent<MeshRenderer>();
            _coneRend.material = Effects.UnlitColor(new Color(0.2f, 1f, 0.3f, 0.16f));
            _coneRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cone.SetActive(false);
        }

        void Update()
        {
            if (_dead) return;
            float dt = Time.deltaTime;
            Sweep(dt);
            if (GameManager.IsPlaying) Detect(dt);
            UpdateCone();
        }

        void Sweep(float dt)
        {
            if (sweepAngle <= 0.01f) return;
            if (_pause > 0f) { _pause -= dt; return; }
            _angle += _dir * sweepSpeed * dt;
            if (Mathf.Abs(_angle) >= sweepAngle) { _angle = Mathf.Clamp(_angle, -sweepAngle, sweepAngle); _dir = -_dir; _pause = endPause; }
            head.localRotation = _baseRot * Quaternion.Euler(0f, _angle, 0f);
        }

        void Detect(float dt)
        {
            var p = PlayerController.Instance; if (p == null) return;
            Vector3 eye = head.position;
            Vector3 target = p.transform.position + Vector3.up * 1.2f;
            Vector3 to = target - eye; float dist = to.magnitude;
            bool seen = dist <= viewRange && Vector3.Angle(head.forward, to) <= fieldOfView * 0.5f
                        && Ballistics.HasLineOfSight(eye, target, transform, p.transform);
            _cool -= dt;
            if (seen)
            {
                float exposure = PlayerVisibility.Instance != null ? PlayerVisibility.Instance.Exposure : 1f;
                float gain = Mathf.Lerp(0.45f, 1f, exposure) * (p.IsSprinting ? 1.3f : 1f);
                _detect += dt * gain;
            }
            else _detect = Mathf.Max(0f, _detect - dt * 0.6f);

            if (_detect >= detectionTime && _cool <= 0f) { Trigger(p); _detect = 0f; _cool = retriggerCooldown; }
        }

        void Trigger(PlayerController p)
        {
            GameEvents.Message("SPOTTED BY CAMERA", NotifyKind.Warning);
            GameEvents.RaiseDetected("Camera");
            SoundFX.Play3D(SoundFX.Beep, head.position, 1f, 40f);
            foreach (var e in EnemyAI.All.ToArray())
                if (e != null && !e.IsDead && Vector3.Distance(e.transform.position, head.position) <= alertRadius)
                    e.Investigate(p.transform.position, true);
            SetIndicator(Color.red);
        }

        void SetIndicator(Color c)
        {
            if (indicator != null) { indicator.material.color = c; indicator.material.SetColor("_EmissionColor", c * 2f); }
            if (indicatorLight != null) indicatorLight.color = c;
        }

        void UpdateCone()
        {
            bool show = NightVision.IsActive;
            if (_cone.activeSelf != show) _cone.SetActive(show);
            if (!show) return;
            for (int i = 0; i < _ring.Length; i++)
            {
                Vector3 wd = head.TransformDirection(_ring[i]);
                float d = viewRange;
                if (Ballistics.Raycast(head.position, wd, viewRange, transform, out RaycastHit hit)) d = hit.distance;
                _verts[i + 1] = _ring[i] * d;
            }
            _verts[0] = Vector3.zero;
            _mesh.vertices = _verts;
            _mesh.RecalculateBounds();
            Color calm = new Color(0.2f, 1f, 0.3f, 0.16f), hot = new Color(1f, 0.15f, 0.1f, 0.3f);
            _coneRend.material.color = _cool > 0f ? hot : Color.Lerp(calm, hot, Mathf.Clamp01(_detect / detectionTime));
        }

        void OnDied(DamageInfo info)
        {
            _dead = true;
            if (_cone != null) _cone.SetActive(false);
            if (indicatorLight != null) indicatorLight.enabled = false;
            if (indicator != null) indicator.material.SetColor("_EmissionColor", Color.black);
            head.localRotation *= Quaternion.Euler(35f, 0f, 0f);
            Effects.Impact(head.position, Vector3.up, false);
        }

        void OnDrawGizmosSelected()
        {
            Transform h = head != null ? head : transform;
            Gizmos.color = Color.red;
            float half = fieldOfView * 0.5f;
            Gizmos.DrawRay(h.position, Quaternion.Euler(0, half, 0) * h.forward * viewRange);
            Gizmos.DrawRay(h.position, Quaternion.Euler(0, -half, 0) * h.forward * viewRange);
        }
    }
}
