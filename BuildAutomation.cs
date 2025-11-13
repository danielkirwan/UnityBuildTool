using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildAutomation
{
    [MenuItem("Build/Test Batch")]
    public static void BuildPC()
    {
        try
        {
            Debug.Log("[BuildAutomation] Method invoked! Project path: " + Application.dataPath);

            string[] args = Environment.GetCommandLineArgs();

            string buildPath = "Builds/WindowsBuild";
            string buildName = "MyGame";
            string version = "1.0.0";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildPath" && i + 1 < args.Length) buildPath = args[i + 1];
                if (args[i] == "-buildName" && i + 1 < args.Length) buildName = args[i + 1];
                if (args[i] == "-version" && i + 1 < args.Length) version = args[i + 1];
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-buildPath" && i + 1 < args.Length) buildPath = args[i + 1];
                if (args[i] == "-buildName" && i + 1 < args.Length) buildName = args[i + 1];
                if (args[i] == "-version" && i + 1 < args.Length) version = args[i + 1];
            }

            Directory.CreateDirectory(buildPath);

            string fullPath = Path.Combine(buildPath, $"{buildName}.exe");

            Debug.Log("[BuildAutomation] Starting build...");
            Debug.Log("[BuildAutomation] Output: " + fullPath);
            Debug.Log("[BuildAutomation] Version: " + version);

            string[] scenes = Array.FindAll(EditorBuildSettings.scenes, s => s.enabled).Select(s => s.path).ToArray();

            Debug.Log("[BuildAutomation] Enabled scenes count: " + scenes.Length);
            Debug.Log("[BuildAutomation] Scenes: " + string.Join(", ", scenes));  

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildAutomation] No scenes enabled�cannot build! Add them in File > Build Settings.");
                return;  
            }

            Debug.Log("[BuildAutomation] Starting build to: " + fullPath);
            BuildReport report = BuildPipeline.BuildPlayer(
                scenes,
                fullPath,
                BuildTarget.StandaloneWindows64,
                BuildOptions.None
            );

            Debug.Log("[BuildAutomation] Build result: " + report.summary.result);
            Debug.Log("[BuildAutomation] Total time: " + report.summary.totalTime);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[BuildAutomation] Build failed! Result: " + report.summary.result);
                if (args.Any(a => a == "-batchmode"))
                {
                    EditorApplication.Exit(1);
                }
            }
            else
            {
                Debug.Log("[BuildAutomation] Build succeeded! Check: " + fullPath);
                if (args.Any(a => a == "-batchmode"))
                {
                    EditorApplication.Exit(0);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[BuildAutomation] Exception during build: " + e.Message + "\nStack trace: " + e.StackTrace);
            if (Environment.GetCommandLineArgs().Any(a => a == "-batchmode"))
            {
                EditorApplication.Exit(1);
            }
        }
    }
}

