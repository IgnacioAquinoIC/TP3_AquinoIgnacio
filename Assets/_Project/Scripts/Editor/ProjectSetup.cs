using System.IO;
using UnityEditor;
using UnityEngine;
namespace ContainerShip.EditorTools
{
    /// <summary>On first open: enables the Input System backend and offers to generate the prototype content.</summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        static ProjectSetup() { EditorApplication.delayCall += Check; }

        static void Check()
        {
            try
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets != null && assets.Length > 0)
                {
                    var so = new SerializedObject(assets[0]);
                    var p = so.FindProperty("activeInputHandler");
                    if (p != null && p.intValue == 0)
                    {
                        p.intValue = 2; // Both
                        so.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                        EditorUtility.DisplayDialog("Container Ship",
                            "Active Input Handling was set to 'Both'. If Unity asks to restart the editor, accept.", "OK");
                    }
                }
            }
            catch { /* non-fatal */ }

            if (!File.Exists(ContainerShipBuilder.ScenePath) && !SessionState.GetBool("ContainerShipAsked", false))
            {
                SessionState.SetBool("ContainerShipAsked", true);
                if (EditorUtility.DisplayDialog("Container Ship",
                    "Generate the prototype (data assets, materials, prefabs, level scene, NavMesh) now?\n\nYou can also run it later from Tools > Container Ship.",
                    "Build it", "Later"))
                    ContainerShipBuilder.BuildAll();
            }
        }
    }
}
