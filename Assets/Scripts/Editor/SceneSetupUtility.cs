using System.Collections.Generic;
using MayorOfMedieval.Building;
using MayorOfMedieval.Character;
using MayorOfMedieval.Core;
using MayorOfMedieval.Economy;
using MayorOfMedieval.Environment;
using MayorOfMedieval.NPC;
using MayorOfMedieval.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MayorOfMedieval.EditorUtils
{
    /// <summary>
    /// One-click rebuild of the whole playable: prefabs, world layout, build pads,
    /// resource nodes and HUD. Re-running it wipes what it made last time, so it is
    /// safe to iterate on.
    /// </summary>
    public static class SceneSetupUtility
    {
        private const string PrefabFolder = "Assets/Prefabs/Gameplay";
        private const string MaterialFolder = "Assets/Materials/Gameplay";
        private const string GeneratedRoot = "--- GAMEPLAY ---";

        [MenuItem("MayorOfMedieval/Delete Save (start fresh)")]
        public static void ClearSave()
        {
            SaveManager.DeleteSave();
            Debug.Log("[SceneSetup] Save cleared — the next Play starts a brand new village.");
        }

        [MenuItem("MayorOfMedieval/Build Playable World")]
        public static void BuildWorld()
        {
            EnsureFolder("Assets/Prefabs", "Gameplay");
            EnsureFolder("Assets/Materials", "Gameplay");

            CleanUp();

            Materials mats = CreateMaterials();
            Prefabs prefabs = CreatePrefabs(mats);

            GameObject root = new GameObject(GeneratedRoot);

            SetupManagers();
            SetupLighting();
            SetupPlayer(mats);
            SetupGroundDetail(root.transform);
            SetupWorldNodes(root.transform, prefabs);
            SetupBuildPads(root.transform, prefabs, mats);
            SetupCustomerRoute(root.transform, prefabs);
            SetupHUD();
            SetupQuestArrow(mats);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SceneSetup] Playable world rebuilt.");
        }

        // ---------------------------------------------------------------- cleanup

        private static void CleanUp()
        {
            string[] names =
            {
                GeneratedRoot, "HUDCanvas", "TopPanel", "QuestArrow",
                "BuildingSlot_Hut", "BuildingSlot_Farm", "PlaceholderBuilding",
                "ExpansionZone_West", "Border_Decorations", "BuildingPlacer",
                "TutorialWaypointArrow", "CustomerSpawner"
            };

            foreach (string name in names)
            {
                GameObject found;
                while ((found = GameObject.Find(name)) != null) Object.DestroyImmediate(found);
            }

            GameObject world = GameObject.Find("------- WORLD -------");
            if (world == null) return;

            // Empty out the old containers...
            foreach (string child in new[] { "Buildings", "Decorations", "NPCs" })
            {
                Transform container = world.transform.Find(child);
                if (container == null) continue;
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(container.GetChild(i).gameObject);
                }
            }

            // ...and drop the groups left behind by the pre-redesign scene entirely. These
            // held capsule "cows" and seed spheres that look like scenery but do nothing.
            foreach (string dead in new[] { "AnimalsGroup", "SeedsGroup", "GridSystem" })
            {
                Transform stale = world.transform.Find(dead);
                if (stale != null) Object.DestroyImmediate(stale.gameObject);
            }
        }

        // -------------------------------------------------------------- materials

        private class Materials
        {
            public Material Wood, Stone, Leaf, Animal, Grain, Bread, Water, Sword, Beer;
            public Material Pad, Wall, Roof, Player, Worker, Customer, Soldier, Dirt, Enemy, Marble;
        }

        private static Materials CreateMaterials()
        {
            return new Materials
            {
                Wood = Mat("M_Wood", GameConfig.ColorOf(ResourceType.Wood)),
                Stone = Mat("M_Stone", GameConfig.ColorOf(ResourceType.Stone)),
                Leaf = Mat("M_Leaf", new Color(0.22f, 0.55f, 0.24f)),
                Animal = Mat("M_Animal", new Color(0.92f, 0.88f, 0.82f)),
                Grain = Mat("M_Grain", GameConfig.ColorOf(ResourceType.Grain)),
                Bread = Mat("M_Bread", GameConfig.ColorOf(ResourceType.Bread)),
                Water = Mat("M_Water", GameConfig.ColorOf(ResourceType.Water)),
                Sword = Mat("M_Sword", GameConfig.ColorOf(ResourceType.Sword)),
                Beer = Mat("M_Beer", GameConfig.ColorOf(ResourceType.Beer)),
                Pad = Mat("M_Pad", new Color(0.35f, 0.82f, 0.40f)),
                Wall = Mat("M_Wall", new Color(0.86f, 0.78f, 0.60f)),
                Roof = Mat("M_Roof", new Color(0.62f, 0.25f, 0.20f)),
                Player = Mat("M_Player", new Color(0.88f, 0.26f, 0.22f)),
                Worker = Mat("M_Worker", new Color(0.30f, 0.50f, 0.85f)),
                Customer = Mat("M_Customer", new Color(0.70f, 0.40f, 0.80f)),
                Soldier = Mat("M_Soldier", new Color(0.35f, 0.40f, 0.62f)),
                Dirt = Mat("M_Dirt", new Color(0.42f, 0.32f, 0.20f)),
                Enemy = Mat("M_Enemy", new Color(0.45f, 0.30f, 0.32f)),
                Marble = Mat("M_Marble", new Color(0.90f, 0.89f, 0.85f))
            };
        }

        private static Material Mat(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(shader);
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------------------------------------------------------------- prefabs

        private class Prefabs
        {
            public GameObject Tree, Rock, Animal, Worker, Customer, Soldier, Dummy;
            public GameObject Market, LumberCamp, Quarry, Farm, CropField, Well, Mill;
            public GameObject Treasury, Blacksmith, Barracks, Inn, VillageSquare, Church;
        }

        private static Prefabs CreatePrefabs(Materials m)
        {
            Prefabs p = new Prefabs();

            p.Tree = SavePrefab(BuildTree(m), "P_Tree");
            p.Rock = SavePrefab(BuildRock(m), "P_Rock");
            p.Animal = SavePrefab(BuildAnimal(m), "P_Animal");
            p.Worker = SavePrefab(BuildWorker(m), "P_Worker");
            p.Customer = SavePrefab(BuildCustomer(m), "P_Customer");
            p.Soldier = SavePrefab(BuildSoldier(m), "P_Soldier");
            p.Dummy = SavePrefab(BuildDummy(m), "P_TrainingDummy");

            p.Market = SavePrefab(BuildMarket(m), "P_Market");
            p.LumberCamp = SavePrefab(BuildGatherCamp(m, "LumberCamp", ResourceType.Wood, p.Worker, "ODUNCU"), "P_LumberCamp");
            p.Quarry = SavePrefab(BuildGatherCamp(m, "Quarry", ResourceType.Stone, p.Worker, "TAS OCAGI"), "P_Quarry");
            p.Farm = SavePrefab(BuildFarm(m, p.Worker), "P_Farm");
            p.CropField = SavePrefab(BuildCropField(m), "P_CropField");
            p.Well = SavePrefab(BuildWell(m), "P_Well");
            p.Mill = SavePrefab(BuildMill(m, p.Worker), "P_Mill");
            p.Treasury = SavePrefab(BuildTreasury(m, p.Worker), "P_Treasury");
            p.Blacksmith = SavePrefab(BuildBlacksmith(m, p.Worker), "P_Blacksmith");
            p.Barracks = SavePrefab(BuildBarracks(m, p.Worker, p.Soldier), "P_Barracks");
            p.Inn = SavePrefab(BuildInn(m, p.Worker), "P_Inn");
            p.VillageSquare = SavePrefab(BuildVillageSquare(m), "P_VillageSquare");
            p.Church = SavePrefab(BuildChurch(m), "P_Church");

            return p;
        }

        private static GameObject SavePrefab(GameObject source, string name)
        {
            string path = PrefabFolder + "/" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return saved;
        }

        // ------------------------------------------------------------ shape helpers

        private static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
            => Shape(GameObject.CreatePrimitive(PrimitiveType.Cube), name, parent, pos, scale, mat, collider);

        private static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
            => Shape(GameObject.CreatePrimitive(PrimitiveType.Cylinder), name, parent, pos, scale, mat, collider);

        private static GameObject Sphere(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = false)
            => Shape(GameObject.CreatePrimitive(PrimitiveType.Sphere), name, parent, pos, scale, mat, collider);

        private static GameObject Shape(GameObject go, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider)
        {
            go.name = name;
            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        private static TextMeshPro Label(Transform parent, Vector3 localPos, string text, float size)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            // TMP wires up its own MeshFilter/MeshRenderer — never pre-add them.
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
            tmp.rectTransform.localPosition = localPos;
            tmp.rectTransform.localRotation = Quaternion.LookRotation(new Vector3(-15f, -20f, 15f).normalized, Vector3.up);
            tmp.ForceMeshUpdate(true, true);
            return tmp;
        }

        // ------------------------------------------------------------ Kenney models

        private const string ModelRoot = "Assets/Models/Kenney/";

        /// <summary>Loads a Kenney FBX, e.g. Model("FantasyTown/wall-door").</summary>
        private static GameObject Model(string relativePath)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + relativePath + ".fbx");
            if (go == null) Debug.LogWarning("[SceneSetup] Missing model: " + relativePath);
            return go;
        }

        /// <summary>Drops one Kenney piece into a parent at a grid position.</summary>
        private static GameObject Piece(string relativePath, Transform parent, Vector3 localPos,
            float yaw = 0f, float scale = 1f)
        {
            GameObject src = Model(relativePath);
            if (src == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = System.IO.Path.GetFileName(relativePath);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            inst.transform.localScale = Vector3.one * scale;
            return inst;
        }

        /// <summary>
        /// Assembles a walled building on Kenney's 1-unit module grid. Walls sit on the
        /// -X face of their tile, so each side just needs the matching yaw:
        /// 0 = west, 180 = east, 90 = north (+Z), -90 = south (-Z).
        /// </summary>
        private static void RaiseWalls(Transform parent, int tilesX, int tilesZ, string wallModel,
            string doorModel, string roofModel, int storeys)
        {
            float halfX = (tilesX - 1) * 0.5f;
            float halfZ = (tilesZ - 1) * 0.5f;

            // Door goes on the middle tile of the south face (the side customers approach).
            int doorTile = tilesX / 2;

            for (int storey = 0; storey < storeys; storey++)
            {
                float y = storey;
                for (int x = 0; x < tilesX; x++)
                {
                    float px = x - halfX;
                    bool isDoor = storey == 0 && x == doorTile && doorModel != null;
                    Piece(isDoor ? doorModel : wallModel, parent, new Vector3(px, y, -halfZ - 0.5f), -90f);
                    Piece(wallModel, parent, new Vector3(px, y, halfZ + 0.5f), 90f);
                }
                for (int z = 0; z < tilesZ; z++)
                {
                    float pz = z - halfZ;
                    Piece(wallModel, parent, new Vector3(-halfX - 0.5f, y, pz), 0f);
                    Piece(wallModel, parent, new Vector3(halfX + 0.5f, y, pz), 180f);
                }
            }

            if (roofModel == null) return;

            // One continuous gable running along X, stretched to cover the depth. Tiling the
            // gable in both directions would give a row of parallel ridges, not a roof.
            bool pointRoof = roofModel.Contains("point");
            for (int x = 0; x < tilesX; x++)
            {
                GameObject tile = Piece(roofModel, parent, new Vector3(x - halfX, storeys, 0f));
                if (tile != null && !pointRoof) tile.transform.localScale = new Vector3(1f, 1f, tilesZ);
            }
        }

        // -------------------------------------------------------- world node prefabs

        private static GameObject BuildTree(Materials m)
        {
            GameObject root = new GameObject("Tree");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Piece("FantasyTown/tree-high", visual.transform, Vector3.zero, Random.Range(0f, 360f));

            // Solid trunk so the Lord walks around the tree instead of through it.
            CapsuleCollider trunk = root.AddComponent<CapsuleCollider>();
            trunk.center = new Vector3(0f, 1f, 0f);
            trunk.radius = 0.32f;
            trunk.height = 2f;

            HarvestNode node = root.AddComponent<HarvestNode>();
            SetPrivate(node, "shakeRoot", visual.transform);
            SetPrivate(node, "resourceType", ResourceType.Wood);
            SetPrivate(node, "unitsPerNode", 3);
            SetPrivate(node, "respawnSeconds", 10f);
            return root;
        }

        private static GameObject BuildRock(Materials m)
        {
            GameObject root = new GameObject("Rock");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Piece("FantasyTown/rock-large", visual.transform, Vector3.zero, Random.Range(0f, 360f));
            Piece("FantasyTown/rock-small", visual.transform, new Vector3(0.7f, 0f, -0.5f), Random.Range(0f, 360f));

            BoxCollider boulder = root.AddComponent<BoxCollider>();
            boulder.center = new Vector3(0.1f, 0.5f, 0f);
            boulder.size = new Vector3(1.8f, 1f, 1.5f);

            HarvestNode node = root.AddComponent<HarvestNode>();
            SetPrivate(node, "shakeRoot", visual.transform);
            SetPrivate(node, "resourceType", ResourceType.Stone);
            SetPrivate(node, "unitsPerNode", 3);
            SetPrivate(node, "respawnSeconds", 12f);
            return root;
        }

        private static GameObject BuildAnimal(Materials m)
        {
            GameObject root = new GameObject("Animal");
            GameObject body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);

            // Kenney has no livestock model, so the sheep stays hand-built — but it now
            // matches the kit's palette instead of the old placeholder colours.
            Cube("Torso", body.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.62f, 0.55f, 1.0f), m.Animal);
            Sphere("Head", body.transform, new Vector3(0f, 0.88f, 0.62f), new Vector3(0.44f, 0.44f, 0.44f), m.Animal);
            Sphere("Wool", body.transform, new Vector3(0f, 0.86f, -0.1f), new Vector3(0.7f, 0.55f, 0.8f), m.Animal);
            Cube("LegA", body.transform, new Vector3(0.2f, 0.18f, 0.3f), new Vector3(0.12f, 0.36f, 0.12f), m.Wood);
            Cube("LegB", body.transform, new Vector3(-0.2f, 0.18f, 0.3f), new Vector3(0.12f, 0.36f, 0.12f), m.Wood);
            Cube("LegC", body.transform, new Vector3(0.2f, 0.18f, -0.3f), new Vector3(0.12f, 0.36f, 0.12f), m.Wood);
            Cube("LegD", body.transform, new Vector3(-0.2f, 0.18f, -0.3f), new Vector3(0.12f, 0.36f, 0.12f), m.Wood);

            HarvestNode node = root.AddComponent<HarvestNode>();
            SetPrivate(node, "resourceType", ResourceType.Meat);
            SetPrivate(node, "unitsPerNode", 2);
            SetPrivate(node, "secondsPerUnit", 0.9f);
            SetPrivate(node, "respawnSeconds", 14f);
            SetPrivate(node, "shakeRoot", body.transform);
            SetPrivate(node, "wanders", true);
            SetPrivate(node, "wanderRadius", 6f);
            return root;
        }

        /// <summary>Kenney blocky character. Models are ~2.7 units tall, so they get scaled down.</summary>
        private static GameObject BuildCharacterBody(string name, string characterModel)
        {
            GameObject root = new GameObject(name);
            Piece("Characters/" + characterModel, root.transform, Vector3.zero, 0f, 0.62f);
            return root;
        }

        private static GameObject BuildWorker(Materials m)
        {
            GameObject root = BuildCharacterBody("Worker", "character-d");
            CarrySystem carry = root.AddComponent<CarrySystem>();
            SetPrivate(carry, "capacity", GameConfig.WorkerCarryCapacity);
            root.AddComponent<CarrierBeacon>();
            root.AddComponent<Worker>();
            return root;
        }

        private static GameObject BuildCustomer(Materials m)
        {
            GameObject root = BuildCharacterBody("Customer", "character-h");
            root.AddComponent<Customer>();
            return root;
        }

        private static GameObject BuildSoldier(Materials m)
        {
            GameObject root = BuildCharacterBody("Soldier", "character-n");
            Piece("FantasyTown/blade", root.transform, new Vector3(0.34f, 0.75f, 0.15f), 0f, 0.9f);
            root.AddComponent<Soldier>();
            return root;
        }

        private static GameObject BuildDummy(Materials m)
        {
            GameObject root = new GameObject("TrainingDummy");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            Piece("MiniForest/target", visual.transform, Vector3.zero, 0f, 2.2f);

            TrainingDummy dummy = root.AddComponent<TrainingDummy>();
            SetPrivate(dummy, "visualRoot", visual.transform);
            return root;
        }

        // ---------------------------------------------------------- building helpers

        /// <summary>
        /// Real medieval house built from Kenney modules. The old primitive signature is
        /// kept so every call site keeps working: size.x/size.z become tile counts.
        /// </summary>
        private static GameObject BuildHut(string name, Materials m, Vector3 size)
        {
            int tilesX = Mathf.Max(2, Mathf.RoundToInt(size.x));
            int tilesZ = Mathf.Max(2, Mathf.RoundToInt(size.z));
            // Two storeys minimum. A single 1-unit wall under a 0.57-unit gable reads as
            // "all roof, no building" from the isometric camera.
            int storeys = size.y >= 1.7f ? 3 : 2;

            GameObject root = new GameObject(name);
            GameObject shell = new GameObject("Shell");
            shell.transform.SetParent(root.transform, false);

            bool woodenWalls = name == "LumberCamp" || name == "Farm" || name == "Inn";
            string wall = woodenWalls ? "FantasyTown/wall-wood" : "FantasyTown/wall";
            string door = woodenWalls ? "FantasyTown/wall-wood-door" : "FantasyTown/wall-door";

            RaiseWalls(shell.transform, tilesX, tilesZ, wall, door, "FantasyTown/roof-gable", storeys);

            // A blocking collider so the Lord can't walk through the building.
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, storeys * 0.5f, 0f);
            box.size = new Vector3(tilesX, storeys, tilesZ);
            return root;
        }

        private static Stockpile AddStockpile(GameObject parent, string name, Vector3 pos, ResourceType type,
            int capacity, bool withdraw, bool deposit, bool supplySource = false, int reserve = 0)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;

            Stockpile pile = go.AddComponent<Stockpile>();
            SetPrivate(pile, "resourceType", type);
            SetPrivate(pile, "capacity", capacity);
            SetPrivate(pile, "playerCanWithdraw", withdraw);
            SetPrivate(pile, "playerCanDeposit", deposit);
            SetPrivate(pile, "isSupplySource", supplySource);
            SetPrivate(pile, "reserveForProduction", reserve);
            return pile;
        }

        private static ServiceCounter AddCounter(GameObject parent, Vector3 pos, params ResourceType[] goods)
        {
            GameObject go = new GameObject("ServiceCounter");
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;

            ServiceCounter counter = go.AddComponent<ServiceCounter>();
            SetPrivate(counter, "acceptedGoods", new List<ResourceType>(goods));
            SetPrivate(counter, "queueDirection", new Vector3(0f, 0f, -1f));
            return counter;
        }

        /// <summary>Turns a building into a shop: shelves the player stocks + a till.</summary>
        private static SalesPoint AddSalesPoint(GameObject parent, Vector3 tillOffset, params Stockpile[] shelves)
        {
            SalesPoint shop = parent.AddComponent<SalesPoint>();
            SetPrivate(shop, "shelves", new List<Stockpile>(shelves));

            GameObject till = new GameObject("TillAnchor");
            till.transform.SetParent(parent.transform, false);
            till.transform.localPosition = tillOffset;
            SetPrivate(shop, "coinAnchor", till.transform);

            // A big green collection pad next to the counter, like the reference games.
            // The takings used to pile up behind the building, so the player had to walk
            // around the back to notice — let alone collect — them.
            GameObject pad = Cylinder("CollectPad", till.transform, new Vector3(0f, 0.015f, 0f),
                new Vector3(2.6f, 0.015f, 2.6f), Mat("M_CollectPad", new Color(0.36f, 0.84f, 0.42f)));
            pad.name = "CollectPad";
            return shop;
        }

        private static WorkerStation AddStation(GameObject parent, GameObject workerPrefab, WorkerRole[] roles,
            ResourceType cargo, Stockpile pile, Vector3 padOffset, ProductionBuilding workshop = null)
        {
            WorkerStation station = parent.AddComponent<WorkerStation>();
            SetPrivate(station, "workerPrefab", workerPrefab);
            SetPrivate(station, "hireOrder", roles);
            SetPrivate(station, "cargoType", cargo);
            SetPrivate(station, "stockpile", pile);
            SetPrivate(station, "padOffset", padOffset);
            if (workshop != null) SetPrivate(station, "workshop", workshop);
            return station;
        }

        // ---------------------------------------------------------- building prefabs

        private static GameObject BuildMarket(Materials m)
        {
            GameObject root = BuildHut("Market", m, new Vector3(5f, 1.5f, 3f));
            for (int i = -2; i <= 2; i++)
            {
                Piece(i % 2 == 0 ? "FantasyTown/stall-red" : "FantasyTown/stall-green",
                    root.transform, new Vector3(i, 0f, -2.6f), 180f);
            }
            Piece("FantasyTown/stall-stool", root.transform, new Vector3(3.1f, 0f, -2.1f));

            // The Market is the village's only shop: every good in the game is sold here.
            // Production buildings just stockpile and their Carriers haul goods over. When
            // a building sold its own output, its carrier moved goods from its own pile to
            // its own shelf — a pointless round trip that never reached a customer.
            ResourceType[] goods =
            {
                ResourceType.Wood, ResourceType.Stone, ResourceType.Meat,
                ResourceType.Grain, ResourceType.Bread, ResourceType.Sword, ResourceType.Beer
            };

            List<Stockpile> shelves = new List<Stockpile>();
            for (int i = 0; i < goods.Length; i++)
            {
                shelves.Add(AddStockpile(root, goods[i] + "Shelf",
                    new Vector3(-3f + i, 0f, -2.0f), goods[i], 10, false, true));
            }

            AddSalesPoint(root, new Vector3(4.8f, 0f, -2.4f), shelves.ToArray());
            AddCounter(root, new Vector3(0f, 0f, -3.6f), goods);
            Label(root.transform, new Vector3(0f, 4.6f, 0f), "PAZAR", 4f);
            return root;
        }

        private static GameObject BuildGatherCamp(Materials m, string name, ResourceType type, GameObject workerPrefab, string caption)
        {
            GameObject root = BuildHut(name, m, new Vector3(2.2f, 1.4f, 2.2f));

            // Player can scoop from here, and it doubles as a depot other workers draw from.
            // Stone keeps a reserve so the Blacksmith is never starved by the market run.
            int reserve = type == ResourceType.Stone ? 6 : 0;
            Stockpile pile = AddStockpile(root, "Stockpile", new Vector3(-1.9f, 0f, 0f), type, 24, true, false, true, reserve);

            AddStation(root, workerPrefab,
                new[] { WorkerRole.Harvester, WorkerRole.Carrier },
                type, pile, new Vector3(2.2f, 0f, 0f));

            Label(root.transform, new Vector3(0f, 4.4f, 0f), caption, 3.5f);
            return root;
        }

        private static GameObject BuildFarm(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Farm", m, new Vector3(2.8f, 1.5f, 2.4f));

            Stockpile pile = AddStockpile(root, "MeatPile", new Vector3(-2.3f, 0f, 0f), ResourceType.Meat, 16, true, false, true);
            AddStation(root, workerPrefab,
                new[] { WorkerRole.Harvester, WorkerRole.Carrier },
                ResourceType.Meat, pile, new Vector3(2.6f, 0f, 0f));

            return root;
        }

        private static GameObject BuildCropField(Materials m)
        {
            GameObject root = new GameObject("CropField");
            Cube("Soil", root.transform, new Vector3(0f, 0.05f, 0f), new Vector3(5f, 0.1f, 4f), m.Dirt, true);

            List<Transform> crops = new List<Transform>();
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    GameObject crop = Cube("Crop", root.transform,
                        new Vector3(-1.5f + x, 0.35f, -1f + z), new Vector3(0.35f, 0.6f, 0.35f), m.Grain);
                    crops.Add(crop.transform);
                }
            }

            GameObject dry = Cube("DryOverlay", root.transform, new Vector3(0f, 0.12f, 0f), new Vector3(5.05f, 0.05f, 4.05f), m.Dirt);

            // Reserve keeps the Mill and Inn supplied before grain goes to the shelf.
            Stockpile pile = AddStockpile(root, "GrainPile", new Vector3(-3.2f, 0f, 0f), ResourceType.Grain, 24, true, false, true, 8);

            CropField field = root.AddComponent<CropField>();
            SetPrivate(field, "output", pile);
            SetPrivate(field, "dryOverlay", dry);
            SetPrivate(field, "cropVisuals", crops.ToArray());

            Label(root.transform, new Vector3(0f, 2.6f, 0f), "TARLA", 3.5f);
            return root;
        }

        private static GameObject BuildWell(Materials m)
        {
            GameObject root = new GameObject("Well");
            // Stone rim from the fountain piece, timber posts and a little gable on top.
            Piece("FantasyTown/fountain-square", root.transform, Vector3.zero);
            Piece("FantasyTown/pillar-wood", root.transform, new Vector3(0.45f, 0.2f, 0f));
            Piece("FantasyTown/pillar-wood", root.transform, new Vector3(-0.45f, 0.2f, 0f));
            Piece("FantasyTown/roof-gable", root.transform, new Vector3(0f, 1.25f, 0f), 90f);
            Piece("FantasyTown/lantern", root.transform, new Vector3(1.1f, 0f, 0.8f));

            BoxCollider wellBox = root.AddComponent<BoxCollider>();
            wellBox.center = new Vector3(0f, 0.4f, 0f);
            wellBox.size = new Vector3(1.2f, 0.8f, 1.2f);

            // Buckets the Mill/Inn producers come and fetch.
            Stockpile buckets = AddStockpile(root, "WaterPile", new Vector3(-1.8f, 0f, 0f), ResourceType.Water, 12, true, false, true);

            WaterWell well = root.AddComponent<WaterWell>();
            SetPrivate(well, "waterPile", buckets);

            Label(root.transform, new Vector3(0f, 3.2f, 0f), "KUYU", 3.5f);
            return root;
        }

        private static GameObject BuildMill(Materials m, GameObject workerPrefab)
        {
            GameObject root = new GameObject("Mill");
            GameObject shell = new GameObject("Shell");
            shell.transform.SetParent(root.transform, false);
            // Two-storey stone tower topped with a point roof, then Kenney's real sails.
            RaiseWalls(shell.transform, 2, 2, "FantasyTown/wall", "FantasyTown/wall-door",
                "FantasyTown/roof-high-point", 2);

            BoxCollider millBox = root.AddComponent<BoxCollider>();
            millBox.center = new Vector3(0f, 1f, 0f);
            millBox.size = new Vector3(2f, 2f, 2f);

            GameObject sails = new GameObject("Sails");
            sails.transform.SetParent(root.transform, false);
            sails.transform.localPosition = new Vector3(0f, 2.4f, -1.15f);
            Piece("FantasyTown/windmill", sails.transform, Vector3.zero, 90f);

            Stockpile grainIn = AddStockpile(root, "GrainInput", new Vector3(-2.6f, 0f, 2.2f), ResourceType.Grain, 16, false, true);
            Stockpile waterIn = AddStockpile(root, "WaterInput", new Vector3(-2.6f, 0f, 0.4f), ResourceType.Water, 16, false, true);
            // Reserve feeds the Inn's brewer before bread is sent to the shelf.
            Stockpile breadOut = AddStockpile(root, "BreadOutput", new Vector3(-2.6f, 0f, -2.2f), ResourceType.Bread, 16, true, false, true, 5);

            ProductionBuilding mill = root.AddComponent<ProductionBuilding>();
            SetIngredients(mill, new[] { grainIn, waterIn });
            SetPrivate(mill, "output", breadOut);
            SetPrivate(mill, "spinner", sails.transform);

            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Bread, breadOut, new Vector3(2.4f, 0f, 1.8f), mill);

            Label(root.transform, new Vector3(0f, 5.2f, 0f), "DEGIRMEN", 3.5f);
            return root;
        }

        private static GameObject BuildTreasury(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Treasury", m, new Vector3(2.4f, 1.6f, 2.4f));
            Cube("GoldTrim", root.transform, new Vector3(0f, 1.75f, 0f), new Vector3(2.1f, 0.18f, 2.1f), m.Grain);
            Sphere("Orb", root.transform, new Vector3(0f, 2.35f, 0f), new Vector3(0.5f, 0.5f, 0.5f), m.Grain);

            // Two tax collectors, no cargo of their own — they just sweep the tills.
            AddStation(root, workerPrefab,
                new[] { WorkerRole.GoldCollector, WorkerRole.GoldCollector },
                ResourceType.Gold, null, new Vector3(2.4f, 0f, 0f));

            Label(root.transform, new Vector3(0f, 4.6f, 0f), "HAZINE", 3.5f);
            return root;
        }

        private static GameObject BuildBlacksmith(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Blacksmith", m, new Vector3(2.6f, 1.5f, 2.4f));
            Piece("FantasyTown/chimney", root.transform, new Vector3(1.5f, 1f, 0.5f), 180f);
            Piece("FantasyTown/chimney-top", root.transform, new Vector3(1.5f, 2f, 0.5f), 180f);
            GameObject anvil = Piece("FantasyTown/cart", root.transform, new Vector3(0f, 0f, -2.0f), 180f);

            Stockpile stoneIn = AddStockpile(root, "StoneInput", new Vector3(-2.4f, 0f, 1.4f), ResourceType.Stone, 16, false, true);
            // Reserve keeps the Barracks mustering even while swords are also being sold.
            Stockpile swordOut = AddStockpile(root, "SwordOutput", new Vector3(-2.4f, 0f, -1.4f), ResourceType.Sword, 16, true, false, true, 5);

            ProductionBuilding forge = root.AddComponent<ProductionBuilding>();
            SetIngredients(forge, new[] { stoneIn });
            SetPrivate(forge, "output", swordOut);
            SetPrivate(forge, "spinner", anvil.transform);
            SetPrivate(forge, "spinSpeed", 0f);
            SetPrivate(forge, "secondsPerCraft", 2.2f);

            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Sword, swordOut, new Vector3(2.4f, 0f, 1.6f), forge);

            Label(root.transform, new Vector3(0f, 4.6f, 0f), "DEMIRCI", 3.5f);
            return root;
        }

        private static GameObject BuildBarracks(Materials m, GameObject workerPrefab, GameObject soldierPrefab)
        {
            GameObject root = BuildHut("Barracks", m, new Vector3(3.2f, 1.7f, 2.6f));
            Piece("FantasyTown/banner-red", root.transform, new Vector3(-1.5f, 1.1f, -0.5f));
            Piece("FantasyTown/banner-red", root.transform, new Vector3(-1.5f, 1.1f, 0.5f));
            Piece("FantasyTown/fence", root.transform, new Vector3(0f, 0f, -2.2f), 90f);

            Stockpile swordIn = AddStockpile(root, "SwordInput", new Vector3(-2.8f, 0f, 0.8f), ResourceType.Sword, 16, false, true);

            GameObject spawn = new GameObject("SoldierSpawn");
            spawn.transform.SetParent(root.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 0f, -2.4f);

            Barracks barracks = root.AddComponent<Barracks>();
            SetPrivate(barracks, "swordInput", swordIn);
            SetPrivate(barracks, "soldierPrefab", soldierPrefab);
            SetPrivate(barracks, "spawnPoint", spawn.transform);

            // A producer keeps hauling swords over from the Blacksmith depot.
            ProductionBuilding feeder = root.AddComponent<ProductionBuilding>();
            SetIngredients(feeder, new[] { swordIn });
            SetPrivate(feeder, "output", (Object)null);
            AddStation(root, workerPrefab, new[] { WorkerRole.Producer },
                ResourceType.Sword, swordIn, new Vector3(2.8f, 0f, 0f), feeder);

            Label(root.transform, new Vector3(0f, 5.0f, 0f), "KISLA", 3.5f);
            return root;
        }

        private static GameObject BuildInn(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Inn", m, new Vector3(3.0f, 1.8f, 2.6f));
            Cube("Sign", root.transform, new Vector3(1.7f, 2.0f, -1.0f), new Vector3(0.1f, 0.6f, 0.9f), m.Wood);

            Stockpile grainIn = AddStockpile(root, "GrainInput", new Vector3(-2.8f, 0f, 2.0f), ResourceType.Grain, 14, false, true);
            Stockpile waterIn = AddStockpile(root, "WaterInput", new Vector3(-2.8f, 0f, 0.6f), ResourceType.Water, 14, false, true);
            Stockpile breadIn = AddStockpile(root, "BreadInput", new Vector3(-2.8f, 0f, -0.8f), ResourceType.Bread, 14, false, true);
            Stockpile beerOut = AddStockpile(root, "BeerOutput", new Vector3(-2.8f, 0f, -2.4f), ResourceType.Beer, 14, true, false, true);

            ProductionBuilding brewery = root.AddComponent<ProductionBuilding>();
            SetIngredients(brewery, new[] { grainIn, waterIn, breadIn });
            SetPrivate(brewery, "output", beerOut);
            SetPrivate(brewery, "secondsPerCraft", 4f);

            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Beer, beerOut, new Vector3(2.6f, 0f, 1.8f), brewery);

            Label(root.transform, new Vector3(0f, 5.0f, 0f), "HAN", 3.5f);
            return root;
        }

        private static GameObject BuildVillageSquare(Materials m)
        {
            GameObject root = new GameObject("VillageSquare");
            // Paved plaza out of road tiles, with Kenney's finished fountain in the middle.
            for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                    Piece("FantasyTown/road", root.transform, new Vector3(x, 0f, z));

            Piece("FantasyTown/fountain-round", root.transform, new Vector3(0f, 0.02f, 0f));
            Piece("FantasyTown/lantern", root.transform, new Vector3(-2f, 0f, -2f));
            Piece("FantasyTown/lantern", root.transform, new Vector3(2f, 0f, 2f));
            Piece("FantasyTown/cart", root.transform, new Vector3(2.1f, 0f, -1.6f), 35f);
            Piece("FantasyTown/hedge", root.transform, new Vector3(-2.1f, 0f, 1.4f));

            Label(root.transform, new Vector3(0f, 2.2f, 0f), "KOY MEYDANI", 3f);
            return root;
        }

        private static GameObject BuildChurch(Materials m)
        {
            GameObject root = new GameObject("Church");
            GameObject nave = new GameObject("Nave");
            nave.transform.SetParent(root.transform, false);
            RaiseWalls(nave.transform, 3, 4, "FantasyTown/wall", "FantasyTown/wall-door",
                "FantasyTown/roof-gable", 2);

            // Bell tower: a narrow 1x1 stack with a tall point roof on top.
            GameObject tower = new GameObject("Tower");
            tower.transform.SetParent(root.transform, false);
            tower.transform.localPosition = new Vector3(0f, 0f, 2.5f);
            RaiseWalls(tower.transform, 1, 1, "FantasyTown/wall", null, null, 4);
            Piece("FantasyTown/roof-high-point", tower.transform, new Vector3(0f, 4f, 0f));
            Piece("FantasyTown/banner-green", tower.transform, new Vector3(-0.5f, 2.6f, 0f));

            BoxCollider churchBox = root.AddComponent<BoxCollider>();
            churchBox.center = new Vector3(0f, 1f, 0.3f);
            churchBox.size = new Vector3(3f, 2f, 5f);

            Label(root.transform, new Vector3(0f, 7.6f, 0f), "KILISE", 3f);
            return root;
        }

        // ------------------------------------------------------------------ scene

        private static void SetupManagers()
        {
            GameObject gm = GameObject.Find("GameManager") ?? new GameObject("GameManager");
            if (gm.GetComponent<GameManager>() == null) gm.AddComponent<GameManager>();

            ResourceManager wallet = gm.GetComponent<ResourceManager>();
            if (wallet == null) wallet = gm.AddComponent<ResourceManager>();
            // A pre-existing component keeps whatever was serialised before, so force it.
            SetPrivate(wallet, "startingGold", GameConfig.StartingGold);

            if (gm.GetComponent<GameProgression>() == null) gm.AddComponent<GameProgression>();
            if (gm.GetComponent<SaveManager>() == null) gm.AddComponent<SaveManager>();

            RoadNetwork roads = gm.GetComponent<RoadNetwork>();
            if (roads == null) roads = gm.AddComponent<RoadNetwork>();
            SetPrivate(roads, "roadPiece", Model("FantasyTown/road"));

            if (gm.GetComponent<DailyQuests>() == null) gm.AddComponent<DailyQuests>();

            AudioManager audio = gm.GetComponent<AudioManager>();
            if (audio == null) audio = gm.AddComponent<AudioManager>();
            // Order must match the Sfx enum.
            string[] sfxNames =
            {
                "sfx_chop", "sfx_mine", "sfx_hunt", "sfx_coins", "sfx_sale",
                "sfx_build", "sfx_click", "sfx_toggle", "sfx_complete", "sfx_hire"
            };
            List<AudioClip> clips = new List<AudioClip>();
            foreach (string n in sfxNames)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + n + ".ogg");
                if (clip == null) Debug.LogWarning("[SceneSetup] Missing SFX: " + n);
                clips.Add(clip);
            }
            SetPrivate(audio, "sfxClips", clips);
            SetPrivate(audio, "musicLoop",
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/music_village_loop.wav"));
        }

        private static void SetupPlayer(Materials m)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            player.transform.position = new Vector3(0f, 1f, 0f);

            if (player.GetComponent<CarrySystem>() == null) player.AddComponent<CarrySystem>();
            if (player.GetComponent<CarrierBeacon>() == null) player.AddComponent<CarrierBeacon>();

            // Swap the placeholder capsule for a real character model. The capsule's own
            // renderer is switched off rather than removed, so the CharacterController and
            // every collider on the Lord keep working exactly as before.
            Renderer capsule = player.GetComponent<Renderer>();
            if (capsule != null) capsule.enabled = false;

            Transform oldVisual = player.transform.Find("LordVisual");
            if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

            GameObject visual = new GameObject("LordVisual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            Piece("Characters/character-a", visual.transform, Vector3.zero, 0f, 0.75f);

            SetupCamera();
        }

        /// <summary>
        /// Gamma space renders noticeably brighter than Linear, so the old sun/ambient
        /// values blew the scene out to near-white. These are re-tuned for Gamma.
        /// </summary>
        private static void SetupLighting()
        {
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.intensity = 0.85f;
                l.color = new Color(1f, 0.96f, 0.89f);
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.55f;
                l.transform.rotation = Quaternion.Euler(48f, 40f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.58f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.45f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.28f, 0.24f);
        }

        /// <summary>
        /// Grass tufts, flowers, mushrooms and stumps sprinkled over the field. The bare
        /// ground plane read as flat green nothing; these break it up without costing
        /// anything meaningful in draw calls.
        /// </summary>
        private static void SetupGroundDetail(Transform root)
        {
            GameObject detail = new GameObject("GroundDetail");
            detail.transform.SetParent(root, false);

            Random.InitState(90210);

            string[] props =
            {
                "grass", "grass", "grass_large", "grass_leafs", "grass_leafsLarge",
                "plant_bush", "plant_flatShort",
                "flower_redA", "flower_yellowA", "flower_purpleA",
                "mushroom_red", "stump_old", "rock_smallA", "rock_smallB"
            };

            for (int i = 0; i < 320; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                // Skip the plaza itself so the centre stays readable.
                float radius = Mathf.Lerp(4f, 30f, Mathf.Sqrt(Random.value));
                Vector3 spot = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                string prop = props[Random.Range(0, props.Length)];
                GameObject go = NatureProp(prop, detail.transform, spot);
                if (go == null) continue;

                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(0.7f, 1.4f);
                go.transform.localScale = new Vector3(s, s, s);
            }
        }

        /// <summary>
        /// Nature-kit models carry vertex colours rather than a texture atlas, which URP Lit
        /// ignores — so each prop gets an explicit flat material instead.
        /// </summary>
        private static GameObject NatureProp(string modelName, Transform parent, Vector3 position)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelRoot + "Nature/" + modelName + ".fbx");
            if (src == null) return null;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = modelName;
            inst.transform.position = position;

            Material mat = NatureMaterialFor(modelName);
            if (mat != null)
            {
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
            }
            return inst;
        }

        private static Material NatureMaterialFor(string modelName)
        {
            string name;
            if (modelName.StartsWith("flower_red") || modelName.StartsWith("mushroom")) name = "NatureRed";
            else if (modelName.StartsWith("flower_yellow")) name = "NatureYellow";
            else if (modelName.StartsWith("flower_purple")) name = "NaturePurple";
            else if (modelName.StartsWith("stump")) name = "NatureWood";
            else if (modelName.StartsWith("rock")) name = "NatureStone";
            else if (modelName.StartsWith("plant_bush")) name = "NatureBush";
            else name = "NatureGreen";

            return AssetDatabase.LoadAssetAtPath<Material>(ModelRoot + "Nature/M_" + name + ".mat");
        }

        /// <summary>Locks the isometric angle and hands the camera over to CameraFollow.</summary>
        private static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.transform.rotation = Quaternion.Euler(45f, 315f, 0f);

            if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();
        }

        private static void SetupWorldNodes(Transform root, Prefabs prefabs)
        {
            GameObject nodes = new GameObject("ResourceNodes");
            nodes.transform.SetParent(root, false);

            Random.InitState(20260806);

            // Loosely scattered across the whole map rather than sitting in tight clumps,
            // with a keep-out ring around the village centre so pads stay walkable.
            // Ring sits outside the village so nodes never grow through a building, but
            // close enough that a gathering run stays short under the tighter camera.
            ScatterFreely(nodes.transform, prefabs.Tree, 28, 17f, 27f);
            ScatterFreely(nodes.transform, prefabs.Rock, 24, 17f, 27f);
            ScatterFreely(nodes.transform, prefabs.Animal, 14, 18f, 26f);

            // A small grove and outcrop right beside the village. Two jobs: the opening
            // frame is not an empty field, and the very first "chop 5 wood" task is a few
            // steps away instead of a trek out to the far ring.
            PlaceStarterCluster(nodes.transform, prefabs.Tree, new Vector3(-5.5f, 0f, -1.5f), 3.4f, 5);
            // Spread wider and one fewer, otherwise the boulders read as a single grey wall.
            PlaceStarterCluster(nodes.transform, prefabs.Rock, new Vector3(5f, 0f, -3f), 4.2f, 3);

            SetupSpawnDressing(root);
        }

        private static void PlaceStarterCluster(Transform parent, GameObject prefab, Vector3 centre, float radius, int count)
        {
            if (prefab == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float r = Random.Range(radius * 0.45f, radius);
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = centre + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(0.9f, 1.2f);
                go.transform.localScale = new Vector3(s, s, s);
            }
        }

        /// <summary>
        /// Non-interactive props around the spawn so the first thing the player sees reads
        /// as a lived-in village rather than a blank lawn.
        /// </summary>
        private static void SetupSpawnDressing(Transform root)
        {
            GameObject dressing = new GameObject("SpawnDressing");
            dressing.transform.SetParent(root, false);
            Transform t = dressing.transform;

            // A short path leading from the spawn down to the Market.
            for (int i = 0; i < 6; i++) Piece("FantasyTown/road", t, new Vector3(0f, 0f, -i));

            Piece("FantasyTown/fence", t, new Vector3(-2.5f, 0f, 1.5f), 0f);
            Piece("FantasyTown/fence", t, new Vector3(-2.5f, 0f, 2.5f), 0f);
            Piece("FantasyTown/fence-gate", t, new Vector3(-2.5f, 0f, 3.5f), 0f);
            // Hedges were dropped here: at this camera distance the kit's thin hedge pieces
            // read as floating pipes rather than greenery. Extra trees fill the space better.
            Piece("FantasyTown/tree-crooked", t, new Vector3(4.6f, 0f, 3.2f), 40f);
            Piece("FantasyTown/tree", t, new Vector3(6.2f, 0f, 1.8f), 150f);

            Piece("FantasyTown/lantern", t, new Vector3(1.4f, 0f, -1.2f));
            Piece("FantasyTown/lantern", t, new Vector3(-1.4f, 0f, -4.2f));
            Piece("FantasyTown/cart", t, new Vector3(2.8f, 0f, -3.4f), 25f);
            Piece("FantasyTown/banner-green", t, new Vector3(-1.6f, 0f, -0.5f), 90f);
            Piece("MiniForest/tent", t, new Vector3(-4.2f, 0f, 2.8f), 200f);
            Piece("MiniForest/plant", t, new Vector3(1.9f, 0f, 0.6f));
            Piece("MiniForest/plant", t, new Vector3(-2.0f, 0f, -2.6f));
        }

        private static void ScatterFreely(Transform parent, GameObject prefab, int count, float minRadius, float maxRadius)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = Vector3.zero;
                // A few tries to avoid landing right on top of another node.
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Mathf.Lerp(minRadius, maxRadius, Mathf.Sqrt(Random.value));
                    position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    if (!IsTooClose(parent, position, 2.2f)) break;
                }

                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = position;
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float s = Random.Range(0.85f, 1.25f);
                go.transform.localScale = new Vector3(s, s, s);
            }
        }

        private static bool IsTooClose(Transform parent, Vector3 position, float minDistance)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (Vector3.Distance(parent.GetChild(i).position, position) < minDistance) return true;
            }
            return false;
        }

        private static void SetupBuildPads(Transform root, Prefabs prefabs, Materials m)
        {
            GameObject pads = new GameObject("BuildPads");
            pads.transform.SetParent(root, false);

            // The village is a wheel: the plaza is the hub, the Market sits just south of
            // it (the very first objective, right where the player spawns), and the ten
            // remaining buildings are spaced evenly around a ring so the town reads as
            // deliberately planned rather than scattered.
            MakePad(pads.transform, m, BuildingKind.VillageSquare, prefabs.VillageSquare, Vector3.zero);
            MakePad(pads.transform, m, BuildingKind.Market, prefabs.Market, new Vector3(0f, 0f, -7f));

            BuildingKind[] ring =
            {
                BuildingKind.LumberCamp, BuildingKind.Quarry, BuildingKind.Blacksmith,
                BuildingKind.Barracks,   BuildingKind.Church, BuildingKind.Treasury,
                BuildingKind.Mill,       BuildingKind.Inn,    BuildingKind.CropField,
                BuildingKind.Well,       BuildingKind.Farm
            };
            GameObject[] ringPrefabs =
            {
                prefabs.LumberCamp, prefabs.Quarry, prefabs.Blacksmith,
                prefabs.Barracks,   prefabs.Church, prefabs.Treasury,
                prefabs.Mill,       prefabs.Inn,    prefabs.CropField,
                prefabs.Well,       prefabs.Farm
            };

            const float ringRadius = 13f;
            for (int i = 0; i < ring.Length; i++)
            {
                // Start just past the Market and sweep the rest of the circle evenly.
                float angle = Mathf.PI * (0.5f + (i + 1) / (float)(ring.Length + 1) * 2f);
                Vector3 spot = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
                MakePad(pads.transform, m, ring[i], ringPrefabs[i], spot);
            }

            SetupTrainingGround(root, prefabs);
        }

        private static void SetupTrainingGround(Transform root, Prefabs prefabs)
        {
            if (prefabs.Dummy == null) return;

            GameObject ground = new GameObject("TrainingGround");
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(-19f, 0f, -13f);

            for (int i = 0; i < 4; i++)
            {
                GameObject dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Dummy, ground.transform);
                float angle = i * Mathf.PI * 0.5f;
                dummy.transform.position = ground.transform.position + new Vector3(Mathf.Cos(angle) * 2.6f, 0f, Mathf.Sin(angle) * 2.6f);
            }
        }

        private static void MakePad(Transform parent, Materials m, BuildingKind kind, GameObject buildingPrefab, Vector3 position)
        {
            GameObject pad = new GameObject("Pad_" + kind);
            pad.transform.SetParent(parent, false);
            pad.transform.position = position;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(pad.transform, false);

            Cylinder("Disc", visual.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.9f, 0.02f, 1.9f), m.Pad);

            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(visual.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Cube("Shaft", arrow.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.45f, 0.18f), m.Pad);
            Cube("Head", arrow.transform, Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f), m.Pad).transform.localRotation
                = Quaternion.Euler(0f, 45f, 45f);

            // Name above the price so the player knows what they are paying for before
            // they commit — a bare number gives no idea what is about to be built.
            Label(visual.transform, new Vector3(0f, 1.15f, -0.05f), GameConfig.DisplayName(kind).ToUpperInvariant(), 3.4f);
            TextMeshPro label = Label(visual.transform, new Vector3(0f, 0.62f, -0.05f), GameConfig.CostOf(kind).ToString(), 5f);

            BuildPad component = pad.AddComponent<BuildPad>();
            SetPrivate(component, "kind", kind);
            SetPrivate(component, "buildingPrefab", buildingPrefab);
            SetPrivate(component, "visualRoot", visual);
            SetPrivate(component, "arrow", arrow.transform);
            SetPrivate(component, "costLabel", label);
        }

        private static void SetupCustomerRoute(Transform root, Prefabs prefabs)
        {
            GameObject spawner = new GameObject("CustomerSpawner");
            spawner.transform.SetParent(root, false);

            GameObject spawn = new GameObject("SpawnPoint");
            spawn.transform.SetParent(spawner.transform, false);
            spawn.transform.position = new Vector3(20f, 0.9f, 16f);

            GameObject exit = new GameObject("ExitPoint");
            exit.transform.SetParent(spawner.transform, false);
            exit.transform.position = new Vector3(23f, 0.9f, 20f);

            CustomerSpawner cs = spawner.AddComponent<CustomerSpawner>();
            SetPrivate(cs, "customerPrefab", prefabs.Customer);
            SetPrivate(cs, "spawnPoint", spawn.transform);
            SetPrivate(cs, "exitPoint", exit.transform);
        }

        private static void SetupQuestArrow(Materials m)
        {
            GameObject arrow = new GameObject("QuestArrow");

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(arrow.transform, false);
            // Chunky enough to read instantly at this camera distance.
            visual.transform.localScale = Vector3.one * 1.7f;

            // Flat arrow lying along +Z so LookRotation(direction) actually aims it at the
            // objective. The old version was a downward marker, which pointed nowhere useful
            // once the code started rotating it toward a target.
            Material arrowMat = Mat("M_QuestArrow", new Color(0.30f, 0.92f, 0.38f));
            Cube("Shaft", visual.transform, new Vector3(0f, 0f, -0.45f), new Vector3(0.34f, 0.12f, 0.9f), arrowMat);
            Cube("HeadL", visual.transform, new Vector3(-0.22f, 0f, 0.22f), new Vector3(0.62f, 0.12f, 0.28f), arrowMat)
                .transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
            Cube("HeadR", visual.transform, new Vector3(0.22f, 0f, 0.22f), new Vector3(0.62f, 0.12f, 0.28f), arrowMat)
                .transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            QuestArrow qa = arrow.AddComponent<QuestArrow>();
            SetPrivate(qa, "arrowVisual", visual);
        }

        private static void SetupHUD()
        {
            GameObject canvasGo = new GameObject("HUDCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject goldPanel = UIImage(canvasGo.transform, "GoldPanel",
                new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(280f, 96f), Color.white);
            Skin(goldPanel, "panel_brown", Color.white);

            UIImage(goldPanel.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(52f, 52f),
                GameConfig.ColorOf(ResourceType.Gold));
            TMP_Text goldText = UIText(goldPanel.transform, "GoldText", new Vector2(0.5f, 0.5f), new Vector2(30f, 0f),
                new Vector2(160f, 70f), "100", 46f, TextAlignmentOptions.Right);

            GameObject questPanel = UIImage(canvasGo.transform, "QuestPanel",
                new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 74f), Color.white);
            Skin(questPanel, "panel_brown_dark", Color.white);

            GameObject barGo = new GameObject("ProgressBar", typeof(RectTransform));
            barGo.transform.SetParent(questPanel.transform, false);
            RectTransform barRect = (RectTransform)barGo.transform;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(520f, 34f);
            barRect.anchoredPosition = Vector2.zero;

            Slider slider = barGo.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;

            GameObject bg = UIImage(barGo.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 34f),
                Color.white);
            StretchFull((RectTransform)bg.transform);
            Skin(bg, "progress_green_border", Color.white);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGo.transform, false);
            RectTransform fillAreaRect = (RectTransform)fillArea.transform;
            StretchFull(fillAreaRect);
            // Inset so the fill sits inside the frame rather than covering its edge.
            fillAreaRect.offsetMin = new Vector2(6f, 6f);
            fillAreaRect.offsetMax = new Vector2(-6f, -6f);

            GameObject fill = UIImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 34f),
                Color.white);
            StretchFull((RectTransform)fill.transform);
            Skin(fill, "progress_green", Color.white);

            slider.fillRect = (RectTransform)fill.transform;
            slider.targetGraphic = fill.GetComponent<Image>();

            TMP_Text progressLabel = UIText(barGo.transform, "ProgressText", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(200f, 34f), "0%", 26f, TextAlignmentOptions.Center);

            TMP_Text questText = UIText(canvasGo.transform, "QuestText", new Vector2(0.5f, 1f), new Vector2(0f, -96f),
                new Vector2(760f, 60f), "Pazari kur!", 40f, TextAlignmentOptions.Center);

            GameObject settings = UIImage(canvasGo.transform, "SettingsButton", new Vector2(0f, 1f),
                new Vector2(64f, -64f), new Vector2(100f, 100f), Color.white);
            Skin(settings, "button_brown", Color.white);
            // Hamburger drawn from plain bars — the gear glyph is not in the TMP font and
            // rendered as a missing-character box.
            for (int i = -1; i <= 1; i++)
            {
                UIImage(settings.transform, "Bar" + i, new Vector2(0.5f, 0.5f), new Vector2(0f, i * -18f),
                    new Vector2(48f, 7f), new Color(0.32f, 0.24f, 0.16f, 1f));
            }
            Button settingsButton = settings.AddComponent<Button>();
            settingsButton.targetGraphic = settings.GetComponent<Image>();

            BuildSettingsPanel(canvasGo.transform, settingsButton);
            BuildDailyQuestCard(canvasGo.transform);

            // Bottom-right dump button — thumb-reachable on a phone, clickable on desktop.
            GameObject trash = UIImage(canvasGo.transform, "TrashButton", new Vector2(1f, 0f),
                new Vector2(-50f, 50f), new Vector2(140f, 140f), Color.white);
            Skin(trash, "button_red", Color.white);
            UIText(trash.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(126f, 126f),
                "AT", 38f, TextAlignmentOptions.Center);

            Button trashButton = trash.AddComponent<Button>();
            trashButton.targetGraphic = trash.GetComponent<Image>();
            trash.AddComponent<CanvasGroup>();
            trash.AddComponent<TrashButton>();

            // Touch zone covering the left half of the screen. It sits behind the buttons in
            // sibling order so the trash button still wins a tap in the bottom-right.
            GameObject stickZone = new GameObject("JoystickZone", typeof(RectTransform));
            stickZone.transform.SetParent(canvasGo.transform, false);
            stickZone.transform.SetAsFirstSibling();
            RectTransform zoneRect = (RectTransform)stickZone.transform;
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = new Vector2(0.6f, 1f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;

            Image zoneHit = stickZone.AddComponent<Image>();
            zoneHit.color = new Color(0f, 0f, 0f, 0f); // invisible but raycastable

            GameObject ring = UIImage(stickZone.transform, "Ring", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(240f, 240f), new Color(1f, 1f, 1f, 0.25f));
            GameObject knob = UIImage(stickZone.transform, "Knob", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(110f, 110f), new Color(1f, 1f, 1f, 0.55f));
            ring.GetComponent<Image>().raycastTarget = false;
            knob.GetComponent<Image>().raycastTarget = false;

            CanvasGroup stickGroup = stickZone.AddComponent<CanvasGroup>();
            stickGroup.alpha = 0f;

            VirtualJoystick joystick = stickZone.AddComponent<VirtualJoystick>();
            SetPrivate(joystick, "ring", ring.GetComponent<RectTransform>());
            SetPrivate(joystick, "knob", knob.GetComponent<RectTransform>());
            SetPrivate(joystick, "group", stickGroup);

            GameObject hudGo = new GameObject("HUDManager");
            hudGo.transform.SetParent(canvasGo.transform, false);
            HUDManager hud = hudGo.AddComponent<HUDManager>();
            hud.Bind(goldText, questText, slider, progressLabel);
        }

        /// <summary>Gear popup with music and SFX sliders, hidden until the gear is tapped.</summary>
        private static void BuildSettingsPanel(Transform canvas, Button openButton)
        {
            GameObject dim = UIImage(canvas, "SettingsPanel", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(100f, 100f), new Color(0f, 0f, 0f, 0.55f));
            StretchFull((RectTransform)dim.transform);

            GameObject card = UIImage(dim.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(640f, 460f), Color.white);
            Skin(card, "panel_brown", Color.white);

            UIText(card.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                new Vector2(520f, 70f), "AYARLAR", 46f, TextAlignmentOptions.Center);

            Slider musicSlider = BuildLabelledSlider(card.transform, "Music", "MUZIK", new Vector2(0f, 24f));
            Slider sfxSlider = BuildLabelledSlider(card.transform, "Sfx", "SES", new Vector2(0f, -96f));

            GameObject close = UIImage(card.transform, "CloseButton", new Vector2(0.5f, 0f),
                new Vector2(0f, 62f), new Vector2(260f, 88f), Color.white);
            Skin(close, "button_red", Color.white);
            UIText(close.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0f, 4f),
                new Vector2(240f, 70f), "KAPAT", 38f, TextAlignmentOptions.Center);
            Button closeButton = close.AddComponent<Button>();
            closeButton.targetGraphic = close.GetComponent<Image>();

            SettingsPanel panel = dim.AddComponent<SettingsPanel>();
            SetPrivate(panel, "panelRoot", dim);
            SetPrivate(panel, "openButton", openButton);
            SetPrivate(panel, "closeButton", closeButton);
            SetPrivate(panel, "musicSlider", musicSlider);
            SetPrivate(panel, "sfxSlider", sfxSlider);
        }

        private static Slider BuildLabelledSlider(Transform parent, string name, string caption, Vector2 pos)
        {
            UIText(parent, name + "Label", new Vector2(0.5f, 0.5f), pos + new Vector2(-210f, 46f),
                new Vector2(240f, 50f), caption, 32f, TextAlignmentOptions.Left);

            GameObject barGo = new GameObject(name + "Slider", typeof(RectTransform));
            barGo.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)barGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500f, 44f);
            rect.anchoredPosition = pos;

            Slider slider = barGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.transition = Selectable.Transition.None;

            GameObject bg = UIImage(barGo.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(500f, 44f), Color.white);
            StretchFull((RectTransform)bg.transform);
            Skin(bg, "progress_green_border", Color.white);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGo.transform, false);
            RectTransform fillAreaRect = (RectTransform)fillArea.transform;
            StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(8f, 8f);
            fillAreaRect.offsetMax = new Vector2(-8f, -8f);

            GameObject fill = UIImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(500f, 44f), Color.white);
            StretchFull((RectTransform)fill.transform);
            Skin(fill, "progress_green", Color.white);

            slider.fillRect = (RectTransform)fill.transform;
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }

        /// <summary>Daily-task card under the quest banner, mirroring the reference layout.</summary>
        private static void BuildDailyQuestCard(Transform canvas)
        {
            GameObject card = UIImage(canvas, "DailyCard", new Vector2(0f, 1f), new Vector2(30f, -190f),
                new Vector2(440f, 240f), Color.white);
            // Darkened so the light task text stays legible on top of it.
            Skin(card, "panel_brown_dark", Color.white);

            UIText(card.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -34f),
                new Vector2(400f, 46f), "GUNLUK GOREVLER", 26f, TextAlignmentOptions.Center);

            DailyQuestCard view = card.AddComponent<DailyQuestCard>();

            List<TMP_Text> rowTexts = new List<TMP_Text>();
            List<Slider> rowBars = new List<Slider>();
            for (int i = 0; i < 3; i++)
            {
                float y = -70f - i * 44f;
                rowTexts.Add(UIText(card.transform, "Row" + i, new Vector2(0.5f, 1f), new Vector2(-6f, y),
                    new Vector2(380f, 34f), "-", 22f, TextAlignmentOptions.Left));
                rowBars.Add(BuildThinBar(card.transform, "Bar" + i, new Vector2(0f, y - 22f)));
            }
            SetPrivate(view, "rows", rowTexts);
            SetPrivate(view, "bars", rowBars);
        }

        private static Slider BuildThinBar(Transform parent, string name, Vector2 pos)
        {
            GameObject barGo = new GameObject(name, typeof(RectTransform));
            barGo.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)barGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(370f, 12f);
            rect.anchoredPosition = pos;

            Slider slider = barGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject bg = UIImage(barGo.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(370f, 12f), new Color(0.2f, 0.18f, 0.16f, 0.9f));
            StretchFull((RectTransform)bg.transform);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGo.transform, false);
            StretchFull((RectTransform)fillArea.transform);

            GameObject fill = UIImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(370f, 12f), new Color(0.38f, 0.85f, 0.42f));
            StretchFull((RectTransform)fill.transform);

            slider.fillRect = (RectTransform)fill.transform;
            slider.targetGraphic = fill.GetComponent<Image>();
            return slider;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject UIImage(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        /// <summary>Loads one of the imported Kenney UI sprites.</summary>
        private static Sprite UISprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Kenney/" + name + ".png");
        }

        /// <summary>Skins an existing UI Image with a 9-sliced Kenney sprite.</summary>
        private static void Skin(GameObject go, string spriteName, Color tint)
        {
            if (go == null) return;
            Image image = go.GetComponent<Image>();
            if (image == null) return;

            Sprite sprite = UISprite(spriteName);
            if (sprite == null)
            {
                Debug.LogWarning("[SceneSetup] Missing UI sprite: " + spriteName);
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = tint;
            // Sliced sprites with no border would otherwise vanish at small sizes.
            image.pixelsPerUnitMultiplier = 1f;
        }

        private static TMP_Text UIText(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
            string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        // ----------------------------------------------------------------- helpers

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string[] bits = parent.Split('/');
                string running = bits[0];
                for (int i = 1; i < bits.Length; i++)
                {
                    if (!AssetDatabase.IsValidFolder(running + "/" + bits[i])) AssetDatabase.CreateFolder(running, bits[i]);
                    running += "/" + bits[i];
                }
            }
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }

        /// <summary>Fills a ProductionBuilding's ingredient list (a list of nested classes).</summary>
        private static void SetIngredients(ProductionBuilding target, Stockpile[] piles)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty list = so.FindProperty("inputs");
            list.arraySize = piles.Length;

            for (int i = 0; i < piles.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("type").enumValueIndex = (int)piles[i].ResourceType;
                element.FindPropertyRelative("pile").objectReferenceValue = piles[i];
                element.FindPropertyRelative("amount").intValue = 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns a [SerializeField] private field through SerializedObject.</summary>
        private static void SetPrivate(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning("[SceneSetup] Missing field '" + fieldName + "' on " + target.GetType().Name);
                return;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = System.Convert.ToInt32(value); break;
                case SerializedPropertyType.Float: prop.floatValue = System.Convert.ToSingle(value); break;
                case SerializedPropertyType.Boolean: prop.boolValue = System.Convert.ToBoolean(value); break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = System.Convert.ToInt32(value); break;
                case SerializedPropertyType.Vector3: prop.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = (Object)value; break;
                case SerializedPropertyType.Generic: SetArray(prop, value); break;
                default:
                    Debug.LogWarning("[SceneSetup] Unhandled field type for '" + fieldName + "'");
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedProperty prop, object value)
        {
            if (!prop.isArray) return;

            System.Collections.IList list = value as System.Collections.IList;
            if (list == null) return;

            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                SerializedProperty element = prop.GetArrayElementAtIndex(i);
                object item = list[i];

                if (element.propertyType == SerializedPropertyType.Enum) element.enumValueIndex = System.Convert.ToInt32(item);
                else if (element.propertyType == SerializedPropertyType.ObjectReference) element.objectReferenceValue = (Object)item;
                else if (element.propertyType == SerializedPropertyType.Integer) element.intValue = System.Convert.ToInt32(item);
            }
        }
    }
}
