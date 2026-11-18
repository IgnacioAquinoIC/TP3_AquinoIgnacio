using UnityEngine;
namespace ContainerShip
{
    /// <summary>Holds the weapons (index 0 = AKS-74U, 1 = Makarov), handles switching with keys 1/2, ticks the active weapon.</summary>
    public class WeaponManager : MonoBehaviour
    {
        public PlayerController player;
        public Weapon[] weapons;
        public int startIndex = 0;

        int _current = -1;
        public Weapon Current => _current >= 0 ? weapons[_current] : null;

        void Start()
        {
            foreach (var w in weapons) { w.Init(player, player.playerCamera); w.gameObject.SetActive(false); }
            Equip(startIndex, false);
        }

        void Update()
        {
            if (!GameManager.IsPlaying || InputReader.Instance == null) return;
            var input = InputReader.Instance;
            if (input.Slot1.WasPressedThisFrame()) Equip(0, true);
            if (input.Slot2.WasPressedThisFrame()) Equip(1, true);
            if (Current != null) Current.Tick(Time.deltaTime, input);
        }

        void Equip(int i, bool announce)
        {
            if (i < 0 || i >= weapons.Length || i == _current || !weapons[i].available) return;
            if (_current >= 0) { weapons[_current].OnUnequip(); weapons[_current].gameObject.SetActive(false); }
            _current = i;
            weapons[i].gameObject.SetActive(true);
            weapons[i].OnEquip();
            player.CurrentAdsFov = weapons[i].data.adsFov;
            if (announce) { GameEvents.Message(weapons[i].data.displayName.ToUpper()); SoundFX.Play2D(SoundFX.Click, 0.6f); }
        }
    }
}
