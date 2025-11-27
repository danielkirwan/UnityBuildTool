using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class IconAutomation
{
    public static void ApplyIcons()
    {
        Debug.Log("========== ICON AUTOMATION START ==========");

        string[] args = System.Environment.GetCommandLineArgs();
        string iconFolder = null;

        // ----------------------------------------------------------
        // Accept BOTH argument formats:
        // ----------------------------------------------------------
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (a.StartsWith("-iconsRootPath=", System.StringComparison.OrdinalIgnoreCase))
                iconFolder = a.Substring("-iconsRootPath=".Length).Trim('"');

            if (a.Equals("-iconsRootPath", System.StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                iconFolder = args[i + 1].Trim('"');

            if (a.StartsWith("-iconFolder=", System.StringComparison.OrdinalIgnoreCase))
                iconFolder = a.Substring("-iconFolder=".Length).Trim('"');

            if (a.Equals("-iconFolder", System.StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                iconFolder = args[i + 1].Trim('"');
        }

        Debug.Log("[IconAutomation] iconFolder ARG = " + iconFolder);

        if (string.IsNullOrEmpty(iconFolder) || !Directory.Exists(iconFolder))
        {
            Debug.LogError("[IconAutomation] INVALID ICON FOLDER");
            EditorApplication.Exit(1);
            return;
        }

        // ----------------------------------------------------------
        // Convert ABSOLUTE → PROJECT RELATIVE
        // ----------------------------------------------------------
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        iconFolder = iconFolder.Replace("\\", "/");

        string relativeRoot = iconFolder.Replace(projectRoot + "/", "");
        Debug.Log("[IconAutomation] Relative Root = " + relativeRoot);

        AssetDatabase.ImportAsset(relativeRoot, ImportAssetOptions.ForceUpdate);

        // ----------------------------------------------------------
        // Platform subfolders
        // ----------------------------------------------------------
        string standaloneFolder = Path.Combine(relativeRoot, "Standalone").Replace("\\", "/");
        string androidFolder = Path.Combine(relativeRoot, "Android").Replace("\\", "/");
        string iosFolder = Path.Combine(relativeRoot, "iOS").Replace("\\", "/");

        Texture2D[] standalone = LoadIconsFromFolder(standaloneFolder);
        Texture2D[] android = LoadIconsFromFolder(androidFolder);
        Texture2D[] ios = LoadIconsFromFolder(iosFolder);

        Debug.Log("[IconAutomation] Loaded icons:");
        Debug.Log($"  Standalone: {standalone.Length}");
        Debug.Log($"  Android: {android.Length}");
        Debug.Log($"  iOS: {ios.Length}");

        // ----------------------------------------------------------
        // NEW: reorder icons to match Unity’s required sizes
        // ----------------------------------------------------------
        standalone = OrderIconsForTargetGroup(BuildTargetGroup.Standalone, standalone);
        android = OrderIconsForTargetGroup(BuildTargetGroup.Android, android);
        ios = OrderIconsForTargetGroup(BuildTargetGroup.iOS, ios);

        // ----------------------------------------------------------
        // Apply icons
        // ----------------------------------------------------------
        try
        {
            if (standalone.Length > 0)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, standalone);

            if (android.Length > 0)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, android);

            if (ios.Length > 0)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, ios);

            Debug.Log("[IconAutomation] Applied icons to PlayerSettings");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[IconAutomation] PlayerSettings saved successfully!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[IconAutomation] ERROR applying icons: " + ex);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("========== ICON AUTOMATION COMPLETE ==========");
        EditorApplication.Exit(0);
    }

    // ----------------------------------------------------------
    // Load PNGs and sort by size
    // ----------------------------------------------------------
    private static Texture2D[] LoadIconsFromFolder(string relativeFolder)
    {
        if (!AssetDatabase.IsValidFolder(relativeFolder))
            return new Texture2D[0];

        string projectRoot = Application.dataPath.Replace("/Assets", "");
        string absFolder = Path.Combine(projectRoot, relativeFolder).Replace("\\", "/");

        if (!Directory.Exists(absFolder))
            return new Texture2D[0];

        List<(Texture2D tex, int size)> list = new();

        foreach (string file in Directory.GetFiles(absFolder, "*.png"))
        {
            string rel = file.Replace("\\", "/").Replace(projectRoot + "/", "");

            AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceUpdate);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);

            if (tex != null)
                list.Add((tex, Mathf.Max(tex.width, tex.height)));
        }

        // Sort by pixel size ASCENDING
        return list.OrderBy(x => x.size).Select(x => x.tex).ToArray();
    }

    // ----------------------------------------------------------
    // NEW: Reorder icon list to match Unity's expected icon sizes
    // ----------------------------------------------------------
    private static Texture2D[] OrderIconsForTargetGroup(BuildTargetGroup group, Texture2D[] textures)
    {
        int[] required = PlayerSettings.GetIconSizesForTargetGroup(group);

        Debug.Log($"[IconAutomation] {group} requires: {string.Join(", ", required)}");

        Texture2D[] ordered = new Texture2D[required.Length];

        foreach (Texture2D tex in textures)
        {
            int size = Mathf.Max(tex.width, tex.height);

            // Find matching index
            int idx = System.Array.IndexOf(required, size);

            if (idx >= 0)
            {
                ordered[idx] = tex;
                Debug.Log($"[IconAutomation]  Mapped {size}px → slot {idx}");
            }
            else
            {
                Debug.LogWarning($"[IconAutomation]  No slot in PlayerSettings matches size {size}px");
            }
        }

        return ordered;
    }
}
