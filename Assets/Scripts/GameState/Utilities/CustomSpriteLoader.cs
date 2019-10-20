using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public static class CustomSpriteLoader {

    static readonly string inStreamingAssetsPath = "CustomSprites";
    static readonly string customSpriteMetaDataExtension = "*.md";
    static readonly string customSpriteExtension = ".png";

    public static Sprite[] Load(string type) {
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath, type);
        string[] metas = Directory.GetFiles(fullPath, customSpriteMetaDataExtension,SearchOption.AllDirectories);
        List<Sprite> loadedSprites = new List<Sprite>();
        foreach (string file in metas) {
            try {
                CustomSpriteMetaData metaData = JsonConvert.DeserializeObject<CustomSpriteMetaData>(File.ReadAllText(file), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
                string customSpritePath = Path.Combine(fullPath, Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)+customSpriteExtension);
                try {
                    if(File.Exists(customSpritePath) == false) {
                        Debug.Log("Loading custom sprite failed! Reason: Sprite does not exist for " + file + ".");
                        continue;
                    }
                    string spriteName = Path.GetFileNameWithoutExtension(customSpritePath);
                    Texture2D texture = new Texture2D(metaData.width, metaData.height, metaData.format, metaData.generateMipMap) {
                        filterMode = FilterMode.Point,
                        mipMapBias = -0.25f
                    };
                    texture.LoadImage(File.ReadAllBytes(customSpritePath));

                    Vector2 pivot = new Vector2(metaData.xPivot, metaData.yPivot);
                    switch (metaData.type) {
                        case SpriteMode.Single:
                            Sprite single = Sprite.Create(texture, new Rect(0, 0, metaData.width, metaData.height), pivot, metaData.pixelsPerUnit);
                            single.name = spriteName;
                            loadedSprites.Add(single);
                            break;
                        case SpriteMode.Multiple:
                            int spriteNumber = 0;
                            for (int y = 0; y < texture.height; y += metaData.height) {
                                for (int x = 0; x < texture.width; x += metaData.width) {
                                    Sprite sprite = Sprite.Create(texture, new Rect(x, y, metaData.width, metaData.height), new Vector2(0.5f, 0.5f), metaData.pixelsPerUnit);
                                    sprite.name = spriteName + "_" + spriteNumber;
                                    loadedSprites.Add(sprite);
                                    spriteNumber++;
                                }
                            }
                            break;
                    }
                }
                catch {
                    Debug.Log("Loading custom sprite failed! Reason: Sprite could not be created for " + file + ".");
                    continue;
                }
            }
            catch {
                Debug.Log("Loading custom sprite failed! Reason: MetaData not deserializable for " + file + "." );
                continue;
            }
        }
        return loadedSprites.ToArray();
    }

}
public enum SpriteMode { Single, Multiple }
public class CustomSpriteMetaData {
    public SpriteMode type;
    public int pixelsPerUnit;
    public int height;
    public int width;
    public int xPivot;
    public int yPivot;
    public bool generateMipMap = true;
    public TextureFormat format = TextureFormat.RGBA32;
}