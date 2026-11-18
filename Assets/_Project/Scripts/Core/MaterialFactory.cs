using UnityEngine;
using UnityEngine.Rendering;
namespace ContainerShip
{
    public static class MaterialFactory
    {
        /// <summary>Lit shader that matches the active pipeline (Built-in or URP).</summary>
        public static Shader LitShader()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit");
                if (s != null) return s;
            }
            return Shader.Find("Standard");
        }
    }
}
