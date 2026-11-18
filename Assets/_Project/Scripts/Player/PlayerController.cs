using UnityEngine;
namespace ContainerShip
{
    /// <summary>Slow, deliberate first-person movement, look, lean, sprint penalties and footstep noise.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public PlayerStats stats;
        public Transform head;
        public Camera playerCamera;

        public bool IsSprinting { get; private set; }
        public bool IsAiming { get; private set; }
        public float AimBlend { get; private set; }
        public float LeanNormalized => stats.leanDistance > 0f ? _leanMeters / stats.leanDistance : 0f;
        /// <summary>True while sprinting and for a short time after: weapons cannot be aimed or fired.</summary>
        public bool SprintRecovering => IsSprinting || _sinceSprint < stats.sprintRecoveryTime;
        public float SpeedFraction => Mathf.Clamp01(new Vector2(_vel.x, _vel.z).magnitude / stats.sprintSpeed);
        public Vector3 Velocity => _vel;
        public float CurrentAdsFov = 55f;

        CharacterController _cc;
        Vector3 _vel;
        float _vy, _pitch, _recoil, _recoilRecovery = 7f, _leanMeters, _stepTimer, _sinceSprint = 10f;
        static readonly RaycastHit[] LeanBuf = new RaycastHit[8];

        void Awake()
        {
            Instance = this;
            _cc = GetComponent<CharacterController>();
            if (head == null) head = transform.Find("Head");
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            var h = GetComponent<Health>();
            if (h != null) h.SetMax(stats.maxHealth);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            var input = InputReader.Instance;
            if (input == null || !GameManager.IsPlaying) return;
            float dt = Time.deltaTime;
            HandleLook(input);
            HandleMovement(input, dt);
            HandleLean(input, dt);
            HandleCamera(dt);
            HandleFootsteps(dt);
        }

        void HandleLook(InputReader input)
        {
            Vector2 d = input.Look.ReadValue<Vector2>() * stats.lookSensitivity;
            d *= Mathf.Lerp(1f, stats.aimSensitivityMultiplier, AimBlend);
            transform.Rotate(Vector3.up * d.x);
            _pitch = Mathf.Clamp(_pitch - d.y, -stats.maxPitch, stats.maxPitch);
        }

        void HandleMovement(InputReader input, float dt)
        {
            Vector2 m = Vector2.ClampMagnitude(input.Move.ReadValue<Vector2>(), 1f);
            bool wasSprinting = IsSprinting;
            IsSprinting = input.Sprint.IsPressed() && m.y > 0.1f;
            if (wasSprinting && !IsSprinting) _sinceSprint = 0f;
            else if (!IsSprinting) _sinceSprint += dt;
            IsAiming = input.Aim.IsPressed() && !SprintRecovering;

            float speed = IsSprinting ? stats.sprintSpeed : (IsAiming ? stats.aimWalkSpeed : stats.walkSpeed);
            Vector3 wish = (transform.right * m.x + transform.forward * m.y) * speed;
            float rate = wish.sqrMagnitude > 0.01f ? stats.acceleration : stats.deceleration;
            _vel = Vector3.MoveTowards(_vel, wish, rate * dt);

            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            _vy += stats.gravity * dt;
            _cc.Move((_vel + Vector3.up * _vy) * dt);
        }

        void HandleLean(InputReader input, float dt)
        {
            float dir = (input.LeanRight.IsPressed() ? 1f : 0f) - (input.LeanLeft.IsPressed() ? 1f : 0f);
            if (IsSprinting) dir = 0f;
            float allowed = ClampLean(dir);
            _leanMeters = Mathf.Lerp(_leanMeters, allowed, 1f - Mathf.Exp(-stats.leanSpeed * dt));
        }

        // Spherecast sideways from the neutral eye position so the head never pushes into geometry.
        float ClampLean(float dir)
        {
            float dist = Mathf.Abs(dir) * stats.leanDistance;
            if (dist < 0.001f) return 0f;
            Vector3 origin = transform.position + Vector3.up * stats.eyeHeight;
            Vector3 side = transform.right * Mathf.Sign(dir);
            int n = Physics.SphereCastNonAlloc(origin, stats.leanCollisionRadius, side, LeanBuf, dist + 0.05f, ~0, QueryTriggerInteraction.Ignore);
            float allowed = dist;
            for (int i = 0; i < n; i++)
            {
                if (LeanBuf[i].collider.transform.IsChildOf(transform)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, LeanBuf[i].distance - 0.02f));
            }
            return Mathf.Sign(dir) * allowed;
        }

        void HandleCamera(float dt)
        {
            _recoil = Mathf.MoveTowards(_recoil, 0f, _recoilRecovery * dt);
            AimBlend = Mathf.MoveTowards(AimBlend, IsAiming ? 1f : 0f, dt / Mathf.Max(0.01f, stats.aimTime));
            float lean = LeanNormalized;
            float aimMul = Mathf.Lerp(1f, stats.aimLeanMultiplier, AimBlend);
            float roll = -lean * stats.leanAngle * aimMul;
            head.localPosition = new Vector3(_leanMeters, stats.eyeHeight - Mathf.Abs(lean) * 0.08f, 0f);
            head.localRotation = Quaternion.Euler(_pitch - _recoil, 0f, roll);
            playerCamera.fieldOfView = Mathf.Lerp(stats.baseFov, CurrentAdsFov, Mathf.SmoothStep(0f, 1f, AimBlend));
        }

        void HandleFootsteps(float dt)
        {
            float planar = new Vector2(_vel.x, _vel.z).magnitude;
            if (!_cc.isGrounded || planar < 0.4f) { _stepTimer = 0f; return; }
            float k = Mathf.InverseLerp(stats.walkSpeed, stats.sprintSpeed, planar);
            float interval = Mathf.Lerp(stats.walkStepInterval, stats.sprintStepInterval, k);
            _stepTimer += dt;
            if (_stepTimer < interval) return;
            _stepTimer = 0f;
            float radius = IsSprinting ? stats.sprintNoiseRadius
                : stats.walkNoiseRadius * Mathf.Clamp01(planar / stats.walkSpeed) * (IsAiming ? 0.6f : 1f);
            SoundFX.Play2D(SoundFX.Step, IsSprinting ? 0.9f : 0.3f, Random.Range(0.9f, 1.1f));
            NoiseSystem.Emit(transform.position, radius, NoiseType.Footstep, gameObject);
        }

        /// <summary>Called by weapons. Pitch kicks the view up and recovers; yaw is a small permanent nudge.</summary>
        public void AddRecoil(float pitch, float yaw, float recovery)
        {
            _recoil = Mathf.Min(_recoil + pitch, 12f);
            _recoilRecovery = recovery;
            transform.Rotate(Vector3.up * yaw);
        }
    }
}
