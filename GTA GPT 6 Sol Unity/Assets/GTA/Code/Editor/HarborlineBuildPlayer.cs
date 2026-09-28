using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HarborlineBuildPlayer
{
    [MenuItem("GTA/Build Windows Player")]
    public static void BuildWindows()
    {
        BuildPlayer(BuildTarget.StandaloneWindows64, "Builds/Harborline-Windows/Harborline.exe", "Windows");
    }

    [MenuItem("GTA/Build macOS Player")]
    public static void BuildMacOS()
    {
        const string profilePath = "Assets/Settings/Build Profiles/macOS Universal.asset";
        const string outputPath = "Builds/Harborline-macOS/Harborline.app";
        BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
        if (profile == null)
            throw new System.Exception("Missing macOS Universal build profile: " + profilePath);

        Directory.CreateDirectory("Builds/Harborline-macOS");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = outputPath
        });
        EnsureBuildSucceeded(report, "macOS");
    }

    [MenuItem("GTA/Build Windows + macOS Players")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildMacOS();
    }

    // Kept for compatibility with older command-line instructions.
    public static void Build()
    {
        BuildWindows();
    }

    private static void BuildPlayer(BuildTarget target, string outputPath, string platformName)
    {
        const string scenePath = "Assets/GTA/Scenes/Harborline.unity";
        if (!File.Exists(scenePath)) HarborlineBuilder.Build();

        string outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        EnsureBuildSucceeded(BuildPipeline.BuildPlayer(options), platformName);
    }

    private static void EnsureBuildSucceeded(BuildReport report, string platformName)
    {
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Harborline " + platformName + " build failed: " + report.summary.result);
        Debug.Log(
            "HARBORLINE " + platformName.ToUpperInvariant() + " BUILD OK: " +
            report.summary.outputPath + " bytes=" + report.summary.totalSize);
    }
}
