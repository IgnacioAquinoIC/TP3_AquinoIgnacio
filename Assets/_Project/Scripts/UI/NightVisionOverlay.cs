using UnityEngine;
using UnityEngine.UI;
namespace ContainerShip
{
    /// <summary>Green tint, vignette and grain shown while night vision is on. Put this script on a GameObject that stays
    /// active, and point 'root' at the child holding the three images.</summary>
    public class NightVisionOverlay : MonoBehaviour
    {
        public GameObject root;
        public Image vignette;
        public RawImage noise;

        void Awake()
        {
            const int s = 128;
            var vt = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x / (s - 1f) - 0.5f) * 2f, dy = (y / (s - 1f) - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    vt.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0.5f, 1.15f, r) * 0.9f));
                }
            vt.Apply();
            vignette.sprite = Sprite.Create(vt, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));

            var nt = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++) { float v = Random.value; nt.SetPixel(x, y, new Color(v, v, v, 1f)); }
            nt.Apply();
            noise.texture = nt;
        }

        void OnEnable() { NightVision.Toggled += Set; Set(NightVision.IsActive); }
        void OnDisable() { NightVision.Toggled -= Set; }
        void Set(bool on) { if (root != null) root.SetActive(on); }

        void Update()
        {
            if (root != null && root.activeSelf) noise.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
        }
    }
}
