using UnityEngine;
using UnityEngine.UI;
namespace ContainerShip
{
    /// <summary>Minimal HUD: health, weapon, ammo, grenades, objective, prompts, feedback, end screen. No minimap by design.</summary>
    public class HUD : MonoBehaviour
    {
        [Header("Sources")]
        public Health playerHealth;
        public PlayerController player;
        public WeaponManager weapons;
        public GrenadeThrower grenades;
        public PlayerInteractor interactor;

        [Header("Texts")]
        public Text healthText, weaponText, ammoText, grenadeText, objectiveText, notificationText, promptText, endTitle, endSubtitle;
        [Header("Images")]
        public Image damageOverlay, flashOverlay, crosshair, holdFill;
        public GameObject holdBar, endPanel;

        float _dmg, _flash, _noteTimer;
        static readonly Color Info = new Color(0.85f, 0.95f, 0.85f), Warn = new Color(1f, 0.35f, 0.25f), Good = new Color(0.4f, 1f, 0.5f);

        void OnEnable()
        {
            GameEvents.Notify += OnNotify;
            GameEvents.PlayerFlashed += OnFlashed;
            if (playerHealth != null) playerHealth.Damaged += OnDamaged;
        }
        void OnDisable()
        {
            GameEvents.Notify -= OnNotify;
            GameEvents.PlayerFlashed -= OnFlashed;
            if (playerHealth != null) playerHealth.Damaged -= OnDamaged;
        }

        void Start()
        {
            if (endPanel != null) endPanel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnState;
        }

        void OnState(GameState s)
        {
            endPanel.SetActive(true);
            if (s == GameState.Victory) { endTitle.text = "MISSION COMPLETE"; endTitle.color = Good; endSubtitle.text = "Control room secured.\n\nPress R to restart"; }
            else { endTitle.text = "MISSION FAILED"; endTitle.color = Warn; endSubtitle.text = "You were killed.\n\nPress R to restart"; }
        }

        void OnNotify(string text, NotifyKind kind)
        {
            notificationText.text = text;
            notificationText.color = kind == NotifyKind.Warning ? Warn : kind == NotifyKind.Success ? Good : Info;
            _noteTimer = 2.5f;
        }
        void OnDamaged(DamageInfo i) { _dmg = 0.55f; SoundFX.Play2D(SoundFX.Hurt, 0.8f); }
        void OnFlashed(float intensity) { _flash = Mathf.Max(_flash, intensity); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            healthText.text = "HEALTH  " + Mathf.CeilToInt(playerHealth.Current);
            healthText.color = playerHealth.Normalized < 0.3f ? Warn : Info;

            var w = weapons.Current;
            if (w != null)
            {
                weaponText.text = w.data.displayName.ToUpper();
                ammoText.text = w.IsReloading ? "RELOADING..." : w.AmmoInMag + " / " + w.Reserve + "    MAGS " + w.SpareMagazines;
            }
            bool fragSel = grenades.Selected == GrenadeType.Fragmentation;
            grenadeText.text = (fragSel ? "> " : "  ") + "[3] FRAG x" + grenades.FragCount + "\n" +
                               (!fragSel ? "> " : "  ") + "[4] FLASH x" + grenades.FlashCount;
            objectiveText.text = ObjectiveManager.Instance != null ? "OBJECTIVE: " + ObjectiveManager.Instance.CurrentObjective : "";

            _noteTimer -= dt;
            var nc = notificationText.color; nc.a = Mathf.Clamp01(_noteTimer / 0.5f); notificationText.color = nc;

            promptText.text = interactor.Prompt;
            bool showBar = !string.IsNullOrEmpty(interactor.Prompt) && interactor.Progress > 0f;
            holdBar.SetActive(showBar);
            if (showBar) holdFill.rectTransform.anchorMax = new Vector2(interactor.Progress, 1f);

            _dmg = Mathf.MoveTowards(_dmg, 0f, dt * 0.8f);
            _flash = Mathf.MoveTowards(_flash, 0f, dt * 0.25f);
            float low = playerHealth.Normalized < 0.3f ? (0.15f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f)) : 0f;
            SetAlpha(damageOverlay, Mathf.Max(_dmg, low));
            SetAlpha(flashOverlay, _flash);
            crosshair.enabled = player.AimBlend < 0.5f && !player.IsSprinting;
        }

        static void SetAlpha(Image img, float a) { var c = img.color; c.a = a; img.color = c; }
    }
}
