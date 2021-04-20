using Andja.Model;
using Andja.Model.Generator;
using Andja.Utility;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Andja {

    public class Test : MonoBehaviour {
        private Image image;

        // Start is called before the first frame update
        public int Width = 100;

        public int Height = 100;
        public bool saveToDisk;
        public Texture2D texture;
        private ThreadRandom Random;

        private void Start() {
            Random = new ThreadRandom(MapGenerator.EditorSeed);
            image = GetComponent<Image>();
            for (int iss = 0; iss < 1; iss++) {
                float progress = 0;
                float[,] values = HeightGenerator.Generate(Width, Height, Random, 1, ref progress);
                texture = new Texture2D(Width, Height);
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        float val = values[x, y];
                        if (val > IslandGenerator.mountainElevation)
                            texture.SetPixel(x, y, new Color(val, 0, 0, 1));
                        else
                        if (val > IslandGenerator.landThreshold)
                            texture.SetPixel(x, y, new Color(val, val, val, 1));
                        else
                        if (val >= IslandGenerator.shoreElevation)
                            texture.SetPixel(x, y, Color.yellow);
                        else
                            texture.SetPixel(x, y, Color.black);
                        //texture.SetPixel(x, y, new Color(1 * val, 1 * val, 1 * val, 0));
                    }
                }
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                if (saveToDisk) {
                    byte[] bytes = texture.EncodeToPNG();
                    var dirPath = "F:/SaveImages/Shore+/";
                    if (!Directory.Exists(dirPath)) {
                        Directory.CreateDirectory(dirPath);
                    }
                    File.WriteAllBytes(dirPath + "Image " + iss + "_9.png", bytes);
                }
            }
            image.preserveAspect = true;
            image.sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100);
        }

        // Update is called once per frame
        private void Update() {
            if (image.mainTexture != null && image.mainTexture.width == Width && image.mainTexture.height == Height)
                return;
        }
    }

    internal struct RandomPoint {
        public float Width;
        public float Height;
        public float x;
        public float y;
    }
}