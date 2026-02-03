using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildWebGL
{
    [MenuItem("Build/Build WebGL")]
    public static void Build()
    {
        // Get all scenes from build settings
        string[] scenes = new string[] {
            "Assets/Carrom/Scenes/Menu.unity",
            "Assets/Carrom/Scenes/Game.unity"
        };

        string buildPath = "../../../WebGL-Build";

        // Ensure the directory exists
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        BuildPipeline.BuildPlayer(buildPlayerOptions);

        Debug.Log("WebGL Build Complete!");
    }
}
