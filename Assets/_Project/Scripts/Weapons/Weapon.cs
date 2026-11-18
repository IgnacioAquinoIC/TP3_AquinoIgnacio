using UnityEngine;
namespace ContainerShip
{
    /// <summary>One hitscan weapon. Behaviour comes from its WeaponData asset; WeaponManager calls Tick() when equipped.</summary>
    public class Weapon : MonoBehaviour
    {
        public WeaponData data;
        public Transform muzzle;
        public bool available = true;

        public int AmmoInMag { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }
        public int SpareMagazines => Mathf.CeilToInt(Reserve / (float)Mathf.Max(1, data.magazineSize));

        PlayerController _player; Camera _cam;
        float _nextFire, _reloadTimer, _equipTimer, _sprintBlend, _kick;

        public void Init(PlayerController player, Camera cam)
        {
            _player = player; _cam = cam;
            AmmoInMag = data.magazineSize;
            Reserve = data.magazineSize * data.startingReserveMagazines;
        }

        public void OnEquip() { IsReloading = false; _equipTimer = data.equipTime; _nextFire = Time.time + 0.1f; }
        public void OnUnequip() { IsReloading = false; }

        public void Tick(float dt, InputReader input)
        {
            _equipTimer = Mathf.Max(0f, _equipTimer - dt);
            _sprintBlend = Mathf.MoveTowards(_sprintBlend, _player.IsSprinting ? 1f : 0f, dt * 5f);
            _kick = Mathf.Lerp(_kick, 0f, dt * 12f);

            if (IsReloading)
            {
                _reloadTimer -= dt;
                if (_reloadTimer <= 0f) FinishReload();
            }

            bool canHandle = !_player.SprintRecovering && _equipTimer <= 0f;
            if (canHandle)
            {
                if (input.Reload.WasPressedThisFrame()) TryReload();
                bool trigger = data.automatic ? input.Fire.IsPressed() : input.Fire.WasPressedThisFrame();
                if (trigger && !IsReloading && Time.time >= _nextFire)
                {
                    if (AmmoInMag > 0) Shoot();
                    else
                    {
                        if (input.Fire.WasPressedThisFrame()) SoundFX.Play2D(data.dryClip != null ? data.dryClip : SoundFX.Dry, 0.6f);
                        TryReload();
                        _nextFire = Time.time + 0.25f;
                    }
                }
            }
            UpdateViewmodel();
        }

        void Shoot()
        {
            _nextFire = Time.time + 60f / data.roundsPerMinute;
            AmmoInMag--;
            Transform ct = _cam.transform;
            float spread = Mathf.Lerp(data.hipSpread, data.adsSpread, _player.AimBlend) + _player.SpeedFraction * data.movingSpreadAdd;
            Vector2 r = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 dir = (ct.forward + ct.right * r.x + ct.up * r.y).normalized;

            if (Ballistics.Raycast(ct.position, dir, data.range, _player.transform, out RaycastHit hit))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null)
                    dmg.TakeDamage(new DamageInfo { amount = data.damage, point = hit.point, direction = dir, source = _player.gameObject, type = DamageType.Bullet });
                Effects.Impact(hit.point, hit.normal, dmg != null && hit.collider.GetComponentInParent<EnemyAI>() != null);
                if (hit.rigidbody != null) hit.rigidbody.AddForceAtPosition(dir * data.impactForce, hit.point, ForceMode.Impulse);
            }

            float m = Mathf.Lerp(1f, data.adsRecoilMultiplier, _player.AimBlend);
            _player.AddRecoil(data.recoilPitch * Random.Range(0.8f, 1.2f) * m, Random.Range(-data.recoilYaw, data.recoilYaw) * m, data.recoilRecovery);
            _kick = Mathf.Min(_kick + data.kickback, 0.1f);

            var clip = data.fireClip != null ? data.fireClip : (data.automatic ? SoundFX.Shot : SoundFX.PistolShot);
            SoundFX.Play2D(clip, data.fireVolume, Random.Range(0.95f, 1.05f));
            if (muzzle != null) Effects.FlashLight(muzzle.position, new Color(1f, 0.7f, 0.3f), 1.2f, 3.5f, 0.03f);
            NoiseSystem.Emit(_player.transform.position, data.shotNoiseRadius, NoiseType.SuppressedShot, _player.gameObject);
        }

        void TryReload()
        {
            if (IsReloading || AmmoInMag >= data.magazineSize || Reserve <= 0) return;
            IsReloading = true;
            _reloadTimer = data.reloadTime;
            SoundFX.Play2D(data.reloadClip != null ? data.reloadClip : SoundFX.Reload, 0.7f);
            GameEvents.Message("RELOADING");
        }

        void FinishReload()
        {
            int take = Mathf.Min(data.magazineSize - AmmoInMag, Reserve);
            AmmoInMag += take; Reserve -= take;
            IsReloading = false;
        }

        void UpdateViewmodel()
        {
            Vector3 pos = Vector3.Lerp(data.hipPosition, data.adsPosition, _player.AimBlend);
            pos = Vector3.Lerp(pos, data.sprintPosition, _sprintBlend);
            Quaternion rot = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(data.sprintRotation), _sprintBlend);

            if (IsReloading)
            {
                float p = 1f - Mathf.Clamp01(_reloadTimer / Mathf.Max(0.01f, data.reloadTime));
                float lower = Mathf.Sin(p * Mathf.PI);
                pos += new Vector3(0f, -0.12f * lower, 0f);
                rot *= Quaternion.Euler(20f * lower, 0f, -15f * lower);
            }
            if (_equipTimer > 0f)
            {
                float e = _equipTimer / Mathf.Max(0.01f, data.equipTime);
                pos += new Vector3(0f, -0.35f * e, 0f);
                rot *= Quaternion.Euler(30f * e, 0f, 0f);
            }
            float bob = _player.SpeedFraction * (1f - _player.AimBlend);
            pos += new Vector3(Mathf.Sin(Time.time * 7f) * 0.004f, Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.004f, 0f) * bob;
            pos.z -= _kick;
            transform.localPosition = pos;
            transform.localRotation = rot;
        }
    }
}
