using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the authored, missionless Harborline free-roam scene. Gameplay components
/// are resolved by name so the world can be assembled while runtime code is evolving.
/// Blender FBX files under Generated/Models replace the procedural environment kit.
/// </summary>
public static class HarborlineBuilder
{
    const string ScenePath = "Assets/GTA/Scenes/Harborline.unity";
    const string MaterialPath = "Assets/GTA/Generated/Materials";
    const string ModelPath = "Assets/GTA/Generated/Models";
    static readonly int[] Grid = { -240, -160, -80, 0, 80, 160, 240 };
    static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, GameObject> ModelCache = new Dictionary<string, GameObject>();
    static readonly List<Transform[]> RoutePaths = new List<Transform[]>();
    static Transform world, roads, architecture, street, nature, locations, routes, characters, vehicles;
    static System.Random random;

    [MenuItem("GTA/Build Harborline")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/GTA/Scenes");
        Directory.CreateDirectory(MaterialPath);
        AssetDatabase.Refresh();
        Materials.Clear();
        ModelCache.Clear();
        RoutePaths.Clear();
        random = new System.Random(68314);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        world = Group("Harborline | 600 x 600 m", null);
        roads = Group("Roads and Promenade", world);
        architecture = Group("District Architecture", world);
        street = Group("Street Furniture", world);
        nature = Group("Parks and Nature", world);
        locations = Group("World Locations", world);
        routes = Group("Traffic Routes", world);
        characters = Group("Population", world);
        vehicles = Group("Vehicle Population", world);

        BuildGroundAndWater();
        BuildRoadGrid();
        BuildDistricts();
        BuildStreetFurniture();
        BuildNature();
        BuildLandmarksAndLocations();
        BuildTrafficRoutes();
        BuildGameplayRoots();
        BuildInitialPopulation();
        ConfigureLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new Exception("Could not save " + ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Harborline built: 600 x 600 m, six districts, gameplay roots and population. " + ScenePath);
    }

    static Transform Group(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Material Mat(string key, Color color, float smooth = 0.16f, bool emission = false)
    {
        if (Materials.TryGetValue(key, out Material cached)) return cached;
        string path = MaterialPath + "/HL_" + key + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { name = "HL_" + key };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smooth);
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 2.4f);
        }
        Materials[key] = material;
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.isStatic = true;
        return go;
    }

    static GameObject Cylinder(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        go.isStatic = true;
        return go;
    }

    static Material M(string name) => Materials[name];

    static void CreatePalette()
    {
        Mat("Asphalt", new Color(.115f, .145f, .17f), .09f);
        Mat("LaneIvory", new Color(.92f, .85f, .62f), .2f);
        Mat("Crosswalk", new Color(.78f, .84f, .83f), .17f);
        Mat("Sidewalk", new Color(.49f, .55f, .54f), .09f);
        Mat("Sandstone", new Color(.81f, .70f, .53f), .16f);
        Mat("Boardwalk", new Color(.39f, .28f, .21f), .23f);
        Mat("Water", new Color(.10f, .37f, .45f), .76f);
        Mat("Seafoam", new Color(.38f, .73f, .72f), .58f);
        Mat("Grass", new Color(.24f, .45f, .31f), .04f);
        Mat("DarkGrass", new Color(.16f, .34f, .28f), .05f);
        Mat("PaleConcrete", new Color(.68f, .72f, .66f), .17f);
        Mat("WarmConcrete", new Color(.62f, .55f, .45f), .15f);
        Mat("Cream", new Color(.87f, .79f, .65f), .16f);
        Mat("Terracotta", new Color(.65f, .34f, .28f), .18f);
        Mat("Slate", new Color(.22f, .30f, .36f), .2f);
        Mat("SlateLight", new Color(.34f, .43f, .46f), .22f);
        Mat("OceanBlue", new Color(.18f, .43f, .55f), .24f);
        Mat("Teal", new Color(.15f, .55f, .53f), .22f);
        Mat("Mustard", new Color(.77f, .59f, .27f), .19f);
        Mat("Glass", new Color(.16f, .35f, .40f), .78f);
        Mat("WindowLight", new Color(.79f, .71f, .48f), .55f, true);
        Mat("Metal", new Color(.30f, .37f, .39f), .42f);
        Mat("NearBlack", new Color(.075f, .11f, .14f), .14f);
        Mat("Roof", new Color(.25f, .31f, .32f), .12f);
        Mat("Bark", new Color(.30f, .24f, .21f), .07f);
        Mat("Leaf", new Color(.19f, .43f, .34f), .08f);
        Mat("LeafGold", new Color(.55f, .56f, .27f), .08f);
        Mat("BeaconRed", new Color(.83f, .19f, .18f), .38f, true);
        Mat("BeaconBlue", new Color(.12f, .44f, .91f), .38f, true);
    }

    static void BuildGroundAndWater()
    {
        CreatePalette();
        Box("Mainland | walkable ground", world, new Vector3(0, -.25f, 66), new Vector3(600, .5f, 468), M("Sandstone"));
        Box("Port reclamation | walkable ground", world, new Vector3(175, -.25f, -234), new Vector3(250, .5f, 132), M("PaleConcrete"));
        Box("Sea bed", world, new Vector3(-80, -9, -235), new Vector3(440, 1, 130), M("DarkGrass"));
        Box("Harbor water surface | swimming", world, new Vector3(-80, -.06f, -235), new Vector3(440, .08f, 130), M("Water"), false);
        // Repeated bright bands give the opaque water surface a directional, stylized rhythm.
        for (int i = 0; i < 38; i++)
        {
            float x = -295 + (i * 79) % 420;
            float z = -292 + (i * 37) % 120;
            Box("Sea glint", world, new Vector3(x, .001f, z), new Vector3(7 + i % 4 * 3, .003f, .18f), M("Seafoam"), false);
        }
        Box("North park berm", nature, new Vector3(-186, .24f, 282), new Vector3(214, .48f, 28), M("DarkGrass"));
        Box("Coastal sand", world, new Vector3(-217, .025f, -174), new Vector3(165, .05f, 13), M("Cream"));
        // Open the retaining wall where the west and east port roads cross the shore.
        Box("Quay retaining wall west", world, new Vector3(60, .8f, -169), new Vector3(20, 1.6f, 3), M("Slate"));
        Box("Quay retaining wall center", world, new Vector3(159, .8f, -169), new Vector3(138, 1.6f, 3), M("Slate"));
        Box("Quay retaining wall east", world, new Vector3(276, .8f, -169), new Vector3(48, 1.6f, 3), M("Slate"));
        for (int i = 0; i < 5; i++) BuildPier(-257 + i * 38, i);
        Box("Western sea wall", world, new Vector3(-299, .7f, 0), new Vector3(2, 1.4f, 600), M("Slate"));
        Box("Eastern perimeter", world, new Vector3(299, .55f, 0), new Vector3(2, 1.1f, 600), M("Slate"));
    }

    static void BuildPier(float x, int index)
    {
        Transform pier = Group("Promenade pier " + index, world);
        pier.position = new Vector3(x, 0, -170);
        GameObject model = FindModel("proppromenadepier");
        if (model == null)
        {
            Box("Timber boardwalk", pier, new Vector3(x, .24f, -204), new Vector3(5.4f, .48f, 68), M("Boardwalk"));
            return;
        }
        // The FBX pivot is the shoreward end; its positive Z direction runs into the water
        // after rotating the segment 180 degrees. Four authored spans make each long pier.
        for (int section = 0; section < 4; section++)
        {
            GameObject segment = (GameObject)PrefabUtility.InstantiatePrefab(model);
            segment.name = "Blender pier span " + section;
            segment.transform.SetParent(pier, false);
            segment.transform.localPosition = new Vector3(0, -1.12f, -17 * section);
            segment.transform.localRotation = Quaternion.Euler(0, 180, 0);
            RemoveModelColliders(segment);
            ConvertModelMaterials(segment);
        }
        Box("Pier collision", pier, new Vector3(x, .14f, -204), new Vector3(5.1f, .28f, 68), M("Boardwalk"))
            .GetComponent<MeshRenderer>().enabled = false;
        Box("Beachside pier threshold", pier, new Vector3(x, .09f, -168), new Vector3(5.2f, .18f, 4), M("Boardwalk"));
    }

    static void BuildRoadGrid()
    {
        foreach (int x in Grid)
        {
            float width = x == 0 ? 20 : 13;
            Box("North-south road x=" + x, roads, new Vector3(x, .035f, 60), new Vector3(width, .07f, 456), M("Asphalt"));
            // Leave each intersection open. Continuous curb colliders used to block cars
            // at every crossing despite the asphalt visually continuing through.
            for (int segment = 1; segment < Grid.Length - 1; segment++)
            {
                float start = Grid[segment] + 12f;
                float end = Grid[segment + 1] - 12f;
                float center = (start + end) * .5f;
                float length = end - start;
                Box("West curb x=" + x, roads, new Vector3(x - width / 2 - .6f, .13f, center),
                    new Vector3(1.2f, .26f, length), M("Sidewalk"));
                Box("East curb x=" + x, roads, new Vector3(x + width / 2 + .6f, .13f, center),
                    new Vector3(1.2f, .26f, length), M("Sidewalk"));
            }
            Box("West curb north x=" + x, roads, new Vector3(x - width / 2 - .6f, .13f, 270),
                new Vector3(1.2f, .26f, 36), M("Sidewalk"));
            Box("East curb north x=" + x, roads, new Vector3(x + width / 2 + .6f, .13f, 270),
                new Vector3(1.2f, .26f, 36), M("Sidewalk"));
            for (int z = -146; z < 284; z += 16)
                Box("NS lane dash", roads, new Vector3(x, .075f, z), new Vector3(.18f, .012f, 7), M("LaneIvory"), false);
        }
        foreach (int z in Grid.Where(v => v >= -160))
        {
            float width = z == -80 ? 20 : 13;
            // Keep intersecting road colliders flush. The former 4.5 cm lip spun
            // moving cars when their front edge met the east-west slab.
            Box("East-west road z=" + z, roads, new Vector3(0, .036f, z), new Vector3(596, .07f, width), M("Asphalt"));
            for (int segment = -1; segment < Grid.Length; segment++)
            {
                float start = segment < 0 ? -298f : Grid[segment] + 12f;
                float end = segment == Grid.Length - 1 ? 298f : Grid[segment + 1] - 12f;
                float center = (start + end) * .5f;
                float length = end - start;
                Box("South curb z=" + z, roads, new Vector3(center, .14f, z - width / 2 - .6f),
                    new Vector3(length, .26f, 1.2f), M("Sidewalk"));
                Box("North curb z=" + z, roads, new Vector3(center, .14f, z + width / 2 + .6f),
                    new Vector3(length, .26f, 1.2f), M("Sidewalk"));
            }
            for (int x = -285; x < 285; x += 16)
                Box("EW lane dash", roads, new Vector3(x, .076f, z), new Vector3(7, .012f, .18f), M("LaneIvory"), false);
        }
        // Waterfront and runway access form a continuous loop with the street grid.
        // Complete the western Port Service leg without a curb across its lane.
        // Match the grid road surface height so vehicles do not meet a raised seam.
        Box("Port road", roads, new Vector3(174, .035f, -240), new Vector3(240, .07f, 14), M("Asphalt"));
        Box("Port connector west", roads, new Vector3(80, .035f, -200), new Vector3(14, .07f, 80), M("Asphalt"));
        Box("Port connector east", roads, new Vector3(240, .035f, -205), new Vector3(14, .07f, 80), M("Asphalt"));
        Box("Airfield runway", roads, new Vector3(212, .08f, 244), new Vector3(32, .10f, 92), M("Asphalt"));
        Box("Runway threshold north", roads, new Vector3(212, .145f, 279), new Vector3(15, .01f, 1), M("Crosswalk"), false);
        for (int z = 207; z <= 272; z += 13)
            Box("Runway centerline", roads, new Vector3(212, .15f, z), new Vector3(.42f, .01f, 7), M("Crosswalk"), false);
        foreach (int x in Grid)
            foreach (int z in Grid.Where(v => v >= -160))
            {
                if ((x + z) % 160 != 0) continue;
                for (int stripe = -4; stripe <= 4; stripe++)
                {
                    Box("Crosswalk east", roads, new Vector3(x + 10, .08f, z + stripe * 1.15f), new Vector3(3.4f, .012f, .62f), M("Crosswalk"), false);
                    Box("Crosswalk west", roads, new Vector3(x - 10, .08f, z + stripe * 1.15f), new Vector3(3.4f, .012f, .62f), M("Crosswalk"), false);
                }
            }
    }

    static void BuildDistricts()
    {
        int id = 0;
        for (int ix = 0; ix < Grid.Length - 1; ix++)
            for (int iz = 1; iz < Grid.Length - 1; iz++)
            {
                float cx = (Grid[ix] + Grid[ix + 1]) * .5f;
                float cz = (Grid[iz] + Grid[iz + 1]) * .5f;
                if (cx == 40 && cz == 120) { BuildClinicBlock(cx, cz, id++); continue; }
                if (cx == 40 && cz == 40) { BuildPoliceBlock(cx, cz, id++); continue; }
                if (cz > 190 && cx < -75) { BuildParkBlock(cx, cz, id++); continue; }
                if (cz > 190 && cx > 75) { BuildAirfieldBlock(cx, cz, id++); continue; }
                if (cx < -120 && cz >= 0) { BuildResidentialBlock(cx, cz, id++); continue; }
                if (cx > 100 && cz < 160) { BuildIndustrialBlock(cx, cz, id++); continue; }
                if (cz < 0 && cx < 90) { BuildMarketBlock(cx, cz, id++); continue; }
                BuildDowntownBlock(cx, cz, id++);
            }
    }

    static void BuildClinicBlock(float cx, float cz, int id)
    {
        Transform block = Group("Civic Medical | block " + id, architecture);
        Box("Clinic plaza", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("PaleConcrete"));
        BuildLandmark(block, "Civic Medical Clinic", cx, cz, 13.6f, .78f, "buildingclinic");
        for (int i = -2; i <= 2; i++)
            Box("Ambulance bay stripe", block, new Vector3(cx + i * 8.5f, .2f, cz - 22),
                new Vector3(.2f, .01f, 8), M("Crosswalk"), false);
    }

    static void BuildPoliceBlock(float cx, float cz, int id)
    {
        Transform block = Group("District Police | block " + id, architecture);
        Box("Station courtyard", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("SlateLight"));
        BuildLandmark(block, "District Police Station", cx, cz, 14.3f, .82f, "buildingpolicestation");
        Box("Patrol bay", block, new Vector3(cx - 24, .2f, cz + 19), new Vector3(13, .02f, 14), M("Asphalt"), false);
    }

    static void BuildDowntownBlock(float cx, float cz, int id)
    {
        Transform block = Group("Downtown | block " + id, architecture);
        Box("Inset plaza", block, new Vector3(cx, .09f, cz), new Vector3(63, .18f, 63), M("PaleConcrete"));
        for (int i = 0; i < 3; i++)
        {
            float bx = cx + (i == 0 ? -19 : i == 1 ? 19 : 0);
            float bz = cz + (i == 2 ? 18 : -10);
            float w = i == 2 ? 22 : 25;
            float d = i == 2 ? 23 : 30;
            float h = 20 + (id * 13 + i * 17) % 39;
            BuildBuilding(block, "Civic tower " + id + "." + i, bx, bz, w, d, h,
                i == 0 ? "OceanBlue" : i == 1 ? "SlateLight" : "WarmConcrete",
                i == 2 ? "buildingapartment" : "buildingoffice", "tower", "commercial");
        }
    }

    static void BuildResidentialBlock(float cx, float cz, int id)
    {
        Transform block = Group("Westhaven residential | block " + id, architecture);
        Box("Courtyard", block, new Vector3(cx, .08f, cz), new Vector3(65, .16f, 65), M("Grass"));
        for (int i = 0; i < 4; i++)
        {
            float bx = cx + (i % 2 == 0 ? -17 : 17);
            float bz = cz + (i < 2 ? -17 : 17);
            int h = 7 + (id + i) % 3 * 3;
            BuildBuilding(block, "Row house " + id + "." + i, bx, bz, 21, 20, h,
                (i + id) % 3 == 0 ? "Terracotta" : (i + id) % 3 == 1 ? "Cream" : "Teal",
                i == 3 && id % 2 == 0 ? "buildingapartment" : "coastalhouse", "residential");
        }
    }

    static void BuildIndustrialBlock(float cx, float cz, int id)
    {
        if (cx == 120 && cz == -120) { BuildGarageBlock(cx, cz, id); return; }
        if (cx == 200 && cz == -120) { BuildFuelBlock(cx, cz, id); return; }
        Transform block = Group("Foundry Quarter | block " + id, architecture);
        Box("Service yard", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("WarmConcrete"));
        BuildBuilding(block, "Workshop " + id, cx - 15, cz + 7, 33, 40, 12, "SlateLight", "warehouse", "factory", "industrial");
        BuildBuilding(block, "Depot " + id, cx + 22, cz - 14, 19, 27, 8, "Terracotta", "warehouse", "service");
        for (int i = 0; i < 4; i++)
            BuildContainer(block, cx - 25 + i * 13, cz - 25, i);
    }

    static void BuildGarageBlock(float cx, float cz, int id)
    {
        Transform block = Group("Copperline Customs | block " + id, architecture);
        Box("Repair forecourt", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("WarmConcrete"));
        BuildLandmark(block, "Copperline Repair Garage", cx, cz, 8.1f, .50f, "buildingrepairgarage");
        for (int i = -1; i <= 1; i++)
            Box("Service bay arrow", block, new Vector3(cx + i * 9, .20f, cz + 22),
                new Vector3(.22f, .01f, 7), M("LaneIvory"), false);
    }

    static void BuildFuelBlock(float cx, float cz, int id)
    {
        Transform block = Group("North Quay Fuel | block " + id, architecture);
        Box("Fuel court", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("PaleConcrete"));
        BuildLandmark(block, "North Quay Fuel Station", cx, cz, 6.3f, .45f, "buildingfuelstation");
        Box("Fuel lane", block, new Vector3(cx, .20f, cz + 21), new Vector3(24, .02f, 12), M("Asphalt"), false);
    }

    static void BuildMarketBlock(float cx, float cz, int id)
    {
        Transform block = Group("Harbor Market | block " + id, architecture);
        Box("Market square", block, new Vector3(cx, .09f, cz), new Vector3(65, .18f, 65), M("Sandstone"));
        BuildBuilding(block, "Arcade " + id + " west", cx - 18, cz, 25, 52, 11, "Cream", "storefront", "market");
        BuildBuilding(block, "Arcade " + id + " east", cx + 19, cz, 24, 52, 13, "Teal", "storefront", "market");
        Box("Market awning", block, new Vector3(cx, 3.7f, cz - 25), new Vector3(25, .35f, 7), M("Mustard"), false);
    }

    static void BuildParkBlock(float cx, float cz, int id)
    {
        Transform block = Group("Juniper Park | block " + id, nature);
        Box("Park lawn", block, new Vector3(cx, .09f, cz), new Vector3(67, .18f, 64), M("Grass"));
        Box("Park path NS", block, new Vector3(cx, .20f, cz), new Vector3(4, .04f, 64), M("Sandstone"), false);
        Box("Park path EW", block, new Vector3(cx, .20f, cz), new Vector3(67, .04f, 4), M("Sandstone"), false);
        for (int i = 0; i < 9; i++)
            BuildTree(block, cx - 28 + (i * 17) % 56, cz - 28 + (i * 23) % 56, 5 + i % 4);
        for (int i = 0; i < 3; i++)
            BuildBench(block, cx - 18 + i * 19, cz + 9);
    }

    static void BuildAirfieldBlock(float cx, float cz, int id)
    {
        Transform block = Group("Cape Airfield | block " + id, architecture);
        Box("Apron", block, new Vector3(cx, .10f, cz), new Vector3(66, .20f, 66), M("PaleConcrete"));
        if (cx < 185)
        {
            BuildLandmark(block, "Cape Airfield Hangar", cx, cz + 10, 10.2f, .86f, "buildingcoasthangar");
            Transform pad = Group("Cape Helipad", block);
            pad.position = new Vector3(cx, 0, cz - 22);
            GameObject model = FindModel("prophelipad");
            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.transform.SetParent(pad, false);
                FitVisual(visual.transform, new Vector3(16, .42f, 16), .2f);
                RemoveModelColliders(visual);
                ConvertModelMaterials(visual);
            }
            else
            {
                Box("Helipad disc", pad, new Vector3(cx, .22f, cz - 22), new Vector3(17, .03f, 17), M("NearBlack"), false);
                Box("Helipad H vertical", pad, new Vector3(cx, .25f, cz - 22), new Vector3(1.2f, .02f, 9), M("Crosswalk"), false);
                Box("Helipad H cross", pad, new Vector3(cx, .26f, cz - 22), new Vector3(8, .02f, 1.2f), M("Crosswalk"), false);
            }
        }
    }

    static void BuildLandmark(Transform parent, string name, float x, float z, float targetHeight,
        float collisionDepthFraction, string modelHint)
    {
        Transform landmark = Group(name, parent);
        landmark.position = new Vector3(x, 0, z);
        GameObject model = FindModel(modelHint);
        if (model == null)
        {
            BuildBuilding(parent, name + " | modular fallback", x, z, 28, 24, targetHeight, "SlateLight");
            return;
        }
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Blender landmark | " + model.name;
        visual.transform.SetParent(landmark, false);
        FitVisual(visual.transform, new Vector3(1000, targetHeight, 1000), 0);
        RemoveModelColliders(visual);
        ConvertModelMaterials(visual);
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float frontOpen = 1f - collisionDepthFraction;
        Vector3 colliderCenter = bounds.center - Vector3.forward * (frontOpen * bounds.size.z * .42f);
        Vector3 colliderSize = new Vector3(bounds.size.x * .82f, bounds.size.y * .95f, bounds.size.z * collisionDepthFraction);
        Box("Landmark collision", landmark, colliderCenter, colliderSize, M("Slate"))
            .GetComponent<MeshRenderer>().enabled = false;
    }

    static void BuildBuilding(Transform parent, string name, float x, float z, float w, float d, float h,
        string materialKey, params string[] modelHints)
    {
        Transform building = Group(name, parent);
        building.position = new Vector3(x, 0, z);
        GameObject model = FindModel(modelHints);
        // Generic search hints must never turn an "officer" or prop into a scaled building.
        if (model != null && !Normalize(Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(model))).StartsWith("hlbuilding"))
            model = null;
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Blender visual | " + model.name;
            visual.transform.SetParent(building, false);
            FitVisual(visual.transform, new Vector3(w, h, d), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
            Box("Building collider", building, new Vector3(x, h * .5f, z), new Vector3(w * .92f, h, d * .92f), M(materialKey)).GetComponent<MeshRenderer>().enabled = false;
            return;
        }
        // This layered kit is an intentional stylized fallback for environment assets.
        Box("Structure", building, new Vector3(x, h * .5f, z), new Vector3(w, h, d), M(materialKey));
        Box("Foundation", building, new Vector3(x, .5f, z), new Vector3(w + 1.2f, 1, d + 1.2f), M("Slate"));
        Box("Roof parapet", building, new Vector3(x, h + .35f, z), new Vector3(w + 1.1f, .7f, d + 1.1f), M("Roof"));
        Box("Door", building, new Vector3(x, 1.7f, z - d * .5f - .07f), new Vector3(2.5f, 3.4f, .15f), M("NearBlack"), false);
        Box("Entry awning", building, new Vector3(x, 3.7f, z - d * .5f - .7f), new Vector3(6.3f, .25f, 1.5f), M("Mustard"), false);
        int floors = Mathf.Clamp(Mathf.RoundToInt(h / 5), 1, 12);
        int columns = Mathf.Clamp(Mathf.RoundToInt(w / 5), 2, 7);
        for (int floor = 0; floor < floors; floor++)
        {
            float wy = 4.3f + floor * (h - 4.7f) / Mathf.Max(1, floors);
            if (wy > h - .8f) break;
            Box("Facade band", building, new Vector3(x, wy - 1.3f, z - d * .5f - .09f), new Vector3(w + .15f, .16f, .18f), M("Sandstone"), false);
            for (int col = 0; col < columns; col++)
            {
                float wx = x - w * .5f + w * (col + .5f) / columns;
                Box("Recessed window", building, new Vector3(wx, wy, z - d * .5f - .10f),
                    new Vector3(Mathf.Min(2.6f, w / columns * .55f), 2.1f, .14f), M((floor + col + (int)x) % 5 == 0 ? "WindowLight" : "Glass"), false);
            }
        }
        Box("Vertical accent", building, new Vector3(x - w * .5f + .8f, h * .5f, z - d * .5f - .16f),
            new Vector3(.5f, h, .25f), M("Mustard"), false);
    }

    static void BuildStreetFurniture()
    {
        for (int xi = 0; xi < Grid.Length; xi++)
            for (int zi = 1; zi < Grid.Length; zi++)
            {
                int x = Grid[xi], z = Grid[zi];
                if ((xi + zi) % 2 != 0) continue;
                BuildLamp(x + 13, z + 13, (xi + zi) % 4 == 0);
                BuildLamp(x - 13, z - 13, false);
                if ((xi + zi) % 3 == 0) BuildTrafficSignal(x + 9, z - 9);
                if ((xi + zi) % 4 == 0) BuildBench(street, x - 16, z + 15);
                if ((xi + zi) % 5 == 0) BuildHydrant(x - 16, z - 13);
            }
        BuildBusStop(-144, -64, 90);
        BuildBusStop(-64, 64, 0);
        BuildBusStop(96, -64, 90);
        BuildBusStop(176, 144, 0);
        for (int i = 0; i < 34; i++)
        {
            float x = -272 + (i * 67) % 550;
            float z = -145 + (i * 113) % 420;
            if (Mathf.Abs(Mathf.Repeat(x + 40, 80) - 40) < 11 || Mathf.Abs(Mathf.Repeat(z + 40, 80) - 40) < 11) continue;
            Cylinder("Safety bollard", street, new Vector3(x, .5f, z), new Vector3(.31f, .5f, .31f), M("Mustard"), true);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = 75 + i * 31;
            Box("Quay mooring bollard", street, new Vector3(x, .65f, -299), new Vector3(.9f, 1.3f, .9f), M("Metal"));
            BuildLamp(x, -276, false);
        }
    }

    static void BuildContainer(Transform parent, float x, float z, int variant)
    {
        Transform root = Group("Dock container " + variant, parent);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("containerdock", "shippingcontainer");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(10, 2.8f, 4), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
            Box("Container collision", root, new Vector3(x, 1.4f, z), new Vector3(10, 2.8f, 4), M("Slate")).GetComponent<MeshRenderer>().enabled = false;
        }
        else Box("Shipping container", root, new Vector3(x, 1.4f, z), new Vector3(10, 2.8f, 4), M(variant % 2 == 0 ? "OceanBlue" : "Mustard"));
    }

    static void BuildBusStop(float x, float z, float yaw)
    {
        Transform root = Group("Coast Bus Stop", street);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("busstopcoast", "busstop");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(4.4f, 2.8f, 1.9f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else
        {
            Box("Shelter roof", root, root.position + Vector3.up * 2.7f, new Vector3(4.4f, .25f, 1.9f), M("Teal"));
            Box("Shelter back", root, root.position + new Vector3(0, 1.4f, .8f), new Vector3(4.2f, 2.6f, .14f), M("Glass"));
        }
        root.rotation = Quaternion.Euler(0, yaw, 0);
    }

    static void BuildHydrant(float x, float z)
    {
        Transform root = Group("Coral Hydrant", street);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("hydrantcoral", "hydrant");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(.7f, 1.2f, .7f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else Cylinder("Hydrant body", root, new Vector3(x, .55f, z), new Vector3(.31f, .55f, .31f), M("Terracotta"), true);
    }

    static void BuildLamp(float x, float z, bool liveLight)
    {
        Transform root = Group("Street Light", street);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("streetlight", "streetlamp", "lamppost");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(1.1f, 8, 1.1f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else
        {
            Cylinder("Tapered mast", root, new Vector3(x, 3.6f, z), new Vector3(.15f, 3.6f, .15f), M("Metal"));
            Box("Luminaire arm", root, new Vector3(x + 1.05f, 7.1f, z), new Vector3(2.2f, .13f, .16f), M("Metal"), false);
            Box("Warm luminaire", root, new Vector3(x + 2f, 6.95f, z), new Vector3(.7f, .10f, .4f), M("WindowLight"), false);
        }
        if (liveLight)
        {
            GameObject lightGo = new GameObject("Night pool");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.position = new Vector3(x + 1.8f, 6.8f, z);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1, .82f, .58f);
            l.range = 13;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;
            l.enabled = false;
        }
    }

    static void BuildTrafficSignal(float x, float z)
    {
        Transform root = Group("Traffic Signal", street);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("trafficlight", "traffic_light", "signal");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(1.2f, 5, 1.2f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else
        {
            Cylinder("Signal post", root, new Vector3(x, 2.4f, z), new Vector3(.12f, 2.4f, .12f), M("Metal"));
            Box("Signal head", root, new Vector3(x, 4.4f, z), new Vector3(.65f, 1.45f, .55f), M("NearBlack"), false);
            Box("Stop lamp", root, new Vector3(x, 4.85f, z - .29f), new Vector3(.32f, .32f, .08f), M("BeaconRed"), false);
            Box("Go lamp", root, new Vector3(x, 3.95f, z - .29f), new Vector3(.32f, .32f, .08f), M("Leaf"), false);
        }
    }

    static void BuildBench(Transform parent, float x, float z)
    {
        Transform root = Group("Promenade Bench", parent);
        root.position = new Vector3(x, 0, z);
        GameObject model = FindModel("bench");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(2.1f, 1.2f, .9f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else
        {
            Box("Seat", root, new Vector3(x, .6f, z), new Vector3(2.1f, .18f, .65f), M("Boardwalk"));
            Box("Back", root, new Vector3(x, 1.02f, z + .33f), new Vector3(2.1f, .75f, .14f), M("Boardwalk"));
            Box("Left support", root, new Vector3(x - .81f, .29f, z), new Vector3(.13f, .58f, .60f), M("Metal"));
            Box("Right support", root, new Vector3(x + .81f, .29f, z), new Vector3(.13f, .58f, .60f), M("Metal"));
        }
    }

    static void BuildNature()
    {
        for (int i = 0; i < 55; i++)
        {
            float x = -280 + (i * 47) % 550;
            float z = 177 + (i * 23) % 104;
            if (x > 90) continue;
            if (Mathf.Abs(x - (-240 + Mathf.Round((x + 240) / 80f) * 80)) < 13) continue;
            BuildTree(nature, x, z, 5 + i % 4);
        }
        for (int i = 0; i < 24; i++)
        {
            float x = -276 + i * 23;
            if (Mathf.Abs(x) < 12) continue;
            BuildTree(nature, x, -143, 4 + i % 3, true);
        }
        for (int i = 0; i < 16; i++)
        {
            float x = -278 + i * 32;
            Box("Shore boulder", nature, new Vector3(x, .36f, -164 + (i % 3) * 2),
                new Vector3(2.2f + i % 4, .72f, 2.4f), M("SlateLight"));
        }
    }

    static void BuildTree(Transform parent, float x, float z, float height, bool palm = false)
    {
        Transform root = Group("Coastal tree", parent);
        root.position = new Vector3(x, 0, z);
        GameObject model = palm ? FindModel("treepalm", "palm") : FindModel("treecanopy", "tree");
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root, false);
            FitVisual(visual.transform, new Vector3(height * .75f, height, height * .75f), 0);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual);
        }
        else
        {
            Cylinder("Trunk", root, new Vector3(x, height * .34f, z), new Vector3(.25f, height * .34f, .25f), M("Bark"));
            Cylinder("Lower canopy", root, new Vector3(x, height * .65f, z), new Vector3(height * .28f, height * .24f, height * .28f), M("Leaf"));
            Cylinder("Upper canopy", root, new Vector3(x, height * .90f, z), new Vector3(height * .20f, height * .19f, height * .20f), M("LeafGold"));
        }
    }

    static void BuildLandmarksAndLocations()
    {
        CreateLocation("Breakwater Arms | weapon shop", "WeaponShop", new Vector3(-39, .2f, -9), "WEAPONS", "Terracotta");
        CreateLocation("Copperline Customs | garage", "Garage", new Vector3(120, .2f, -105), "GARAGE", "Teal");
        CreateLocation("Westhaven House | safehouse", "Safehouse", new Vector3(-203, .2f, 115), "HOME", "Cream");
        CreateLocation("Civic Medical | hospital", "Hospital", new Vector3(40, .2f, 134), "MEDICAL", "OceanBlue");
        CreateLocation("Luma Apparel | clothing", "Clothing", new Vector3(-39, .2f, -89), "LUMA", "Mustard");
        CreateLocation("North Quay Fuel | gas station", "GasStation", new Vector3(200, .2f, -106), "FUEL", "Mustard");
        CreateLocation("Cape Airfield | aircraft access", "Airfield", new Vector3(120, .2f, 231), "AIRFIELD", "SlateLight");
        CreateLocation("North Quay | dock", "Dock", new Vector3(90, .2f, -223), "MARINA", "OceanBlue");
        CreateLocation("District Police Station", "PoliceStation", new Vector3(40, .2f, 53), "POLICE", "Slate");

        // Unique landmarks make the six districts legible from street level.
        Transform landmark = Group("Seabright Clock Tower", architecture);
        Box("Tower core", landmark, new Vector3(-2, 16, 210), new Vector3(11, 32, 11), M("Cream"));
        Box("Clock crown", landmark, new Vector3(-2, 34, 210), new Vector3(14, 4, 14), M("Slate"));
        Box("Clock face", landmark, new Vector3(-2, 32.8f, 204.4f), new Vector3(5, 5, .18f), M("WindowLight"), false);
        Box("Landmark mast", landmark, new Vector3(-2, 40, 210), new Vector3(.6f, 8, .6f), M("Mustard"));
        Transform crane = Group("North Quay gantry", architecture);
        Box("Gantry legs west", crane, new Vector3(122, 11, -280), new Vector3(1.8f, 22, 1.8f), M("Mustard"));
        Box("Gantry legs east", crane, new Vector3(166, 11, -280), new Vector3(1.8f, 22, 1.8f), M("Mustard"));
        Box("Gantry crossbeam", crane, new Vector3(144, 22, -280), new Vector3(46, 2, 2), M("Mustard"));
        Box("Crane arm", crane, new Vector3(145, 22, -253), new Vector3(2, 2, 54), M("Mustard"));
        Box("Hook cable", crane, new Vector3(145, 15, -237), new Vector3(.12f, 14, .12f), M("NearBlack"), false);
    }

    static void CreateLocation(string name, string kind, Vector3 position, string sign, string colorKey)
    {
        Transform point = Group(name, locations);
        point.position = position;
        Component location = AddComponentByName(point.gameObject, "SandboxLocation", "Kind", kind);
        if (location != null) SetValue(location, "DisplayName", name.Split('|')[0].Trim());
        // A low anchored sign is readable in the 3D world. The point itself is an interaction marker.
        Box(sign + " sign fascia", point, position + new Vector3(0, 3.6f, 1.3f),
            new Vector3(8, 1.2f, .28f), M(colorKey), false);
        Box(sign + " sign inset", point, position + new Vector3(0, 3.6f, 1.49f),
            new Vector3(6.9f, .66f, .08f), M("WindowLight"), false);
        GameObject lettering = new GameObject(sign + " lettering");
        lettering.transform.SetParent(point, false);
        lettering.transform.position = position + new Vector3(0, 3.36f, 1.56f);
        lettering.transform.rotation = Quaternion.identity;
        TextMesh text = lettering.AddComponent<TextMesh>();
        text.text = sign;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = .41f;
        text.fontSize = 64;
        text.color = new Color(.08f, .14f, .17f);
    }

    static void BuildTrafficRoutes()
    {
        Route("Residential Loop", new Vector2(-237, -77), new Vector2(-237, 163),
            new Vector2(-163, 163), new Vector2(-163, -77));
        Route("Downtown Circuit", new Vector2(-77, 3), new Vector2(-77, 163),
            new Vector2(83, 163), new Vector2(83, 3));
        Route("Industrial Circuit", new Vector2(83, -157), new Vector2(243, -157),
            new Vector2(243, 83), new Vector2(83, 83));
        Route("Harbor Arterial", new Vector2(-237, -77), new Vector2(243, -77),
            new Vector2(243, 163), new Vector2(-237, 163));
        Route("Port Service", new Vector2(83, -157), new Vector2(83, -237),
            new Vector2(237, -237), new Vector2(237, -157));
        Route("Airfield Access", new Vector2(83, 83), new Vector2(83, 243),
            new Vector2(163, 243), new Vector2(163, 83));
    }

    static void Route(string name, params Vector2[] points)
    {
        Transform route = Group("Route | " + name, routes);
        Transform[] waypoints = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Transform wp = Group("WP_" + i.ToString("00"), route);
            wp.position = new Vector3(points[i].x, .2f, points[i].y);
            waypoints[i] = wp;
        }
        RoutePaths.Add(waypoints);
    }

    static void BuildGameplayRoots()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0, .28f, -147);
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1.05f, 0);
        cc.height = 2.1f;
        cc.radius = .37f;
        cc.stepOffset = .32f;
        AttachCharacterVisual(player.transform, "hlplayernova", "player", "hero", "protagonist");
        AttachWeaponVisual(player.transform, "pistolwave9", "Equipped Pistol Visual", true);
        AttachWeaponVisual(player.transform, "carbinebreakwater", "Stowed Carbine Visual", false);
        AttachWeaponVisual(player.transform, "smgcurrent", "Stowed SMG Visual", false);
        AttachWeaponVisual(player.transform, "shotgunbreaker", "Stowed Shotgun Visual", false);
        AddComponentByName(player, "SandboxPlayer");
        AddComponentByName(player, "SandboxArsenal");

        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = player.transform.position + new Vector3(0, 3.2f, -6.6f);
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.fieldOfView = 67;
        camera.nearClipPlane = .08f;
        camera.farClipPlane = 620;
        cameraGo.AddComponent<AudioListener>();
        AddComponentByName(cameraGo, "SandboxCamera");

        GameObject systems = new GameObject("Harborline Systems");
        AddComponentByName(systems, "SandboxDirector");
        AddComponentByName(systems, "SandboxHUD");
    }

    static void BuildInitialPopulation()
    {
        Transform npcSpawns = Group("NPC Spawn Points", world);
        for (int i = 0; i < 28; i++)
        {
            float x = -260 + (i * 71) % 520;
            float z = -135 + (i * 89) % 398;
            if (z > 175 && x > 95) z = 129;
            float snapX = Mathf.Round(x / 80f) * 80;
            float snapZ = Mathf.Round(z / 80f) * 80;
            if (Mathf.Abs(x - snapX) < 20) x = snapX + (x < snapX ? -16 : 16);
            if (Mathf.Abs(z - snapZ) < 20) z = snapZ + (z < snapZ ? -16 : 16);
            Transform spawn = Group("Pedestrian spawn " + i.ToString("00"), npcSpawns);
            spawn.position = new Vector3(x, .25f, z);
            if (i < 20) SpawnPedestrian("Civilian " + i.ToString("00"), "Civilian", spawn.position, i);
        }
        SpawnPedestrian("Police Officer 01", "Police", new Vector3(56, .22f, 47), 101);
        SpawnPedestrian("Police Officer 02", "Police", new Vector3(33, .22f, 47), 102);
        SpawnPedestrian("Tactical Officer at Station", "Tactical", new Vector3(53, .22f, 35), 105);
        SpawnPedestrian("Arms Shopkeeper", "Shopkeeper", new Vector3(-47, .22f, -37), 103);
        SpawnPedestrian("Garage Mechanic", "Shopkeeper", new Vector3(123, .22f, -112), 104);

        Transform vehicleSpawns = Group("Vehicle Spawn Points", world);
        Vector3[] trafficPositions =
        {
            new Vector3(-237,.23f,-77), new Vector3(-163,.23f,163), new Vector3(-77,.23f,3),
            new Vector3(83,.23f,163), new Vector3(83,.23f,-157), new Vector3(243,.23f,83),
            new Vector3(243,.23f,-77), new Vector3(243,.23f,163), new Vector3(83,.23f,-237),
            new Vector3(163,.23f,243)
        };
        int[] trafficRoutes = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 5 };
        float[] trafficHeading = { 0, 180, 0, 180, 90, 270, 0, 270, 90, 180 };
        for (int i = 0; i < trafficPositions.Length; i++)
        {
            Transform spawn = Group("Traffic spawn " + i.ToString("00"), vehicleSpawns);
            spawn.position = trafficPositions[i];
            string[] categories = { "Compact", "Pickup", "SportsCar", "Van", "Pickup", "Compact", "SportsCar", "PoliceCruiser", "Van", "Compact" };
            string category = categories[i];
            SpawnVehicle("Traffic " + category + " " + i.ToString("00"), category,
                spawn.position, trafficHeading[i], true, trafficRoutes[i]);
        }
        for (int i = 0; i < 9; i++)
        {
            float x = -205 + (i % 3) * 82;
            float z = -120 + (i / 3) * 82;
            string[] categories = { "Van", "SportsCar", "Compact", "Pickup" };
            string category = categories[i % categories.Length];
            SpawnVehicle("Parked " + category + " " + i.ToString("00"), category,
                new Vector3(x, .23f, z), 90, false);
        }
        SpawnVehicle("Police Cruiser at Station", "PoliceCruiser", new Vector3(22, .23f, 53), 0, false);
        SpawnVehicle("Motorcycle at Quay", "Motorcycle", new Vector3(134, .23f, -151), 180, false);
        SpawnVehicle("Speedboat at Marina", "Boat", new Vector3(-198, -.04f, -204), 180, false);
        SpawnVehicle("Civil Helicopter at Cape", "Helicopter", new Vector3(120, .25f, 178), 0, false);
        SpawnVehicle("Prop Plane at Cape", "Airplane", new Vector3(212, .25f, 251), 0, false);
    }

    static void SpawnPedestrian(string name, string role, Vector3 position, int visualVariant)
    {
        GameObject npc = new GameObject(name);
        npc.transform.SetParent(characters, false);
        npc.transform.position = position;
        CharacterController cc = npc.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1.0f, 0);
        cc.height = 2.0f;
        cc.radius = .34f;
        cc.stepOffset = .28f;
        if (role == "Police") AttachCharacterVisual(npc.transform, "policeofficer", "officer");
        else if (role == "Tactical") AttachCharacterVisual(npc.transform, "policetactical", "tactical");
        else AttachCharacterVisual(npc.transform, visualVariant % 2 == 0 ? "pedestrian01" : "pedestrian02", "pedestrian", "civilian");
        AddComponentByName(npc, "SandboxPedestrian", "Role", role);
    }

    static void SpawnVehicle(string name, string category, Vector3 position, float yaw, bool traffic, int routeIndex = -1)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(vehicles, false);
        root.transform.position = position;
        root.transform.rotation = Quaternion.Euler(0, yaw, 0);
        bool twoWheels = category == "Motorcycle";
        bool boat = category == "Boat";
        bool aircraft = category == "Helicopter" || category == "Airplane";
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.size = boat ? new Vector3(2.8f, 1.4f, 6.4f) : aircraft ? new Vector3(5.8f, 2.1f, 7.7f) : twoWheels ? new Vector3(.85f, 1.4f, 2.4f) :
            category == "Van" ? new Vector3(2.2f, 2.2f, 5.2f) : category == "Pickup" ? new Vector3(2.1f, 1.75f, 5.0f) :
            category == "SportsCar" ? new Vector3(1.95f, 1.2f, 4.6f) : new Vector3(1.9f, 1.45f, 4.4f);
        box.center = new Vector3(0, boat ? .45f : aircraft ? 1.2f : box.size.y * .56f, 0);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = aircraft ? 1900 : boat ? 900 : twoWheels ? 240 : 1250;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = !traffic && (boat || aircraft);
        string[] hints = category switch
        {
            "PoliceCruiser" => new[] { "carpoliceinterceptor", "policecruiser", "policecar" },
            "Compact" => new[] { "carcoralrunner" },
            "SportsCar" => new[] { "carsprintgt", "sportscar" },
            "Pickup" => new[] { "cardocksidepickup", "pickup" },
            "Van" => new[] { "vandelivery", "van" },
            "Motorcycle" => new[] { "motorcycle", "bike" },
            "Helicopter" => new[] { "helicopter", "heli" },
            "Airplane" => new[] { "airplane", "prop_plane", "plane" },
            "Boat" => new[] { "speedboat", "motorboat", "boat" },
            _ => new[] { category.ToLowerInvariant() }
        };
        GameObject model = FindModel(hints);
        GameObject importedVisual = null;
        if (model != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            importedVisual = visual;
            visual.name = "Blender visual | " + model.name;
            visual.transform.SetParent(root.transform, false);
            FitVisual(visual.transform, box.size * .94f, .1f);
            RemoveModelColliders(visual);
            ConvertModelMaterials(visual, category == "Compact" ? Mathf.Abs(Mathf.RoundToInt(position.x + position.z)) % 5 : -1);
        }
        else Debug.LogWarning("Harborline vehicle has no Blender visual yet: " + name + " (" + category + ")");
        Component vehicle = AddComponentByName(root, "SandboxVehicle");
        if (vehicle != null)
        {
            SetEnum(vehicle, "kind", category == "Motorcycle" ? "Motorcycle" :
                category == "Boat" ? "Boat" : category == "Helicopter" ? "Helicopter" :
                category == "Airplane" ? "Plane" : "Car");
            SetValue(vehicle, "isPolice", category == "PoliceCruiser");
            SetValue(vehicle, "chassisSize", box.size);
            if (category == "SportsCar") { SetValue(vehicle, "engineUpgrade", 2); SetValue(vehicle, "brakeUpgrade", 1); }
            if (category == "Pickup" || category == "Van") SetValue(vehicle, "armorUpgrade", 1);
            if (boat) SetValue(vehicle, "waterLevel", 0f);
            if (importedVisual != null)
            {
                Transform rotor = FindVisualTransform(importedVisual.transform, "rotormain", "mainrotor");
                if (rotor != null) SetValue(vehicle, "rotor", rotor);
                Transform[] wheelVisuals =
                {
                    FindVisualTransform(importedVisual.transform, "wheelfl"),
                    FindVisualTransform(importedVisual.transform, "wheelfr"),
                    FindVisualTransform(importedVisual.transform, "wheelrl"),
                    FindVisualTransform(importedVisual.transform, "wheelrr")
                };
                if (wheelVisuals.Any(t => t != null)) SetValue(vehicle, "wheelVisuals", wheelVisuals);
            }
            if (!boat && !aircraft) AddVehicleLights(root.transform, vehicle, box.size, category == "PoliceCruiser");
        }
        AddComponentByName(root, "SandboxVehicleFeedback");
        if (traffic)
        {
            Component agent = AddComponentByName(root, "SandboxTrafficAgent");
            if (agent != null)
            {
                if (category == "PoliceCruiser") SetValue(agent, "Emergency", true);
                if (routeIndex >= 0 && routeIndex < RoutePaths.Count)
                    SetValue(agent, "Waypoints", RotateRouteToVehicle(RoutePaths[routeIndex], position));
            }
        }
    }

    static Transform[] RotateRouteToVehicle(Transform[] route, Vector3 position)
    {
        int nearest = 0;
        float best = float.MaxValue;
        for (int i = 0; i < route.Length; i++)
        {
            float distance = (route[i].position - position).sqrMagnitude;
            if (distance >= best) continue;
            nearest = i;
            best = distance;
        }
        Transform[] result = new Transform[route.Length];
        for (int i = 0; i < route.Length; i++) result[i] = route[(nearest + i + 1) % route.Length];
        return result;
    }

    static Transform FindVisualTransform(Transform root, params string[] names)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            string name = Normalize(transform.name);
            foreach (string expected in names) if (name.Contains(expected)) return transform;
        }
        return null;
    }

    static void AddVehicleLights(Transform root, Component vehicle, Vector3 size, bool police)
    {
        float x = Mathf.Min(.78f, size.x * .34f);
        float y = Mathf.Max(.72f, size.y * .57f);
        float front = size.z * .49f;
        float back = -size.z * .49f;
        Light[] head =
        {
            VehicleLight(root, "Headlamp L", new Vector3(-x, y, front), Color.white, LightType.Spot, 25, 2.3f, 0),
            VehicleLight(root, "Headlamp R", new Vector3(x, y, front), Color.white, LightType.Spot, 25, 2.3f, 0)
        };
        Light[] brake =
        {
            VehicleLight(root, "Brake lamp L", new Vector3(-x, y, back), new Color(1f, .12f, .08f), LightType.Spot, 6, 1.2f, 180),
            VehicleLight(root, "Brake lamp R", new Vector3(x, y, back), new Color(1f, .12f, .08f), LightType.Spot, 6, 1.2f, 180)
        };
        SetValue(vehicle, "headlights", head);
        SetValue(vehicle, "brakeLights", brake);
        if (police)
        {
            Light[] emergency =
            {
                VehicleLight(root, "Emergency red", new Vector3(-.48f, size.y + .22f, 0), new Color(1, .09f, .05f), LightType.Point, 14, 3.2f, 0),
                VehicleLight(root, "Emergency blue", new Vector3(.48f, size.y + .22f, 0), new Color(.06f, .33f, 1), LightType.Point, 14, 3.2f, 0)
            };
            SetValue(vehicle, "policeLights", emergency);
        }
    }

    static Light VehicleLight(Transform parent, string name, Vector3 localPosition, Color color,
        LightType type, float range, float intensity, float yaw)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        Light light = go.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        if (type == LightType.Spot) light.spotAngle = 54;
        light.enabled = false;
        return light;
    }

    static void AttachCharacterVisual(Transform root, params string[] hints)
    {
        GameObject model = FindModel(hints);
        if (model == null) { Debug.LogWarning("Harborline character has no Blender visual yet: " + root.name); return; }
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = "Blender visual | " + model.name;
        visual.transform.SetParent(root, false);
        FitVisual(visual.transform, new Vector3(.72f, 1.88f, .55f), 0);
        RemoveModelColliders(visual);
        ConvertModelMaterials(visual);
    }

    static void AttachWeaponVisual(Transform player, string modelHint, string name, bool active)
    {
        GameObject model = FindModel(modelHint);
        if (model == null) return;
        Transform anchor = player.childCount > 0 ? player.GetChild(0) : player;
        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        visual.name = name;
        visual.transform.SetParent(anchor, false);
        FitVisual(visual.transform, modelHint.Contains("carbine") ? new Vector3(.25f, .27f, .77f) : new Vector3(.32f, .24f, .48f), 0);
        visual.transform.localPosition = modelHint.Contains("carbine") ? new Vector3(-.21f, .86f, -.2f) : new Vector3(.32f, 1.03f, .13f);
        visual.transform.localRotation = Quaternion.Euler(10, 5, modelHint.Contains("carbine") ? -18 : 8);
        RemoveModelColliders(visual);
        ConvertModelMaterials(visual);
        visual.SetActive(active);
    }

    static GameObject FindModel(params string[] hints)
    {
        foreach (string hint in hints)
        {
            if (ModelCache.TryGetValue(hint, out GameObject cached))
            {
                if (cached != null) return cached;
                continue;
            }
            string normalizedHint = Normalize(hint);
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelPath });
            // Exact basename wins over substring matches, e.g. the static Nova asset
            // must not accidentally resolve to the optional Nova_Rigged FBX.
            for (int pass = 0; pass < 2; pass++)
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Normalize(Path.GetFileNameWithoutExtension(path));
                bool exact = filename == normalizedHint || filename == "hl" + normalizedHint;
                if (pass == 0 && !exact || pass == 1 && !filename.Contains(normalizedHint)) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                ModelCache[hint] = prefab;
                return prefab;
            }
            ModelCache[hint] = null;
        }
        return null;
    }

    static string Normalize(string value) => new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    static void FitVisual(Transform visual, Vector3 desiredSize, float bottom)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 size = bounds.size;
        float scale = Mathf.Min(desiredSize.x / Mathf.Max(.001f, size.x),
            desiredSize.y / Mathf.Max(.001f, size.y), desiredSize.z / Mathf.Max(.001f, size.z));
        visual.localScale *= scale;
        renderers = visual.GetComponentsInChildren<Renderer>(true);
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 offset = visual.position - bounds.center;
        visual.position += new Vector3(offset.x, bottom - bounds.min.y, offset.z);
    }

    static void RemoveModelColliders(GameObject visual)
    {
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
    }

    static void ConvertModelMaterials(GameObject visual, int bodyVariant = -1)
    {
        Color[] carColors =
        {
            new Color(.91f, .33f, .28f), new Color(.15f, .51f, .61f),
            new Color(.77f, .78f, .70f), new Color(.70f, .54f, .27f),
            new Color(.29f, .37f, .43f)
        };
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] old = renderer.sharedMaterials;
            Material[] converted = new Material[old.Length];
            for (int i = 0; i < old.Length; i++)
            {
                Material original = old[i];
                if (original == null) { converted[i] = M("PaleConcrete"); continue; }
                Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                    original.HasProperty("_Color") ? original.GetColor("_Color") : Color.gray;
                string key = "Blender_" + Normalize(original.name);
                if (bodyVariant >= 0 && Normalize(original.name).Contains("coral"))
                {
                    key = "VehiclePaint_" + bodyVariant;
                    color = carColors[bodyVariant];
                }
                converted[i] = Mat(key, color, .24f);
            }
            renderer.sharedMaterials = converted;
        }
    }

    static Component AddComponentByName(GameObject go, string className, string enumMember = null, string enumValue = null)
    {
        Type found = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(t => t.Name == className && !t.IsAbstract);
        if (found == null) { Debug.LogWarning("Runtime type not compiled yet: " + className); return null; }
        Component component = go.GetComponent(found) ?? go.AddComponent(found);
        if (!string.IsNullOrEmpty(enumMember)) SetEnum(component, enumMember, enumValue);
        return component;
    }

    static void SetEnum(Component component, string member, string value)
    {
        Type type = component.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        FieldInfo field = type.GetField(member, flags);
        if (field != null && field.FieldType.IsEnum)
        {
            try { field.SetValue(component, Enum.Parse(field.FieldType, value, true)); EditorUtility.SetDirty(component); }
            catch (ArgumentException) { Debug.LogWarning("Unknown enum value " + value + " for " + type.Name + "." + member); }
            return;
        }
        PropertyInfo property = type.GetProperty(member, flags);
        if (property != null && property.PropertyType.IsEnum && property.CanWrite)
        {
            try { property.SetValue(component, Enum.Parse(property.PropertyType, value, true)); EditorUtility.SetDirty(component); }
            catch (ArgumentException) { Debug.LogWarning("Unknown enum value " + value + " for " + type.Name + "." + member); }
        }
    }

    static void SetValue(Component component, string member, object value)
    {
        Type type = component.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        FieldInfo field = type.GetField(member, flags);
        if (field != null && field.FieldType.IsAssignableFrom(value.GetType()))
        {
            field.SetValue(component, value);
            EditorUtility.SetDirty(component);
            return;
        }
        PropertyInfo property = type.GetProperty(member, flags);
        if (property != null && property.CanWrite && property.PropertyType.IsAssignableFrom(value.GetType()))
        {
            property.SetValue(component, value);
            EditorUtility.SetDirty(component);
        }
    }

    static void ConfigureLighting()
    {
        GameObject sunGo = new GameObject("Harborline Sun");
        sunGo.transform.rotation = Quaternion.Euler(42, -36, 0);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1, .91f, .75f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.54f, .66f, .69f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.62f, .76f, .78f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 135;
        RenderSettings.fogEndDistance = 550;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
    }
}
