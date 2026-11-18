# Container Ship — Tactical Stealth FPS Prototype (Unity 6)

A slow-paced Rainbow Six–style infiltration prototype: reach the control room of an abandoned container ship at midnight.
Pure Unity: C# scripts, ScriptableObjects, prefabs, a normal `.unity` scene, native NavMesh, Unity Input System, UGUI.

## Getting started
1. Install **Unity 6 (6000.0.x)**. In Unity Hub: *Add → Add project from disk* → select this folder (accept the editor-version prompt).
2. Let packages import (Input System, AI Navigation, UGUI are in `Packages/manifest.json`).
3. A dialog offers to **generate the prototype**; click *Build it*. (Or run **Tools ▸ Container Ship ▸ Build Prototype**.)
   If Unity asks to enable the new Input System backend / restart, accept.
4. Open `Assets/_Project/Scenes/ContainerShip_Level01.unity` and press **Play**.

Works in the Built-in pipeline or URP (materials pick the right shader). Re-running the builder keeps your edited data assets/materials and
regenerates prefabs + scene.

## Controls (all rebindable on the `_Managers ▸ InputReader` component)
WASD move · Mouse look · Q/E lean · LMB fire · RMB aim · R reload · Shift sprint · MMB throw grenade · 1 AKS-74U · 2 Makarov ·
3 frag · 4 flashbang · N night vision · **F (hold) interact** (E is lean) · R restarts on the end screen.

## Folder layout
`Assets/_Project/` — `Scripts/{Core,Data,Player,Weapons,Enemy,World,UI,Editor}`, plus generated `Data/` (ScriptableObjects), `Materials/`, `Prefabs/`, `Scenes/`.

## ScriptableObjects (config, no runtime logic)
| Asset | Type | Key fields |
|---|---|---|
| PlayerStats | PlayerStats | health, walk/sprint/aim speed, accel/decel, look sens, lean distance/angle, noise radii, sprint recovery |
| WD_AKS74U / WD_Makarov | WeaponData | damage, RPM, auto/semi, mag size, reserve mags, reload time, spread, recoil, ADS FOV, viewmodel positions, noise radius, optional audio clips |
| GD_Fragmentation / GD_Flashbang | GrenadeData | type, fuse, radius, lethal radius, damage, flash duration, throw force, starting count |
| ES_Guard | EnemyStats | health, speeds, view range/angle, dark-vision factor, hearing, awareness rates, attack range/damage/accuracy, flash susceptibility |

## GameObjects and required components
| GameObject | Components / references |
|---|---|
| **Player** (prefab) | CharacterController, Health, PlayerController (stats, head, playerCamera), PlayerVisibility, PlayerInteractor (player), NightVision (nvLight), WeaponManager (player, weapons[AKS, Makarov]), GrenadeThrower (player, fragPrefab, flashPrefab) |
| Player ▸ Head ▸ PlayerCamera | Camera (tag MainCamera), AudioListener |
| …PlayerCamera ▸ NightVisionLight | Light (disabled until N) |
| …PlayerCamera ▸ WeaponHolder ▸ AKS-74U / Makarov | Weapon (data, muzzle) + placeholder meshes |
| **Enemy_Guard** (prefab) | NavMeshAgent, CapsuleCollider, Health, EnemyPerception (stats, eyes), EnemyAI (stats, patrolPoints, eyes, muzzle); child Head has SphereCollider + HitZone (×2.5) |
| **SecurityCamera** (prefab) | Health (35), SecurityCamera (head, indicator, indicatorLight); the cone mesh is generated at runtime under Head |
| **Grenade_Frag / Grenade_Flashbang** (prefabs) | Rigidbody, Collider (bouncy PhysicsMaterial), Grenade (data) |
| **Level** | NavMeshSurface (collect Children), all geometry as child cubes with colliders |
| **Level ▸ Control_Console** | BoxCollider, ObjectiveConsole (screen) — hold F |
| **ObjectiveZone_ControlRoom** | Trigger BoxCollider, ObjectiveZone |
| **_Managers** | InputReader, GameManager (playerHealth), ObjectiveManager, LevelAmbience (clankPoints), LevelBootstrap (surface) |
| **HUD_Canvas** | Canvas, CanvasScaler, GraphicRaycaster, HUD (all Text/Image/GameObject refs + player refs); child NightVisionOverlay has NightVisionOverlay (root, vignette, noise) |
| Lights | Moon (directional), spot "floodlights" on deck, flickering engine/corridor lights (FlickerLight), bright control-room lights |

To add an enemy: drag `Enemy_Guard` in, create empty Transforms as patrol points, assign them to `EnemyAI.patrolPoints`.
To add a camera: drag `SecurityCamera`, rotate it (forward = +Z of the root), tune sweep/range/FOV.
To make anything shootable/explodable: add `Health` (and optionally `HitZone` on child colliders).

## How the systems fit together
- **Damage**: `IDamageable` → `Health` (shared by player, enemies, cameras, props) → `Died`/`Damaged` events. Hitscan uses `Ballistics.Raycast`; grenades use `OverlapSphere` + line-of-sight so cover blocks blasts.
- **Noise**: `NoiseSystem.Emit(pos, radius, type, source)`. Walking is quiet (3 m), sprinting loud (14 m), suppressed shots (6–9 m), unsuppressed gunfire/explosions (35 m). Enemies filter by distance and wall occlusion.
- **Sprint penalties**: noise ×4+, vision multiplier on the player, bigger enemy awareness gain, no aim/fire while sprinting plus a recovery delay.
- **Darkness**: `PlayerVisibility` samples nearby lights (with line of sight). Enemy vision range and camera detection speed shrink in shadow; night vision brightens only *your* view.
- **Enemy AI** (`EnemyAI`): Patrolling → Suspicious → Alerted → Pursuing → Attacking → Returning, plus Stunned (flashbang) and Dead. An awareness meter (0–1) fills from sight; noises jump states directly. Pursuit alerts nearby allies.
- **Cameras**: sweep, cone + LOS detection, alert nearby guards; cones are drawn (wall-clipped) only with night vision on; they can be shot out.
- **Flashbang**: stun duration scales with distance, line of sight and whether the enemy faces the blast; also briefly whites out the player.
- **Audio**: all sounds are procedurally generated (`SoundFX`) so the game is audible with no imports. Assign real clips to `WeaponData` or replace calls in `SoundFX` later.

## Known limits / next steps
- Placeholder primitive art and procedural audio; no animations (enemies are capsules that fall over).
- The weapon viewmodel can clip into walls; add a weapon pull-back raycast or a second weapon camera.
- `Sprites/Default` shader is used for tracers/cones/sparks (works in Built-in and URP in the Editor; add it to *Always Included Shaders* for player builds).
- Not play-tested inside Unity in the authoring environment: expect to tune light intensities (render-pipeline dependent), NavMesh agent settings and balance values via the ScriptableObjects.
