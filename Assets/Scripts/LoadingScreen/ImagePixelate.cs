using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Andja.Controller;

namespace Andja.LoadScreen {
    public class ImagePixelate : MonoBehaviour {
        Image i;
        Loading loading;
        void Start() {
            loading = FindObjectOfType<Loading>();
            i = GetComponent<Image>();
            var list = new List<Sprite>(StructureSpriteController.structureSprites.Values);
            i.sprite = list[Random.Range(0, list.Count)];
        }

        void Update() {
            float percentage = 0.0001f + Mathf.Clamp01(1f - ((loading.percantage * loading.percantage) / (100f * 100f)));
            i.material.SetFloat("_CellSize", percentage);
        }
    }

}
