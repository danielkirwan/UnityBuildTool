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
        Debug.Log("[BuildAutomation] Method invoked! Project path: " + Application.dataPath);

        string[] args = Environment.GetCommandLineArgs();

        string buildPath = "Builds/WindowsBuild";
        string buildName = "MyGame";
        string version = "1.0.0";
        string buildType = "release";

        // Parse arguments using stable "-key=value" pattern
        foreach (var arg in args)
        {
            if (arg.StartsWith("-buildPath="))
                buildPath = arg.Substring("-buildPath=".Length);

            if (arg.StartsWith("-buildName="))
                buildName = arg.Substring("-buildName=".Length);

            if (arg.StartsWith("-version="))
                version = arg.Substring("-version=".Length);

            if (arg.StartsWith("-buildType="))
                buildType = arg.Substring("-buildType=".Length);
        }

        Debug.Log($"[BuildAutomation] Build Path: {buildPath}");
        Debug.Log($"[BuildAutomation] Build Name: {buildName}");
        Debug.Log($"[BuildAutomation] Version: {version}");
        Debug.Log($"[BuildAutomation] Build Type: {buildType}");

        try
        {
            PlayerSettings.bundleVersion = version;
            Debug.Log($"[BuildAutomation] Applied PlayerSettings.bundleVersion = {version}");

            string versionedPath = Path.Combine(buildPath, $"v{version}");
            Directory.CreateDirectory(versionedPath);

            string fullExePath = Path.Combine(versionedPath, $"{buildName}_v{version}.exe");

            File.WriteAllText(Path.Combine(versionedPath, "version.json"),
                $"{{\n  \"version\": \"{version}\",\n  \"buildName\": \"{buildName}\",\n  \"buildType\": \"{buildType}\",\n  \"date\": \"{DateTime.Now}\" \n}}"
            );

            // Get enabled scenes
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildAutomation] No scenes enabled in Build Settings!");
                if (args.Any(a => a == "-batchmode"))
                    EditorApplication.Exit(1);
                return;
            }

            // Development / Release flags
            BuildOptions options = BuildOptions.None;

            if (buildType.Equals("development", StringComparison.OrdinalIgnoreCase))
            {
                options = BuildOptions.Development |
                          BuildOptions.AllowDebugging |
                          BuildOptions.ConnectWithProfiler;

                Debug.Log("[BuildAutomation] Development flags enabled.");
            }

            Debug.Log("[BuildAutomation] Starting build...");
            Debug.Log("[BuildAutomation] Output File: " + fullExePath);

            BuildReport report = BuildPipeline.BuildPlayer(
                scenes,
                fullExePath,
                BuildTarget.StandaloneWindows64,
                options
            );

            Debug.Log($"[BuildAutomation] Build Result: {report.summary.result}");
            Debug.Log($"[BuildAutomation] Total Build Time: {report.summary.totalTime}");

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[BuildAutomation] Build FAILED!");
                if (args.Any(a => a == "-batchmode"))
                    EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[BuildAutomation] Build completed successfully.");
                Debug.Log("[BuildAutomation] File location: " + fullExePath);

                if (args.Any(a => a == "-batchmode"))
                    EditorApplication.Exit(0);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[BuildAutomation] ERROR: " + ex.Message);
            Debug.LogError(ex.StackTrace);

            if (args.Any(a => a == "-batchmode"))
                EditorApplication.Exit(1);
        }
    }
}
