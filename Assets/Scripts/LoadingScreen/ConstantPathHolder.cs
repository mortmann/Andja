using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantPathHolder : MonoBehaviour {
    public static string ApplicationDataPath;

    public static string StreamingAssets { get; internal set; }

    // Use this for initialization
    void Awake() {
        ApplicationDataPath = Application.dataPath;
        StreamingAssets = Application.streamingAssetsPath;
    }

}
