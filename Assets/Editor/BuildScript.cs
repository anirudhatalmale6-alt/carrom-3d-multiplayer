using UnityEditor;
using UnityEngine;

public class BuildScript
{
    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        string[] scenes = new string[] {
            "Assets/Carrom/Scenes/Menu.unity",
            "Assets/Carrom/Scenes/Game.unity",
            "Assets/Carrom/Scenes/Practice.unity"
        };

        string buildPath = "/var/lib/freelancer/projects/40107975/WebGL-Build-Final";

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log("Starting WebGL build to: " + buildPath);

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
        }
        else
        {
            Debug.LogError("Build failed with " + report.summary.totalErrors + " errors");
        }
    }
}
