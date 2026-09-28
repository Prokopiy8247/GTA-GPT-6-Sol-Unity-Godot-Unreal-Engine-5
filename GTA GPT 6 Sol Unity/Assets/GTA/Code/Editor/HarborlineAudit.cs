using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Scene and imported-asset validation used after each generation pass.</summary>
public static class HarborlineAudit
{
    [MenuItem("GTA/Audit Harborline Scene")]
    public static void Run()
    {
        const string path = "Assets/GTA/Scenes/Harborline.unity";
        if (!File.Exists(path)) throw new System.Exception("Harborline scene has not been generated");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var player = Object.FindFirstObjectByType<SandboxPlayer>();
        var camera = Object.FindFirstObjectByType<SandboxCamera>();
        var director = Object.FindFirstObjectByType<SandboxDirector>();
        var hud = Object.FindFirstObjectByType<SandboxHUD>();
        var vehicles = Object.FindObjectsByType<SandboxVehicle>(FindObjectsSortMode.None);
        var actors = Object.FindObjectsByType<SandboxPedestrian>(FindObjectsSortMode.None);
        var locations = Object.FindObjectsByType<SandboxLocation>(FindObjectsSortMode.None);
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var missing = new List<string>();
        if (player == null) missing.Add("player");
        if (camera == null) missing.Add("camera");
        if (director == null) missing.Add("director");
        if (hud == null) missing.Add("HUD");
        if (player != null && player.GetComponent<SandboxArsenal>() == null) missing.Add("arsenal");
        if (actors.Length < 15) missing.Add("pedestrian population");
        if (vehicles.Length < 10) missing.Add("vehicle population");
        if (locations.Length < 6) missing.Add("world services");
        int architectureActors = 0;
        Transform architecture = GameObject.Find("District Architecture")?.transform;
        if (architecture != null)
        {
            foreach (Transform child in architecture.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject) != child.gameObject) continue;
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);
                string modelName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
                bool actorModel = modelName.Contains("player") || modelName.Contains("pedestrian")
                    || modelName.Contains("police_officer") || modelName.Contains("police_tactical");
                if (!actorModel) continue;
                architectureActors++;
                Renderer[] actorRenderers = child.GetComponentsInChildren<Renderer>(true);
                float height = 0f;
                if (actorRenderers.Length > 0)
                {
                    Bounds bounds = actorRenderers[0].bounds;
                    foreach (Renderer actorRenderer in actorRenderers.Skip(1)) bounds.Encapsulate(actorRenderer.bounds);
                    height = bounds.size.y;
                }
                missing.Add("actor FBX under architecture: " + modelName + " at " + child.position
                    + " height=" + height.ToString("F1") + "m");
            }
        }
        else missing.Add("architecture root");

        int spawnBlockers = 0;
        bool groundSupported = false;
        if (player != null)
        {
            Physics.SyncTransforms();
            CharacterController capsule = player.GetComponent<CharacterController>();
            if (capsule != null)
            {
                Vector3 center = player.transform.TransformPoint(capsule.center);
                float halfSegment = Mathf.Max(0f, capsule.height * .5f - capsule.radius);
                Vector3 top = center + Vector3.up * halfSegment;
                Vector3 bottom = center - Vector3.up * halfSegment;
                Collider[] nearby = Physics.OverlapCapsule(top, bottom, capsule.radius * .96f, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider collider in nearby)
                {
                    if (collider.transform.IsChildOf(player.transform)) continue;
                    spawnBlockers++;
                    missing.Add("player spawn intersects " + collider.name + " at " + collider.transform.position);
                }
            }
            RaycastHit[] groundHits = Physics.RaycastAll(player.transform.position + Vector3.up * .7f,
                Vector3.down, 2.3f, ~0, QueryTriggerInteraction.Ignore);
            groundSupported = groundHits.Any(hit => !hit.collider.transform.IsChildOf(player.transform)
                && hit.normal.y > .5f && hit.point.y <= player.transform.position.y + .08f);
            if (!groundSupported) missing.Add("player spawn has no walkable ground within 1.6m");
        }
        int curbBlockers = 0;
        GameObject roadRoot = GameObject.Find("Roads and Promenade");
        if (roadRoot != null)
        {
            int[] grid = { -240, -160, -80, 0, 80, 160, 240 };
            foreach (Collider curb in roadRoot.GetComponentsInChildren<Collider>(true))
            {
                if (!curb.name.Contains("curb")) continue;
                foreach (int x in grid)
                foreach (int z in grid.Where(value => value >= -160))
                {
                    Bounds crossing = new Bounds(new Vector3(x, .13f, z), new Vector3(7f, .5f, 7f));
                    if (!curb.bounds.Intersects(crossing)) continue;
                    curbBlockers++;
                    if (curbBlockers <= 8) missing.Add("curb blocks crossing x=" + x + " z=" + z + ": " + curb.name);
                }
            }
            if (curbBlockers > 8) missing.Add("additional blocked curb crossings: " + (curbBlockers - 8));
        }
        else missing.Add("roads root");
        foreach (SandboxVehicle.VehicleKind kind in System.Enum.GetValues(typeof(SandboxVehicle.VehicleKind)))
            if (!vehicles.Any(v => v.Kind == kind)) missing.Add("vehicle class " + kind);
        int missingMaterial = 0, importedModels = 0;
        foreach (var renderer in renderers)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(renderer.gameObject)) importedModels++;
            foreach (var material in renderer.sharedMaterials)
                if (material == null || material.shader == null || material.shader.name.Contains("InternalErrorShader")) missingMaterial++;
        }
        if (missingMaterial > 0) missing.Add("unsupported/missing materials: " + missingMaterial);
        bool freeRoamFirst = EditorBuildSettings.scenes.Length > 0 && EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path == path;
        if (!freeRoamFirst) missing.Add("build scene order");
        string summary = "HARBORLINE AUDIT player=" + (player != null) + " vehicles=" + vehicles.Length
            + " actors=" + actors.Length + " locations=" + locations.Length + " renderers=" + renderers.Length
            + " importedModelRenderers=" + importedModels + " missingMaterial=" + missingMaterial
            + " curbBlockers=" + curbBlockers
            + " architectureActors=" + architectureActors + " spawnBlockers=" + spawnBlockers
            + " spawnGround=" + groundSupported
            + " firstBuildScene=" + freeRoamFirst + " issues=" + missing.Count;
        Debug.Log(summary);
        Directory.CreateDirectory(".sol-run");
        File.WriteAllLines(".sol-run/scene_audit.txt", new[] { summary }.Concat(missing.Select(x => "ISSUE: " + x)));
        if (missing.Count > 0) throw new System.Exception("Harborline audit failed: " + string.Join(", ", missing));
    }
}
