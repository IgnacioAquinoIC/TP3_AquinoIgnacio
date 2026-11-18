using System;
using UnityEngine;
namespace ContainerShip
{
    public enum NoiseType { Footstep, SuppressedShot, Gunshot, Explosion, Impact, Other }

    /// <summary>Global noise bus. Anything that makes sound emits here; enemies listen.</summary>
    public static class NoiseSystem
    {
        public static event Action<Vector3, float, NoiseType, GameObject> NoiseEmitted;
        public static void Emit(Vector3 position, float radius, NoiseType type, GameObject source)
        {
            NoiseEmitted?.Invoke(position, radius, type, source);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { NoiseEmitted = null; }
    }
}
