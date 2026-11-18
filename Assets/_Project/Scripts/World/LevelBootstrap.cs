using Unity.AI.Navigation;
using UnityEngine;
namespace ContainerShip
{
    /// <summary>Safety net: if the level's NavMeshSurface has no baked data, bake at startup (before enemies spawn their agents).</summary>
    [DefaultExecutionOrder(-100)]
    public class LevelBootstrap : MonoBehaviour
    {
        public NavMeshSurface surface;
        void Awake()
        {
            if (surface != null && surface.navMeshData == null) surface.BuildNavMesh();
        }
    }
}
