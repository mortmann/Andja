﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantPathHolder : MonoBehaviour {
    public static string ApplicationDataPath;

    public static string StreamingAssets { get; internal set; }

    // Use this for initialization
    void Awake() {
        ApplicationDataPath = Application.dataPath;
        StreamingAssets = Application.streamingAssetsPath;

        if (Application.isEditor)
            ClearConsole();
    }

    static void ClearConsole() {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

        clearMethod.Invoke(null, null);
    }
}