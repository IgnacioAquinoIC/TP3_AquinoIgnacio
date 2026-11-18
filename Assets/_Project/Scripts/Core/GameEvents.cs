using System;
using UnityEngine;
namespace ContainerShip
{
    public enum NotifyKind { Info, Warning, Success }

    public static class GameEvents
    {
        public static event Action<string, NotifyKind> Notify;
        public static event Action<float> PlayerFlashed;
        public static event Action<string> PlayerDetected;
        static float _lastDetected = -99f;

        public static void Message(string text, NotifyKind kind = NotifyKind.Info) { Notify?.Invoke(text, kind); }
        public static void RaiseFlashed(float intensity) { PlayerFlashed?.Invoke(intensity); }
        public static void RaiseDetected(string source)
        {
            if (Time.unscaledTime - _lastDetected < 3f) return;
            _lastDetected = Time.unscaledTime;
            PlayerDetected?.Invoke(source);
            Message("DETECTED", NotifyKind.Warning);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Notify = null; PlayerFlashed = null; PlayerDetected = null; _lastDetected = -99f; }
    }
}
