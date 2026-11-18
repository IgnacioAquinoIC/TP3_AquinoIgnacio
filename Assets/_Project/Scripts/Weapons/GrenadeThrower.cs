using UnityEngine;
namespace ContainerShip
{
    /// <summary>Grenade inventory. Keys 3/4 select the type, middle mouse throws the selected grenade.</summary>
    public class GrenadeThrower : MonoBehaviour
    {
        public PlayerController player;
        public Grenade fragPrefab;
        public Grenade flashPrefab;
        public GrenadeType Selected { get; private set; } = GrenadeType.Fragmentation;
        public int FragCount { get; private set; }
        public int FlashCount { get; private set; }
        float _cooldown;

        void Start()
        {
            FragCount = fragPrefab.data.startingCount;
            FlashCount = flashPrefab.data.startingCount;
        }

        void Update()
        {
            if (!GameManager.IsPlaying || InputReader.Instance == null) return;
            var input = InputReader.Instance;
            _cooldown -= Time.deltaTime;
            if (input.Slot3.WasPressedThisFrame()) Select(GrenadeType.Fragmentation);
            if (input.Slot4.WasPressedThisFrame()) Select(GrenadeType.Flashbang);
            if (input.Throw.WasPressedThisFrame() && _cooldown <= 0f) Throw();
        }

        void Select(GrenadeType t)
        {
            Selected = t;
            GameEvents.Message(t == GrenadeType.Fragmentation ? "FRAGMENTATION GRENADE SELECTED" : "FLASHBANG SELECTED");
            SoundFX.Play2D(SoundFX.Click, 0.6f);
        }

        void Throw()
        {
            bool frag = Selected == GrenadeType.Fragmentation;
            if ((frag ? FragCount : FlashCount) <= 0) { GameEvents.Message("NO GRENADES LEFT", NotifyKind.Warning); _cooldown = 0.5f; return; }

            Transform cam = player.playerCamera.transform;
            Vector3 pos = cam.position + cam.forward * 0.4f;
            if (Ballistics.Raycast(cam.position, cam.forward, 0.5f, player.transform, out RaycastHit hit))
                pos = hit.point - cam.forward * 0.1f;

            var prefab = frag ? fragPrefab : flashPrefab;
            var g = Instantiate(prefab, pos, Random.rotation);
            var gc = g.GetComponent<Collider>();
            var pc = player.GetComponent<Collider>();
            if (gc != null && pc != null) Physics.IgnoreCollision(gc, pc);
            g.Launch(cam.forward * prefab.data.throwForce + Vector3.up * 2f + player.Velocity, player.gameObject);

            if (frag) FragCount--; else FlashCount--;
            _cooldown = 0.8f;
            SoundFX.Play2D(SoundFX.Click, 0.5f, 0.7f);
        }
    }
}
