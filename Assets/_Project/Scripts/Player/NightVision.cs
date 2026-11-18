using System;
using UnityEngine;
namespace ContainerShip
{
    /// <summary>Toggles night vision (default N): brightens the world, shows the green overlay (see NightVisionOverlay)
    /// and makes SecurityCamera detection cones visible.</summary>
    public class NightVision : MonoBehaviour
    {
        public static bool IsActive { get; private set; }
        public static event Action<bool> Toggled;

        public Light nvLight;
        public Color nvAmbient = new Color(0.07f, 0.12f, 0.08f);
        public float nvFogDensity = 0.012f;

        Color _ambient; float _fog;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { IsActive = false; Toggled = null; }

        void Start()
        {
            _ambient = RenderSettings.ambientLight; _fog = RenderSettings.fogDensity;
            if (nvLight != null) nvLight.enabled = false;
        }

        void Update()
        {
            if (!GameManager.IsPlaying || InputReader.Instance == null) return;
            if (InputReader.Instance.NightVision.WasPressedThisFrame()) Set(!IsActive);
        }

        void Set(bool on)
        {
            IsActive = on;
            if (nvLight != null) nvLight.enabled = on;
            RenderSettings.ambientLight = on ? nvAmbient : _ambient;
            RenderSettings.fogDensity = on ? nvFogDensity : _fog;
            SoundFX.Play2D(on ? SoundFX.NVOn : SoundFX.NVOff, 0.6f);
            Toggled?.Invoke(on);
        }

        void OnDisable()
        {
            if (IsActive) { RenderSettings.ambientLight = _ambient; RenderSettings.fogDensity = _fog; IsActive = false; }
        }
    }
}
