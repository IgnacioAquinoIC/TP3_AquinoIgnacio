using UnityEngine;
namespace ContainerShip
{
    /// <summary>Single-shot crossbow. Uses the standard Weapon ammo/reload/viewmodel flow (magazine = 1 bolt, reserve = 2 more),
    /// but fires a physical CrossbowBolt instead of a hitscan ray. Reloads automatically after each shot.</summary>
    public class Crossbow : Weapon
    {
        public CrossbowBolt boltPrefab;
        [Tooltip("Optional: the bolt shown on the viewmodel rail. Hidden while empty / reloading.")]
        public GameObject loadedBolt;

        CrossbowData Cb => (CrossbowData)data;

        void Update()
        {
            if (loadedBolt != null) loadedBolt.SetActive(AmmoInMag > 0 && !IsReloading);
        }

        protected override void Shoot()
        {
            _nextFire = Time.time + 60f / data.roundsPerMinute;
            AmmoInMag--;

            Transform ct = _cam.transform;
            float spread = Mathf.Lerp(data.hipSpread, data.adsSpread, _player.AimBlend) + _player.SpeedFraction * data.movingSpreadAdd;
            Vector2 r = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 dir = (ct.forward + ct.right * r.x + ct.up * r.y).normalized;

            // Spawn just in front of the camera; if a wall is right there, back off so the bolt does not start inside it.
            float dist = 0.5f;
            if (Ballistics.Raycast(ct.position, dir, 0.7f, _player.transform, out RaycastHit hit)) dist = Mathf.Max(0.05f, hit.distance - 0.17f);
            Vector3 pos = ct.position + dir * dist;

            var bolt = Instantiate(boltPrefab, pos, Quaternion.LookRotation(dir));
            var bc = bolt.GetComponent<Collider>();
            var pc = _player.GetComponent<Collider>();
            if (bc != null && pc != null) Physics.IgnoreCollision(bc, pc);
            bolt.Launch(dir * Cb.boltSpeed, Cb, _player.gameObject);

            float m = Mathf.Lerp(1f, data.adsRecoilMultiplier, _player.AimBlend);
            _player.AddRecoil(data.recoilPitch * m, Random.Range(-data.recoilYaw, data.recoilYaw) * m, data.recoilRecovery);
            _kick = Mathf.Min(_kick + data.kickback, 0.1f);

            SoundFX.Play2D(data.fireClip != null ? data.fireClip : SoundFX.CrossbowShot, data.fireVolume, Random.Range(0.95f, 1.05f));
            NoiseSystem.Emit(_player.transform.position, data.shotNoiseRadius, NoiseType.SuppressedShot, _player.gameObject);

            TryReload(); // spent: start winding the next bolt in (data.reloadTime) if any are left in reserve
        }
    }
}
