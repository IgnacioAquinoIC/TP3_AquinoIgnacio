using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ContainerShip.EditorTools
{
    /// <summary>Generates data assets, materials, prefabs and the playable level scene. Re-runnable: existing data assets
    /// and materials are kept (so your edits survive); prefabs and the scene are regenerated.</summary>
    public static class ContainerShipBuilder
    {
        const string Root = "Assets/_Project";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string MatDir = Root + "/Materials";
        const string SceneDir = Root + "/Scenes";
        public const string ScenePath = SceneDir + "/ContainerShip_Level01.unity";

        static PlayerStats _playerStats; static WeaponData _aks, _mak; static GrenadeData _frag, _flash; static EnemyStats _guard;
        static Grenade _fragPrefab, _flashPrefab;
        static GameObject _enemyPrefab, _cameraPrefab, _playerPrefab;

        [MenuItem("Tools/Container Ship/Build Prototype (assets + prefabs + scene)")]
        public static void BuildAll()
        {
            EnsureFolders();
            CreateData();
            CreateGrenadePrefabs();
            CreateEnemyPrefab();
            CreateCameraPrefab();
            CreatePlayerPrefab();
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Container Ship prototype built. Open " + ScenePath + " and press Play.");
        }

        [MenuItem("Tools/Container Ship/Open Level Scene")]
        public static void OpenScene() { if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath); }

        // ------------------------------------------------------------------ setup
        static void EnsureFolders()
        {
            foreach (var f in new[] { Root, DataDir, PrefabDir, MatDir, SceneDir })
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = Path.GetDirectoryName(f).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
                }
        }

        static T Asset<T>(string name, System.Action<T> init = null) where T : ScriptableObject
        {
            string path = DataDir + "/" + name + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); init?.Invoke(a); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static void CreateData()
        {
            _playerStats = Asset<PlayerStats>("PlayerStats");
            _aks = Asset<WeaponData>("WD_AKS74U", d =>
            {
                d.displayName = "AKS-74U (Suppressed)"; d.automatic = true; d.damage = 24f; d.roundsPerMinute = 650f;
                d.magazineSize = 30; d.startingReserveMagazines = 3; d.reloadTime = 2.5f; d.equipTime = 0.5f;
                d.hipSpread = 3f; d.adsSpread = 0.25f; d.movingSpreadAdd = 2.5f;
                d.recoilPitch = 0.9f; d.recoilYaw = 0.4f; d.recoilRecovery = 7f; d.kickback = 0.035f; d.adsFov = 52f;
                d.hipPosition = new Vector3(0.17f, -0.2f, 0.42f); d.adsPosition = new Vector3(0f, -0.068f, 0.3f);
                d.shotNoiseRadius = 9f; d.fireVolume = 0.6f;
            });
            _mak = Asset<WeaponData>("WD_Makarov", d =>
            {
                d.displayName = "Makarov (Suppressed)"; d.automatic = false; d.damage = 18f; d.roundsPerMinute = 380f;
                d.magazineSize = 8; d.startingReserveMagazines = 4; d.reloadTime = 1.7f; d.equipTime = 0.35f;
                d.hipSpread = 2.2f; d.adsSpread = 0.15f; d.movingSpreadAdd = 1.8f;
                d.recoilPitch = 1.4f; d.recoilYaw = 0.3f; d.recoilRecovery = 9f; d.kickback = 0.03f; d.adsFov = 58f; d.range = 60f;
                d.hipPosition = new Vector3(0.13f, -0.15f, 0.35f); d.adsPosition = new Vector3(0f, -0.052f, 0.3f);
                d.sprintPosition = new Vector3(0.2f, -0.28f, 0.25f);
                d.shotNoiseRadius = 6f; d.fireVolume = 0.5f;
            });
            _frag = Asset<GrenadeData>("GD_Fragmentation", d =>
            {
                d.displayName = "Fragmentation"; d.type = GrenadeType.Fragmentation; d.startingCount = 2;
                d.fuseTime = 3.5f; d.radius = 9f; d.lethalRadius = 4.5f; d.maxDamage = 160f;
            });
            _flash = Asset<GrenadeData>("GD_Flashbang", d =>
            {
                d.displayName = "Flashbang"; d.type = GrenadeType.Flashbang; d.startingCount = 2;
                d.fuseTime = 2.5f; d.radius = 12f; d.flashMaxDuration = 7f;
            });
            _guard = Asset<EnemyStats>("ES_Guard");
        }

        // ------------------------------------------------------------------ materials
        static Material M(string name, Color c, float metal = 0.3f, float smooth = 0.3f, Color? emission = null)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(MaterialFactory.LitShader());
            m.color = c;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material Steel => M("Mat_Floor", new Color(0.12f, 0.13f, 0.14f), 0.6f, 0.4f);
        static Material Wall => M("Mat_Wall", new Color(0.2f, 0.22f, 0.24f), 0.5f, 0.35f);
        static Material Rust => M("Mat_Pipe", new Color(0.35f, 0.22f, 0.13f), 0.6f, 0.25f);
        static Material Machine => M("Mat_Machinery", new Color(0.15f, 0.2f, 0.18f), 0.5f, 0.3f);
        static Material Dark => M("Mat_DarkMetal", new Color(0.07f, 0.07f, 0.08f), 0.8f, 0.5f);
        static Material Console => M("Mat_Console", new Color(0.1f, 0.1f, 0.12f), 0.4f, 0.4f);
        static Material Screen => M("Mat_Screen", new Color(0.05f, 0.2f, 0.1f), 0f, 0.8f, new Color(0.1f, 0.9f, 0.4f) * 1.5f);
        static Material Lamp => M("Mat_Lamp", new Color(1f, 0.9f, 0.7f), 0f, 0.5f, new Color(1f, 0.85f, 0.6f) * 2f);
        static Material Cloth => M("Mat_EnemyCloth", new Color(0.12f, 0.14f, 0.1f), 0f, 0.1f);
        static Material Skin => M("Mat_EnemySkin", new Color(0.5f, 0.4f, 0.35f), 0f, 0.2f);
        static Material Red => M("Mat_RedLight", new Color(0.8f, 0.05f, 0.05f), 0f, 0.5f, new Color(1f, 0.1f, 0.1f) * 2f);
        static Material[] Containers => new[]
        {
            M("Mat_Container_Red", new Color(0.45f, 0.15f, 0.1f), 0.5f, 0.25f),
            M("Mat_Container_Blue", new Color(0.1f, 0.2f, 0.35f), 0.5f, 0.25f),
            M("Mat_Container_Green", new Color(0.12f, 0.3f, 0.2f), 0.5f, 0.25f),
            M("Mat_Container_Orange", new Color(0.5f, 0.33f, 0.1f), 0.5f, 0.25f),
        };

        // ------------------------------------------------------------------ primitive helpers
        static GameObject Part(Transform parent, string name, PrimitiveType t, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, bool keepCollider = false)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        // Axis-aligned box from min/max coordinates (keeps its collider).
        static GameObject B(Transform parent, string name, float x0, float x1, float z0, float z1, float y0, float y1, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
            g.transform.localScale = new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0));
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static GameObject Pipe(Transform parent, string name, Vector3 center, float length, bool vertical, bool alongZ, float diameter, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = center;
            g.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            if (!vertical) g.transform.localEulerAngles = alongZ ? new Vector3(90f, 0f, 0f) : new Vector3(0f, 0f, 90f);
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        static void Save(GameObject go, string path, out GameObject prefab)
        {
            prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ------------------------------------------------------------------ prefabs
        static void CreateGrenadePrefabs()
        {
            _fragPrefab = MakeGrenade("Grenade_Frag", _frag, new Color(0.15f, 0.25f, 0.12f), false);
            _flashPrefab = MakeGrenade("Grenade_Flashbang", _flash, new Color(0.5f, 0.5f, 0.52f), true);
        }

        static Grenade MakeGrenade(string name, GrenadeData data, Color color, bool cylinder)
        {
            var go = GameObject.CreatePrimitive(cylinder ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            go.name = name;
            go.transform.localScale = cylinder ? new Vector3(0.05f, 0.07f, 0.05f) : Vector3.one * 0.08f;
            go.GetComponent<Renderer>().sharedMaterial = M("Mat_" + name, color, 0.5f, 0.4f);
            string pm = MatDir + "/GrenadeBounce.asset";
            var phys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pm);
            if (phys == null)
            {
                phys = new PhysicsMaterial("GrenadeBounce") { bounciness = 0.35f, dynamicFriction = 0.5f, staticFriction = 0.5f, bounceCombine = PhysicsMaterialCombine.Maximum };
                AssetDatabase.CreateAsset(phys, pm);
            }
            go.GetComponent<Collider>().sharedMaterial = phys;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.4f; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            go.AddComponent<Grenade>().data = data;
            Save(go, PrefabDir + "/" + name + ".prefab", out var prefab);
            return prefab.GetComponent<Grenade>();
        }

        static void CreateEnemyPrefab()
        {
            var root = new GameObject("Enemy_Guard");
            var cap = root.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 0.725f, 0f); cap.radius = 0.35f; cap.height = 1.45f;
            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f; agent.height = 1.8f;
            var health = root.AddComponent<Health>();
            var perception = root.AddComponent<EnemyPerception>();
            var ai = root.AddComponent<EnemyAI>();

            Part(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.725f, 0f), new Vector3(0.7f, 0.725f, 0.7f), Vector3.zero, Cloth);
            var head = Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.28f, Vector3.zero, Skin, true);
            var hz = head.AddComponent<HitZone>(); hz.health = health; hz.multiplier = 2.5f;
            Part(root.transform, "Visor", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0.12f), new Vector3(0.22f, 0.06f, 0.08f), Vector3.zero, Dark);
            Part(root.transform, "Rifle", PrimitiveType.Cube, new Vector3(0.2f, 1.2f, 0.3f), new Vector3(0.06f, 0.1f, 0.6f), Vector3.zero, Dark);

            var eyes = new GameObject("Eyes").transform; eyes.SetParent(root.transform, false); eyes.localPosition = new Vector3(0f, 1.62f, 0.1f);
            var muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(root.transform, false); muzzle.localPosition = new Vector3(0.2f, 1.22f, 0.62f);

            ai.stats = _guard; ai.eyes = eyes; ai.muzzle = muzzle;
            perception.stats = _guard; perception.eyes = eyes;
            Save(root, PrefabDir + "/Enemy_Guard.prefab", out _enemyPrefab);
        }

        static void CreateCameraPrefab()
        {
            var root = new GameObject("SecurityCamera");
            var health = root.AddComponent<Health>(); health.maxHealth = 35f;
            Part(root.transform, "Mount", PrimitiveType.Cube, new Vector3(0f, 0f, -0.2f), new Vector3(0.3f, 0.3f, 0.12f), Vector3.zero, Dark);
            var head = new GameObject("Head").transform; head.SetParent(root.transform, false);
            Part(head, "Body", PrimitiveType.Cube, new Vector3(0f, 0f, 0.1f), new Vector3(0.22f, 0.18f, 0.4f), Vector3.zero, Dark, true);
            Part(head, "Lens", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.32f), new Vector3(0.1f, 0.02f, 0.1f), new Vector3(90f, 0f, 0f), M("Mat_Lens", Color.black, 1f, 0.9f));
            var ind = Part(head, "Indicator", PrimitiveType.Sphere, new Vector3(0.08f, 0.09f, 0.25f), Vector3.one * 0.04f, Vector3.zero, Red);
            var lg = new GameObject("IndicatorLight"); lg.transform.SetParent(ind.transform, false);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = Color.red; l.range = 2.5f; l.intensity = 0.6f;
            var cam = root.AddComponent<SecurityCamera>();
            cam.head = head; cam.indicator = ind.GetComponent<Renderer>(); cam.indicatorLight = l;
            Save(root, PrefabDir + "/SecurityCamera.prefab", out _cameraPrefab);
        }

        static Weapon BuildAKS(Transform parent)
        {
            var root = new GameObject("AKS-74U"); root.transform.SetParent(parent, false);
            Part(root.transform, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.05f, 0.075f, 0.42f), Vector3.zero, Dark);
            Part(root.transform, "Suppressor", PrimitiveType.Cylinder, new Vector3(0f, 0.005f, 0.37f), new Vector3(0.04f, 0.16f, 0.04f), new Vector3(90f, 0f, 0f), Dark);
            Part(root.transform, "Stock", PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.28f), new Vector3(0.03f, 0.06f, 0.18f), Vector3.zero, Dark);
            Part(root.transform, "Magazine", PrimitiveType.Cube, new Vector3(0f, -0.11f, 0.04f), new Vector3(0.03f, 0.14f, 0.07f), new Vector3(-12f, 0f, 0f), Dark);
            Part(root.transform, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.09f, -0.12f), new Vector3(0.03f, 0.09f, 0.04f), Vector3.zero, Dark);
            Part(root.transform, "RearSight", PrimitiveType.Cube, new Vector3(0f, 0.045f, -0.1f), new Vector3(0.012f, 0.02f, 0.012f), Vector3.zero, Dark);
            Part(root.transform, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.2f), new Vector3(0.008f, 0.03f, 0.008f), Vector3.zero, Dark);
            var mz = new GameObject("Muzzle").transform; mz.SetParent(root.transform, false); mz.localPosition = new Vector3(0f, 0.005f, 0.55f);
            var w = root.AddComponent<Weapon>(); w.data = _aks; w.muzzle = mz; return w;
        }

        static Weapon BuildMakarov(Transform parent)
        {
            var root = new GameObject("Makarov"); root.transform.SetParent(parent, false);
            Part(root.transform, "Slide", PrimitiveType.Cube, new Vector3(0f, 0.015f, 0f), new Vector3(0.03f, 0.04f, 0.2f), Vector3.zero, Dark);
            Part(root.transform, "Frame", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0f), new Vector3(0.03f, 0.025f, 0.19f), Vector3.zero, Dark);
            Part(root.transform, "Grip", PrimitiveType.Cube, new Vector3(0f, -0.07f, -0.07f), new Vector3(0.03f, 0.1f, 0.055f), new Vector3(-15f, 0f, 0f), Dark);
            Part(root.transform, "Suppressor", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.19f), new Vector3(0.032f, 0.09f, 0.032f), new Vector3(90f, 0f, 0f), Dark);
            Part(root.transform, "RearSight", PrimitiveType.Cube, new Vector3(0f, 0.04f, -0.08f), new Vector3(0.01f, 0.014f, 0.01f), Vector3.zero, Dark);
            Part(root.transform, "FrontSight", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.08f), new Vector3(0.006f, 0.014f, 0.006f), Vector3.zero, Dark);
            var mz = new GameObject("Muzzle").transform; mz.SetParent(root.transform, false); mz.localPosition = new Vector3(0f, 0.015f, 0.29f);
            var w = root.AddComponent<Weapon>(); w.data = _mak; w.muzzle = mz; return w;
        }

        static void CreatePlayerPrefab()
        {
            var root = new GameObject("Player");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
            root.AddComponent<Health>();
            var pc = root.AddComponent<PlayerController>();
            root.AddComponent<PlayerVisibility>();
            var inter = root.AddComponent<PlayerInteractor>();
            var nv = root.AddComponent<NightVision>();
            var wm = root.AddComponent<WeaponManager>();
            var gt = root.AddComponent<GrenadeThrower>();

            var head = new GameObject("Head").transform; head.SetParent(root.transform, false); head.localPosition = new Vector3(0f, 1.62f, 0f);
            var camGo = new GameObject("PlayerCamera"); camGo.tag = "MainCamera"; camGo.transform.SetParent(head, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f; cam.fieldOfView = 70f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            var nvl = new GameObject("NightVisionLight"); nvl.transform.SetParent(camGo.transform, false);
            var light = nvl.AddComponent<Light>();
            light.type = LightType.Point; light.range = 20f; light.intensity = 1.1f; light.color = new Color(0.6f, 1f, 0.7f); light.shadows = LightShadows.Hard; light.enabled = false;

            var holder = new GameObject("WeaponHolder").transform; holder.SetParent(camGo.transform, false);
            var aks = BuildAKS(holder); var mak = BuildMakarov(holder);

            pc.stats = _playerStats; pc.head = head; pc.playerCamera = cam;
            inter.player = pc;
            nv.nvLight = light;
            wm.player = pc; wm.weapons = new[] { aks, mak };
            gt.player = pc; gt.fragPrefab = _fragPrefab; gt.flashPrefab = _flashPrefab;
            Save(root, PrefabDir + "/Player.prefab", out _playerPrefab);
        }

        // ------------------------------------------------------------------ scene
        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.015f, 0.018f, 0.025f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.025f; RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.04f);

            var level = new GameObject("Level").transform;
            BuildLevel(level);
            var surface = level.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            BuildLighting();
            var routes = new GameObject("PatrolRoutes").transform;
            BuildEnemies(routes);
            BuildCameras();

            // player
            var player = (GameObject)PrefabUtility.InstantiatePrefab(_playerPrefab);
            player.transform.position = new Vector3(0f, 0.1f, 0.5f);

            // managers
            var mgr = new GameObject("_Managers");
            mgr.AddComponent<InputReader>();
            var gm = mgr.AddComponent<GameManager>(); gm.playerHealth = player.GetComponent<Health>();
            mgr.AddComponent<ObjectiveManager>();
            var amb = mgr.AddComponent<LevelAmbience>();
            var pts = new List<Transform>();
            foreach (var p in new[] { new Vector3(-12, 3, 30), new Vector3(12, 3, 45), new Vector3(0, 3, 58), new Vector3(0, 3, 10), new Vector3(-8, 3, 38) })
            { var t = new GameObject("ClankPoint").transform; t.SetParent(mgr.transform); t.position = p; pts.Add(t); }
            amb.clankPoints = pts.ToArray();
            var boot = mgr.AddComponent<LevelBootstrap>(); boot.surface = surface;

            // objective zone
            var zone = new GameObject("ObjectiveZone_ControlRoom");
            zone.transform.position = new Vector3(4f, 1.5f, 58f);
            var bc = zone.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(19f, 3f, 11f);
            zone.AddComponent<ObjectiveZone>();

            BuildHUD(player);

            surface.BuildNavMesh();
            if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
                AssetDatabase.CreateAsset(surface.navMeshData, SceneDir + "/ShipNavMesh.asset");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void BuildLevel(Transform level)
        {
            var floor = Steel; var wall = Wall; var cont = Containers;

            B(level, "Floor", -14, 14, -2, 64, -0.5f, 0f, floor);
            B(level, "Hull_West", -14.5f, -14f, -2.5f, 64.5f, 0f, 5f, wall);
            B(level, "Hull_East", 14f, 14.5f, -2.5f, 64.5f, 0f, 5f, wall);
            B(level, "Hull_South", -14.5f, 14.5f, -2.5f, -2f, 0f, 5f, wall);
            B(level, "Hull_North", -14.5f, 14.5f, 64f, 64.5f, 0f, 5f, wall);

            // Deck containers: x0,x1,z0,z1,height,materialIndex
            float[][] c =
            {
                new[]{-13f,-5f,3.8f,6.2f,2.6f,0f}, new[]{-13f,-5f,7.8f,10.2f,5.2f,1f},
                new[]{5f,13f,4.8f,7.2f,2.6f,2f},   new[]{5f,13f,8.8f,11.2f,2.6f,3f},
                new[]{-3f,3f,10.8f,13.2f,2.6f,1f}, new[]{-10f,-2f,14.8f,17.2f,2.6f,2f},
                new[]{3f,11f,15.8f,18.2f,5.2f,0f}, new[]{-12f,-6f,19.8f,22.2f,2.6f,3f},
                new[]{5f,11f,20.8f,23.2f,2.6f,1f},
            };
            for (int i = 0; i < c.Length; i++)
                B(level, "Container_" + (i + 1).ToString("00"), c[i][0], c[i][1], c[i][2], c[i][3], 0f, c[i][4], cont[(int)c[i][5]]);

            // Bulkhead into engine room (door x -1.5..1.5)
            B(level, "Bulkhead_L", -14f, -1.5f, 24.75f, 25.25f, 0f, 5f, wall);
            B(level, "Bulkhead_R", 1.5f, 14f, 24.75f, 25.25f, 0f, 5f, wall);

            // Engine room
            B(level, "EngineCeiling", -14f, 14f, 25.25f, 41.5f, 4.4f, 4.7f, wall);
            B(level, "EngineBack_L", -14f, 8.25f, 41f, 41.5f, 0f, 4.7f, wall);
            B(level, "EngineBack_R", 11.75f, 14f, 41f, 41.5f, 0f, 4.7f, wall);
            B(level, "Generator_A", -10.5f, -5.5f, 28f, 32f, 0f, 3f, Machine);
            B(level, "Generator_B", 5.5f, 10.5f, 28f, 32f, 0f, 3f, Machine);
            B(level, "Pillar", -1f, 1f, 31f, 33f, 0f, 4.4f, Rust);
            B(level, "Cabinet", 11f, 13.5f, 34f, 36.5f, 0f, 2f, Machine);
            B(level, "Crates_A", -12.5f, -10.5f, 36f, 38.5f, 0f, 1.6f, cont[3]);
            B(level, "Crates_B", -8f, -6f, 38f, 40f, 0f, 1.2f, cont[1]);
            for (int i = 0; i < 3; i++) Pipe(level, "Pipe_V" + i, new Vector3(-13.3f, 2.2f, 27f + i * 6f), 4.4f, true, false, 0.35f, Rust);
            Pipe(level, "Pipe_H", new Vector3(-13.3f, 3.5f, 33.5f), 16f, false, true, 0.3f, Rust);
            Pipe(level, "Pipe_H2", new Vector3(13.3f, 3.2f, 33.5f), 16f, false, true, 0.25f, Rust);

            // Corridor (x 8.75..11.25, z 41.5..52)
            B(level, "Corridor_W", 8.25f, 8.75f, 41.5f, 52f, 0f, 3.2f, wall);
            B(level, "Corridor_E", 11.25f, 11.75f, 41.5f, 52f, 0f, 3.2f, wall);
            B(level, "Corridor_Ceiling", 8.25f, 11.75f, 41.5f, 52f, 3.2f, 3.5f, wall);

            // Control room (x -6..14, z 52.5..64)
            B(level, "Control_Front_L", -6.5f, 8.25f, 52f, 52.5f, 0f, 4.7f, wall);
            B(level, "Control_Front_R", 11.75f, 14f, 52f, 52.5f, 0f, 4.7f, wall);
            B(level, "Control_West", -6.5f, -6f, 52f, 64f, 0f, 4.7f, wall);
            B(level, "Control_Ceiling", -6.5f, 14f, 52f, 64f, 4.4f, 4.7f, wall);
            B(level, "Table_A", 4f, 8f, 55f, 56.2f, 0f, 0.9f, Console);
            B(level, "Table_B", 9f, 13f, 59f, 60.2f, 0f, 0.9f, Console);
            B(level, "ServerRack", 12.5f, 13.9f, 54f, 58f, 0f, 2.3f, Machine);

            var consoleObj = B(level, "Control_Console", -2f, 2f, 62.2f, 63.6f, 0f, 1f, Console);
            var screen = B(level, "Console_Screen", -1.6f, 1.6f, 63.2f, 63.5f, 1f, 1.7f, Screen);
            screen.transform.SetParent(consoleObj.transform, true);
            var oc = consoleObj.AddComponent<ObjectiveConsole>(); oc.screen = screen.GetComponent<Renderer>();
        }

        static Light AddLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range, bool spot, bool shadows, bool flicker, bool fixture)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = pos;
            var l = g.AddComponent<Light>();
            l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (spot) { l.type = LightType.Spot; l.spotAngle = 95f; g.transform.rotation = Quaternion.Euler(90f, 0f, 0f); }
            else l.type = LightType.Point;
            if (flicker) g.AddComponent<FlickerLight>();
            if (fixture) Part(g.transform, "Fixture", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.5f, 0.1f, 0.2f), Vector3.zero, Lamp);
            return l;
        }

        static void BuildLighting()
        {
            var root = new GameObject("Lighting").transform;
            var moon = new GameObject("Moon"); moon.transform.SetParent(root);
            moon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var ml = moon.AddComponent<Light>(); ml.type = LightType.Directional; ml.color = new Color(0.35f, 0.45f, 0.7f); ml.intensity = 0.25f; ml.shadows = LightShadows.Soft;

            var sodium = new Color(1f, 0.8f, 0.5f);
            // Deck floodlights (small pools in a dark deck)
            AddLight(root, "Deck_Lamp_Start", new Vector3(0, 4.2f, 3), sodium, 3f, 9f, true, false, false, true);
            AddLight(root, "Deck_Lamp_Mid", new Vector3(-4, 4.2f, 12), sodium, 3f, 9f, true, false, true, true);
            AddLight(root, "Deck_Lamp_North", new Vector3(0, 4.2f, 19), sodium, 3f, 9f, true, false, false, true);
            // Engine room: dim and flickering
            AddLight(root, "Engine_Lamp_A", new Vector3(-6, 4.1f, 35), new Color(0.6f, 1f, 0.7f), 2.2f, 9f, false, true, false, true);
            AddLight(root, "Engine_Lamp_B", new Vector3(8, 4.1f, 38), new Color(0.9f, 0.9f, 0.7f), 2f, 8f, false, true, true, true);
            // Corridor: red emergency light
            AddLight(root, "Corridor_Emergency", new Vector3(10, 3f, 46), new Color(1f, 0.1f, 0.05f), 2.5f, 7f, false, true, true, false);
            // Control room: bright, high contrast
            var white = new Color(0.85f, 0.95f, 1f);
            AddLight(root, "Control_Light_A", new Vector3(-1, 4.1f, 56), white, 4f, 11f, false, true, false, true);
            AddLight(root, "Control_Light_B", new Vector3(6, 4.1f, 57), white, 4f, 11f, false, true, false, true);
            AddLight(root, "Control_Light_C", new Vector3(10, 4.1f, 61), white, 4f, 11f, false, true, false, true);
            AddLight(root, "Console_Glow", new Vector3(0, 1.9f, 61.5f), new Color(0.2f, 1f, 0.5f), 2f, 5f, false, false, false, false);
        }

        static Transform[] Route(Transform routes, string name, params Vector2[] pts)
        {
            var list = new List<Transform>();
            for (int i = 0; i < pts.Length; i++)
            {
                var t = new GameObject(name + "_P" + i).transform; t.SetParent(routes);
                t.position = new Vector3(pts[i].x, 0f, pts[i].y); list.Add(t);
            }
            return list.ToArray();
        }

        static void SpawnEnemy(Transform routes, string name, float wait, params Vector2[] pts)
        {
            var e = (GameObject)PrefabUtility.InstantiatePrefab(_enemyPrefab);
            e.name = name; e.transform.position = new Vector3(pts[0].x, 0f, pts[0].y);
            var ai = e.GetComponent<EnemyAI>();
            ai.patrolPoints = Route(routes, name, pts); ai.patrolWaitTime = wait;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
        }

        static void BuildEnemies(Transform routes)
        {
            SpawnEnemy(routes, "Guard_Deck", 2.5f, new Vector2(0, 14), new Vector2(0, 22), new Vector2(-4, 19));
            SpawnEnemy(routes, "Guard_Engine", 2f, new Vector2(-6, 36), new Vector2(2, 28), new Vector2(5, 36));
            SpawnEnemy(routes, "Guard_Corridor", 4f, new Vector2(10, 44), new Vector2(10, 50));
            SpawnEnemy(routes, "Guard_Control", 3f, new Vector2(8, 57), new Vector2(2, 57), new Vector2(3, 60.5f));
        }

        static void SpawnCamera(string name, Vector3 pos, float yaw, float sweep, float speed, float range, float fov)
        {
            var c = (GameObject)PrefabUtility.InstantiatePrefab(_cameraPrefab);
            c.name = name; c.transform.position = pos; c.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var sc = c.GetComponent<SecurityCamera>();
            sc.sweepAngle = sweep; sc.sweepSpeed = speed; sc.viewRange = range; sc.fieldOfView = fov;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sc);
        }

        static void BuildCameras()
        {
            SpawnCamera("Camera_Deck", new Vector3(5.3f, 2.95f, 10f), 270f, 40f, 18f, 14f, 55f);
            SpawnCamera("Camera_Engine", new Vector3(13.6f, 3.8f, 26f), 315f, 35f, 16f, 18f, 50f);
            SpawnCamera("Camera_Corridor", new Vector3(10f, 3f, 41.9f), 0f, 20f, 15f, 11f, 35f);
            SpawnCamera("Camera_Control", new Vector3(13.6f, 3.7f, 63.4f), 225f, 40f, 16f, 18f, 55f);
        }

        // ------------------------------------------------------------------ HUD
        static Font _font;
        static RectTransform Rect(Transform parent, string name, params System.Type[] comps)
        {
            var types = new List<System.Type> { typeof(RectTransform) }; types.AddRange(comps);
            var g = new GameObject(name, types.ToArray()); g.transform.SetParent(parent, false);
            return g.GetComponent<RectTransform>();
        }
        static Image FullImage(Transform parent, string name, Color c)
        {
            var r = Rect(parent, name, typeof(Image));
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var img = r.GetComponent<Image>(); img.color = c; img.raycastTarget = false; return img;
        }
        static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            var r = Rect(parent, name, typeof(Text));
            r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = size;
            var t = r.GetComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.alignment = align; t.color = new Color(0.85f, 0.95f, 0.85f, 0.9f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        static void BuildHUD(GameObject player)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            var t = canvasGo.transform;
            var hud = canvasGo.AddComponent<HUD>();

            hud.damageOverlay = FullImage(t, "DamageOverlay", new Color(0.8f, 0f, 0f, 0f));

            // Night-vision overlay
            var nvCtl = new GameObject("NightVisionOverlay"); nvCtl.transform.SetParent(t, false);
            var nvRoot = Rect(t, "NV_Root"); nvRoot.anchorMin = Vector2.zero; nvRoot.anchorMax = Vector2.one; nvRoot.offsetMin = nvRoot.offsetMax = Vector2.zero;
            FullImage(nvRoot, "Tint", new Color(0.2f, 1f, 0.3f, 0.13f));
            var vig = FullImage(nvRoot, "Vignette", Color.white);
            var noiseR = Rect(nvRoot, "Grain", typeof(RawImage)); noiseR.anchorMin = Vector2.zero; noiseR.anchorMax = Vector2.one; noiseR.offsetMin = noiseR.offsetMax = Vector2.zero;
            var noise = noiseR.GetComponent<RawImage>(); noise.color = new Color(0.4f, 1f, 0.5f, 0.07f); noise.raycastTarget = false;
            var nvo = nvCtl.AddComponent<NightVisionOverlay>(); nvo.root = nvRoot.gameObject; nvo.vignette = vig; nvo.noise = noise;

            hud.flashOverlay = FullImage(t, "FlashOverlay", new Color(1f, 1f, 1f, 0f));

            var ch = Rect(t, "Crosshair", typeof(Image)); ch.anchorMin = ch.anchorMax = new Vector2(0.5f, 0.5f); ch.sizeDelta = new Vector2(4, 4);
            hud.crosshair = ch.GetComponent<Image>(); hud.crosshair.color = new Color(1f, 1f, 1f, 0.6f); hud.crosshair.raycastTarget = false;

            hud.healthText = MakeText(t, "Health", new Vector2(0, 0), new Vector2(40, 40), new Vector2(400, 40), 28, TextAnchor.LowerLeft);
            hud.weaponText = MakeText(t, "Weapon", new Vector2(1, 0), new Vector2(-40, 85), new Vector2(700, 36), 24, TextAnchor.LowerRight);
            hud.ammoText = MakeText(t, "Ammo", new Vector2(1, 0), new Vector2(-40, 40), new Vector2(700, 40), 30, TextAnchor.LowerRight);
            hud.grenadeText = MakeText(t, "Grenades", new Vector2(1, 0), new Vector2(-40, 135), new Vector2(400, 70), 22, TextAnchor.LowerRight);
            hud.objectiveText = MakeText(t, "Objective", new Vector2(0, 1), new Vector2(40, -30), new Vector2(1000, 36), 24, TextAnchor.UpperLeft);
            hud.notificationText = MakeText(t, "Notification", new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(900, 50), 34, TextAnchor.UpperCenter);
            hud.notificationText.rectTransform.pivot = new Vector2(0.5f, 1);
            hud.promptText = MakeText(t, "Prompt", new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(900, 40), 26, TextAnchor.MiddleCenter);
            hud.promptText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var bar = Rect(t, "HoldBar", typeof(Image)); bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f); bar.anchoredPosition = new Vector2(0, -165); bar.sizeDelta = new Vector2(300, 8);
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f); bar.GetComponent<Image>().raycastTarget = false;
            var fill = FullImage(bar, "Fill", new Color(0.4f, 1f, 0.5f, 0.95f));
            hud.holdBar = bar.gameObject; hud.holdFill = fill;

            var endImg = FullImage(t, "EndPanel", new Color(0, 0, 0, 0.75f));
            hud.endPanel = endImg.gameObject;
            hud.endTitle = MakeText(endImg.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1000, 100), 64, TextAnchor.MiddleCenter);
            hud.endTitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hud.endSubtitle = MakeText(endImg.transform, "Subtitle", new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(1000, 160), 30, TextAnchor.UpperCenter);
            hud.endSubtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            endImg.gameObject.SetActive(false);

            hud.playerHealth = player.GetComponent<Health>();
            hud.player = player.GetComponent<PlayerController>();
            hud.weapons = player.GetComponent<WeaponManager>();
            hud.grenades = player.GetComponent<GrenadeThrower>();
            hud.interactor = player.GetComponent<PlayerInteractor>();
        }
    }
}
