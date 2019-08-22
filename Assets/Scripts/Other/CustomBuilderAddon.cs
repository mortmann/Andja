using UnityEditor;
using System.IO;
using UnityEditor.Build;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class CustomBuilderAddon : IPostprocessBuildWithReport {
    public int callbackOrder { get { return 0; } }
    public void OnPostprocessBuild(BuildTarget target, string path) {
        foreach(string filepath in Directory.GetFiles(SaveController.GetIslandSavePath()))
            File.Copy(
                filepath, 
                Path.Combine(path, filepath.Substring(filepath.IndexOf("Islands")))
           );
    }

    public void OnPostprocessBuild(BuildReport report) {

    }
}

