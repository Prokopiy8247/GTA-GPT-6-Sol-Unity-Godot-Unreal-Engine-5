using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Read-only import audit for the optional rigged Nova character.</summary>
public static class NovaRigAudit
{
    const string PathInProject = "Assets/GTA/Generated/Models/HL_Player_Nova_Rigged.fbx";

    [MenuItem("GTA/Audit Optional Nova Rig")]
    public static void Run()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathInProject);
        if (prefab == null) throw new Exception("Rigged player FBX was not imported: " + PathInProject);

        ModelImporter importer = AssetImporter.GetAtPath(PathInProject) as ModelImporter;
        SkinnedMeshRenderer[] skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(PathInProject)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string[] report = new[]
        {
            "FBX: " + PathInProject,
            "Importer animation type: " + (importer != null ? importer.animationType.ToString() : "unavailable"),
            "Import animation enabled: " + (importer != null && importer.importAnimation),
            "SkinnedMeshRenderers: " + skins.Length,
            "Total skin bones: " + skins.Sum(s => s.bones != null ? s.bones.Length : 0),
            "Skins with mesh: " + skins.Count(s => s.sharedMesh != null),
            "Skins with root bone: " + skins.Count(s => s.rootBone != null),
            "Null material slots: " + skins.Sum(s => s.sharedMaterials.Count(m => m == null)),
            "Imported AnimationClips: " + clips.Length
        }.Concat(clips.Select(c => "Clip: " + c.name + " | seconds=" + c.length.ToString("F3") +
            " | curves=" + AnimationUtility.GetCurveBindings(c).Length)).ToArray();
        Directory.CreateDirectory(".sol-run");
        File.WriteAllLines(".sol-run/nova_rig_audit.txt", report);
        Debug.Log("NOVA RIG AUDIT\n" + string.Join("\n", report));
    }
}
