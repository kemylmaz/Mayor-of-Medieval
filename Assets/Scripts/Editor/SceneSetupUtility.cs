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
            SetupPlayer(mats);
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

            foreach (string child in new[] { "Buildings", "Decorations" })
            {
                Transform container = world.transform.Find(child);
                if (container == null) continue;
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(container.GetChild(i).gameObject);
                }
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

        // -------------------------------------------------------- world node prefabs

        private static GameObject BuildTree(Materials m)
        {
            GameObject root = new GameObject("Tree");
            Cylinder("Trunk", root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.55f, 0.22f), m.Wood);
            Sphere("Canopy", root.transform, new Vector3(0f, 1.5f, 0f), new Vector3(1.1f, 0.95f, 1.1f), m.Leaf);

            HarvestNode node = root.AddComponent<HarvestNode>();
            SetPrivate(node, "resourceType", ResourceType.Wood);
            SetPrivate(node, "unitsPerNode", 3);
            SetPrivate(node, "respawnSeconds", 10f);
            return root;
        }

        private static GameObject BuildRock(Materials m)
        {
            GameObject root = new GameObject("Rock");
            Sphere("Body", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.75f, 1.1f), m.Stone);
            Sphere("Chunk", root.transform, new Vector3(0.45f, 0.2f, -0.3f), new Vector3(0.55f, 0.45f, 0.55f), m.Stone);

            HarvestNode node = root.AddComponent<HarvestNode>();
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

            Cube("Torso", body.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.55f, 0.5f, 0.95f), m.Animal);
            Sphere("Head", body.transform, new Vector3(0f, 0.85f, 0.6f), new Vector3(0.42f, 0.42f, 0.42f), m.Animal);
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

        private static GameObject BuildCharacterBody(string name, Material bodyMat)
        {
            GameObject root = new GameObject(name);
            Cylinder("Body", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.45f, 0.45f), bodyMat);
            Sphere("Head", root.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.5f, 0.5f, 0.5f), bodyMat);
            return root;
        }

        private static GameObject BuildWorker(Materials m)
        {
            GameObject root = BuildCharacterBody("Worker", m.Worker);
            CarrySystem carry = root.AddComponent<CarrySystem>();
            SetPrivate(carry, "capacity", GameConfig.WorkerCarryCapacity);
            root.AddComponent<CarrierBeacon>();
            root.AddComponent<Worker>();
            return root;
        }

        private static GameObject BuildCustomer(Materials m)
        {
            GameObject root = BuildCharacterBody("Customer", m.Customer);
            root.AddComponent<Customer>();
            return root;
        }

        private static GameObject BuildSoldier(Materials m)
        {
            GameObject root = BuildCharacterBody("Soldier", m.Soldier);
            Cube("Blade", root.transform, new Vector3(0.35f, 0.8f, 0.2f), new Vector3(0.1f, 0.7f, 0.1f), m.Sword);
            root.AddComponent<Soldier>();
            return root;
        }

        private static GameObject BuildDummy(Materials m)
        {
            GameObject root = new GameObject("TrainingDummy");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            Cylinder("Post", visual.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.18f, 0.6f, 0.18f), m.Wood);
            Cube("Torso", visual.transform, new Vector3(0f, 1.35f, 0f), new Vector3(0.7f, 0.7f, 0.4f), m.Enemy);
            Cube("Arms", visual.transform, new Vector3(0f, 1.5f, 0f), new Vector3(1.5f, 0.16f, 0.16f), m.Wood);
            Sphere("Head", visual.transform, new Vector3(0f, 1.95f, 0f), new Vector3(0.42f, 0.42f, 0.42f), m.Enemy);

            TrainingDummy dummy = root.AddComponent<TrainingDummy>();
            SetPrivate(dummy, "visualRoot", visual.transform);
            return root;
        }

        // ---------------------------------------------------------- building helpers

        private static GameObject BuildHut(string name, Materials m, Vector3 size)
        {
            GameObject root = new GameObject(name);
            Cube("Walls", root.transform, new Vector3(0f, size.y * 0.5f, 0f), size, m.Wall, true);
            Cube("Roof", root.transform, new Vector3(0f, size.y + 0.28f, 0f),
                new Vector3(size.x * 0.85f, 0.55f, size.z * 0.85f), m.Roof);
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
            GameObject root = BuildHut("Market", m, new Vector3(2.6f, 1.5f, 2.0f));
            Cube("Stall", root.transform, new Vector3(0f, 0.55f, -1.35f), new Vector3(2.8f, 0.18f, 0.7f), m.Wood);

            // Shelves the Lord (or a Carrier) stocks; customers help themselves.
            Stockpile woodShelf = AddStockpile(root, "WoodShelf", new Vector3(-1.6f, 0f, -1.2f), ResourceType.Wood, 12, false, true);
            Stockpile stoneShelf = AddStockpile(root, "StoneShelf", new Vector3(1.6f, 0f, -1.2f), ResourceType.Stone, 12, false, true);

            AddSalesPoint(root, new Vector3(0f, 0f, 1.6f), woodShelf, stoneShelf);
            AddCounter(root, new Vector3(0f, 0f, -2.4f), ResourceType.Wood, ResourceType.Stone);
            Label(root.transform, new Vector3(0f, 2.5f, 0f), "PAZAR", 4f);
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

            Label(root.transform, new Vector3(0f, 2.4f, 0f), caption, 3.5f);
            return root;
        }

        private static GameObject BuildFarm(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Farm", m, new Vector3(2.8f, 1.5f, 2.4f));

            Stockpile pile = AddStockpile(root, "MeatPile", new Vector3(-2.3f, 0f, 0f), ResourceType.Meat, 16, true, false, true);
            Stockpile shelf = AddStockpile(root, "MeatShelf", new Vector3(0f, 0f, -1.5f), ResourceType.Meat, 12, false, true);

            AddSalesPoint(root, new Vector3(0f, 0f, 1.8f), shelf);
            AddStation(root, workerPrefab,
                new[] { WorkerRole.Harvester, WorkerRole.Carrier },
                ResourceType.Meat, pile, new Vector3(2.6f, 0f, 0f));

            AddCounter(root, new Vector3(0f, 0f, -2.6f), ResourceType.Meat);
            Label(root.transform, new Vector3(0f, 2.5f, 0f), "CIFTLIK", 3.5f);
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
            Stockpile shelf = AddStockpile(root, "GrainShelf", new Vector3(0f, 0f, -2.4f), ResourceType.Grain, 12, false, true);

            CropField field = root.AddComponent<CropField>();
            SetPrivate(field, "output", pile);
            SetPrivate(field, "dryOverlay", dry);
            SetPrivate(field, "cropVisuals", crops.ToArray());

            AddSalesPoint(root, new Vector3(3.2f, 0f, 0f), shelf);
            AddCounter(root, new Vector3(0f, 0f, -3.4f), ResourceType.Grain);
            Label(root.transform, new Vector3(0f, 2f, 0f), "TARLA", 3.5f);
            return root;
        }

        private static GameObject BuildWell(Materials m)
        {
            GameObject root = new GameObject("Well");
            Cylinder("Rim", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.45f, 1.2f), m.Stone, true);
            Cylinder("Water", root.transform, new Vector3(0f, 0.85f, 0f), new Vector3(0.95f, 0.03f, 0.95f), m.Water);
            Cube("PostA", root.transform, new Vector3(0.5f, 1.3f, 0f), new Vector3(0.12f, 0.9f, 0.12f), m.Wood);
            Cube("PostB", root.transform, new Vector3(-0.5f, 1.3f, 0f), new Vector3(0.12f, 0.9f, 0.12f), m.Wood);
            Cube("Roof", root.transform, new Vector3(0f, 1.9f, 0f), new Vector3(1.5f, 0.15f, 1.5f), m.Roof);

            // Buckets the Mill/Inn producers come and fetch.
            Stockpile buckets = AddStockpile(root, "WaterPile", new Vector3(-1.8f, 0f, 0f), ResourceType.Water, 12, true, false, true);

            WaterWell well = root.AddComponent<WaterWell>();
            SetPrivate(well, "waterPile", buckets);

            Label(root.transform, new Vector3(0f, 2.6f, 0f), "KUYU", 3.5f);
            return root;
        }

        private static GameObject BuildMill(Materials m, GameObject workerPrefab)
        {
            GameObject root = new GameObject("Mill");
            Cylinder("Tower", root.transform, new Vector3(0f, 1.4f, 0f), new Vector3(1.8f, 1.4f, 1.8f), m.Wall, true);
            Cube("Cap", root.transform, new Vector3(0f, 2.95f, 0f), new Vector3(1.9f, 0.4f, 1.9f), m.Roof);

            GameObject sails = new GameObject("Sails");
            sails.transform.SetParent(root.transform, false);
            sails.transform.localPosition = new Vector3(0f, 2.6f, -1.1f);
            Cube("BladeA", sails.transform, Vector3.zero, new Vector3(0.25f, 3.2f, 0.1f), m.Wood);
            Cube("BladeB", sails.transform, Vector3.zero, new Vector3(3.2f, 0.25f, 0.1f), m.Wood);

            Stockpile grainIn = AddStockpile(root, "GrainInput", new Vector3(-2.6f, 0f, 2.2f), ResourceType.Grain, 16, false, true);
            Stockpile waterIn = AddStockpile(root, "WaterInput", new Vector3(-2.6f, 0f, 0.4f), ResourceType.Water, 16, false, true);
            // Reserve feeds the Inn's brewer before bread is sent to the shelf.
            Stockpile breadOut = AddStockpile(root, "BreadOutput", new Vector3(-2.6f, 0f, -2.2f), ResourceType.Bread, 16, true, false, true, 5);
            Stockpile shelf = AddStockpile(root, "BreadShelf", new Vector3(0f, 0f, -2.0f), ResourceType.Bread, 12, false, true);

            ProductionBuilding mill = root.AddComponent<ProductionBuilding>();
            SetIngredients(mill, new[] { grainIn, waterIn });
            SetPrivate(mill, "output", breadOut);
            SetPrivate(mill, "spinner", sails.transform);

            AddSalesPoint(root, new Vector3(2.6f, 0f, 0f), shelf);
            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Bread, breadOut, new Vector3(2.4f, 0f, 1.8f), mill);

            AddCounter(root, new Vector3(0f, 0f, -3.0f), ResourceType.Bread);
            Label(root.transform, new Vector3(0f, 3.7f, 0f), "DEGIRMEN", 3.5f);
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

            Label(root.transform, new Vector3(0f, 3f, 0f), "HAZINE", 3.5f);
            return root;
        }

        private static GameObject BuildBlacksmith(Materials m, GameObject workerPrefab)
        {
            GameObject root = BuildHut("Blacksmith", m, new Vector3(2.6f, 1.5f, 2.4f));
            Cube("Chimney", root.transform, new Vector3(0.9f, 2.2f, 0.8f), new Vector3(0.4f, 1.1f, 0.4f), m.Stone);
            GameObject anvil = Cube("Anvil", root.transform, new Vector3(0f, 0.35f, -1.7f), new Vector3(0.8f, 0.5f, 0.5f), m.Stone);

            Stockpile stoneIn = AddStockpile(root, "StoneInput", new Vector3(-2.4f, 0f, 1.4f), ResourceType.Stone, 16, false, true);
            // Reserve keeps the Barracks mustering even while swords are also being sold.
            Stockpile swordOut = AddStockpile(root, "SwordOutput", new Vector3(-2.4f, 0f, -1.4f), ResourceType.Sword, 16, true, false, true, 5);
            Stockpile shelf = AddStockpile(root, "SwordShelf", new Vector3(0f, 0f, -1.0f), ResourceType.Sword, 10, false, true);

            ProductionBuilding forge = root.AddComponent<ProductionBuilding>();
            SetIngredients(forge, new[] { stoneIn });
            SetPrivate(forge, "output", swordOut);
            SetPrivate(forge, "spinner", anvil.transform);
            SetPrivate(forge, "spinSpeed", 0f);
            SetPrivate(forge, "secondsPerCraft", 2.2f);

            AddSalesPoint(root, new Vector3(2.6f, 0f, 0f), shelf);
            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Sword, swordOut, new Vector3(2.4f, 0f, 1.6f), forge);

            AddCounter(root, new Vector3(0f, 0f, -2.6f), ResourceType.Sword);
            Label(root.transform, new Vector3(0f, 2.7f, 0f), "DEMIRCI", 3.5f);
            return root;
        }

        private static GameObject BuildBarracks(Materials m, GameObject workerPrefab, GameObject soldierPrefab)
        {
            GameObject root = BuildHut("Barracks", m, new Vector3(3.2f, 1.7f, 2.6f));
            Cube("BannerPost", root.transform, new Vector3(-1.4f, 2.4f, -1.2f), new Vector3(0.12f, 1.4f, 0.12f), m.Wood);
            Cube("Banner", root.transform, new Vector3(-1.0f, 2.9f, -1.2f), new Vector3(0.7f, 0.6f, 0.06f), m.Player);

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

            Label(root.transform, new Vector3(0f, 3f, 0f), "KISLA", 3.5f);
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
            Stockpile shelf = AddStockpile(root, "BeerShelf", new Vector3(0f, 0f, -1.6f), ResourceType.Beer, 12, false, true);

            ProductionBuilding brewery = root.AddComponent<ProductionBuilding>();
            SetIngredients(brewery, new[] { grainIn, waterIn, breadIn });
            SetPrivate(brewery, "output", beerOut);
            SetPrivate(brewery, "secondsPerCraft", 4f);

            AddSalesPoint(root, new Vector3(2.8f, 0f, 0f), shelf);
            AddStation(root, workerPrefab, new[] { WorkerRole.Producer, WorkerRole.Carrier },
                ResourceType.Beer, beerOut, new Vector3(2.6f, 0f, 1.8f), brewery);

            AddCounter(root, new Vector3(0f, 0f, -2.8f), ResourceType.Beer);
            Label(root.transform, new Vector3(0f, 3.1f, 0f), "HAN", 3.5f);
            return root;
        }

        private static GameObject BuildVillageSquare(Materials m)
        {
            GameObject root = new GameObject("VillageSquare");
            Cylinder("Plaza", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(6f, 0.04f, 6f), m.Marble, true);
            Cylinder("FountainBase", root.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.8f, 0.25f, 1.8f), m.Stone);
            Cylinder("FountainWater", root.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.5f, 0.04f, 1.5f), m.Water);
            Cylinder("Spout", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.25f, 0.45f, 0.25f), m.Stone);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 2.3f, 0.25f, Mathf.Sin(angle) * 2.3f);
                Cube("Bench", root.transform, pos, new Vector3(0.9f, 0.2f, 0.35f), m.Wood);
            }

            Label(root.transform, new Vector3(0f, 2.2f, 0f), "KOY MEYDANI", 3f);
            return root;
        }

        private static GameObject BuildChurch(Materials m)
        {
            GameObject root = new GameObject("Church");
            Cube("Nave", root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(3.0f, 2.4f, 4.5f), m.Marble, true);
            Cube("Roof", root.transform, new Vector3(0f, 2.7f, 0f), new Vector3(3.3f, 0.5f, 4.8f), m.Roof);
            Cube("Tower", root.transform, new Vector3(0f, 2.6f, 2.6f), new Vector3(1.4f, 5.2f, 1.4f), m.Marble);
            Cube("Spire", root.transform, new Vector3(0f, 5.6f, 2.6f), new Vector3(1.1f, 1.2f, 1.1f), m.Roof);
            Cube("CrossV", root.transform, new Vector3(0f, 6.6f, 2.6f), new Vector3(0.12f, 0.9f, 0.12f), m.Grain);
            Cube("CrossH", root.transform, new Vector3(0f, 6.75f, 2.6f), new Vector3(0.55f, 0.12f, 0.12f), m.Grain);

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
        }

        private static void SetupPlayer(Materials m)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            player.transform.position = new Vector3(0f, 1f, 0f);

            if (player.GetComponent<CarrySystem>() == null) player.AddComponent<CarrySystem>();
            if (player.GetComponent<CarrierBeacon>() == null) player.AddComponent<CarrierBeacon>();

            Renderer r = player.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m.Player;
        }

        private static void SetupWorldNodes(Transform root, Prefabs prefabs)
        {
            GameObject nodes = new GameObject("ResourceNodes");
            nodes.transform.SetParent(root, false);

            Random.InitState(20260806);

            // Loosely scattered across the whole map rather than sitting in tight clumps,
            // with a keep-out ring around the village centre so pads stay walkable.
            ScatterFreely(nodes.transform, prefabs.Tree, 26, 8f, 23f);
            ScatterFreely(nodes.transform, prefabs.Rock, 22, 9f, 23f);
            ScatterFreely(nodes.transform, prefabs.Animal, 14, 11f, 22f);
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

            MakePad(pads.transform, m, BuildingKind.Market, prefabs.Market, new Vector3(5f, 0f, 4f));
            MakePad(pads.transform, m, BuildingKind.LumberCamp, prefabs.LumberCamp, new Vector3(-6f, 0f, 6f));
            MakePad(pads.transform, m, BuildingKind.Quarry, prefabs.Quarry, new Vector3(-8f, 0f, -5f));
            MakePad(pads.transform, m, BuildingKind.Farm, prefabs.Farm, new Vector3(9f, 0f, -5f));
            MakePad(pads.transform, m, BuildingKind.CropField, prefabs.CropField, new Vector3(1f, 0f, -12f));
            MakePad(pads.transform, m, BuildingKind.Well, prefabs.Well, new Vector3(6f, 0f, -11f));
            MakePad(pads.transform, m, BuildingKind.Mill, prefabs.Mill, new Vector3(13f, 0f, 3f));
            MakePad(pads.transform, m, BuildingKind.Treasury, prefabs.Treasury, new Vector3(-2f, 0f, 8f));
            MakePad(pads.transform, m, BuildingKind.Blacksmith, prefabs.Blacksmith, new Vector3(-13f, 0f, 1f));
            MakePad(pads.transform, m, BuildingKind.Barracks, prefabs.Barracks, new Vector3(-14f, 0f, -8f));
            MakePad(pads.transform, m, BuildingKind.Inn, prefabs.Inn, new Vector3(13f, 0f, -10f));
            MakePad(pads.transform, m, BuildingKind.VillageSquare, prefabs.VillageSquare, new Vector3(0f, 0f, 14f));
            MakePad(pads.transform, m, BuildingKind.Church, prefabs.Church, new Vector3(-8f, 0f, 14f));

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
            Cube("Shaft", visual.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.35f, 0.9f, 0.35f), m.Pad);
            Cube("Head", visual.transform, Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f), m.Pad).transform.localRotation
                = Quaternion.Euler(0f, 45f, 45f);

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
                new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(260f, 90f), new Color(0.12f, 0.12f, 0.14f, 0.85f));
            UIImage(goldPanel.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(56f, 56f),
                new Color(0.35f, 0.78f, 0.35f));
            TMP_Text goldText = UIText(goldPanel.transform, "GoldText", new Vector2(0.5f, 0.5f), new Vector2(28f, 0f),
                new Vector2(150f, 70f), "100", 46f, TextAlignmentOptions.Right);

            GameObject questPanel = UIImage(canvasGo.transform, "QuestPanel",
                new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(560f, 62f), new Color(0.12f, 0.12f, 0.14f, 0.85f));

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
                new Color(0.25f, 0.25f, 0.28f, 1f));
            StretchFull((RectTransform)bg.transform);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(barGo.transform, false);
            StretchFull((RectTransform)fillArea.transform);

            GameObject fill = UIImage(fillArea.transform, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 34f),
                new Color(0.35f, 0.82f, 0.40f));
            StretchFull((RectTransform)fill.transform);

            slider.fillRect = (RectTransform)fill.transform;
            slider.targetGraphic = fill.GetComponent<Image>();

            TMP_Text progressLabel = UIText(barGo.transform, "ProgressText", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(200f, 34f), "0%", 26f, TextAlignmentOptions.Center);

            TMP_Text questText = UIText(canvasGo.transform, "QuestText", new Vector2(0.5f, 1f), new Vector2(0f, -96f),
                new Vector2(760f, 60f), "Pazari kur!", 40f, TextAlignmentOptions.Center);

            UIImage(canvasGo.transform, "SettingsButton", new Vector2(0f, 1f), new Vector2(52f, -52f), new Vector2(72f, 72f),
                new Color(0.9f, 0.9f, 0.92f, 0.9f));

            GameObject hudGo = new GameObject("HUDManager");
            hudGo.transform.SetParent(canvasGo.transform, false);
            HUDManager hud = hudGo.AddComponent<HUDManager>();
            hud.Bind(goldText, questText, slider, progressLabel);
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
