using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasScale : MonoBehaviour {
    static Vector2 referenceResolution;
    public static float Width => Screen.width / referenceResolution.x;
    public static float Height => Screen.height / referenceResolution.y;
    public static Vector2 Vector => new Vector2(Width, Height);

    void Start(){
        referenceResolution = FindObjectOfType<CanvasScaler>().referenceResolution;
    }

}
