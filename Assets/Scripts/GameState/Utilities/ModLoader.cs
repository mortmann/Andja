﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System;

public static class ModLoader {
    static readonly string savedActiveModsFile = "mods.ini";
    public static List<string> ActiveMods = new List<string> { };

    static readonly string inStreamingAssetsPath = "Mods";
    static readonly string customMetaDataExtension = ".md";
    static readonly List<string> customSpriteExtension = new List<string> { ".png", ".psd", ".jpg", ".jpeg",
                                                                            ".bmp", ".exr", ".gif", ".hdr",
                                                                            ".iff", ".jpeg", ".pict", ".tga", ".tiff" };

    static readonly List<string> customSoundExtension = new List<string> { ".ogg", ".wav", ".aif", ".it",
                                                                            ".aiff", ".mod", ".s3m", ".xm" };

    static readonly string customXMLExtension = "*.xml";
    static readonly string customIconName = "*_icon";
    static readonly string customIconExtension = ".png";

    static List<Mod> LoadedMods = new List<Mod>();

    public static void LoadMods() {
        List<Mod> avaible = AvaibleMods();        
        foreach (string modName in ActiveMods) {
            if (avaible.Exists(x=>x.name==modName)==false) {
                ActiveMods.Remove(modName);
                Debug.LogWarning("ActiveMod " + modName + " can´t be found and was removed.");
                continue;
            }
            Mod mod = avaible.Find(x => x.name == modName);
            mod.spriteTypeToSprite = new Dictionary<SpriteType, Sprite[]>();
            foreach (SpriteType type in Enum.GetValues(typeof(SpriteType))) {
                mod.spriteTypeToSprite[type] = LoadSpritesForMod(type, modName);
            }
            mod.xmlTypeToXMLString = LoadXMLsForMod(modName);
            mod.iconSprites = LoadIconsForMod(modName);
            mod.soundDatas = LoadSoundsForMod(modName);
            LoadedMods.Add(mod);
        }
    }

    private static List<Sprite> LoadIconsForMod(string mod) {
        List<Sprite> loadedIcons = new List<Sprite>();
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath, mod);
        string[] files = Directory.GetFiles(fullPath, customIconName + customIconExtension, SearchOption.AllDirectories);
        foreach (string file in files) {
            try {
                try {
                    string spriteName = Path.GetFileNameWithoutExtension(file);
                    Texture2D texture = new Texture2D(0, 0) {
                        filterMode = FilterMode.Point,
                        mipMapBias = -0.25f
                    };
                    texture.LoadImage(File.ReadAllBytes(file));
                    Vector2 pivot = new Vector2(texture.width, texture.height) / 2;
                    Sprite icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
                    icon.name = spriteName;
                    loadedIcons.Add(icon);
                }
                catch {
                    Debug.Log("Loading custom icon failed! Reason: Sprite could not be created for " + file + ".");
                    continue;
                }
            }
            catch {
                Debug.Log("Loading custom icon failed! Reason: MetaData not deserializable for " + file + ".");
                continue;
            }
        }
        return loadedIcons;
    }

    public static Sprite[] LoadSpritesForMod(SpriteType type, string Mod) {
        List<Sprite> loadedSprites = new List<Sprite>();
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath, Mod);
        string[] pictures = Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories)
            .Where(file => customSpriteExtension.Contains( Path.GetExtension(file)) )
            .Where(file=>file.Contains("_icon")==false) //no icons
                            .ToArray();
        foreach (string pictureFilePath in pictures) {
            try {
                string metaDataFilePath = Path.Combine(Path.GetDirectoryName(pictureFilePath), Path.GetFileNameWithoutExtension(pictureFilePath) + customMetaDataExtension);
                if (File.Exists(metaDataFilePath) == false) {
                    Debug.Log("Loading custom sprite failed! Reason: MetaData does not exist for " + pictureFilePath + ".");
                    continue;
                }
                CustomSpriteMetaData metaData = JsonConvert.DeserializeObject<CustomSpriteMetaData>(File.ReadAllText(metaDataFilePath), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
                if (metaData.type != type)
                    continue;
                try {
                    string spriteName = Path.GetFileNameWithoutExtension(pictureFilePath);
                    Texture2D texture = new Texture2D(metaData.width, metaData.height, metaData.format, metaData.generateMipMap) {
                        filterMode = FilterMode.Point,
                        mipMapBias = -0.25f
                    };
                    texture.LoadImage(File.ReadAllBytes(pictureFilePath));

                    Vector2 pivot = new Vector2(metaData.xPivot, metaData.yPivot);
                    switch (metaData.spriteMode) {
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
                    Debug.Log("Loading custom sprite failed! Reason: Sprite could not be created for " + pictureFilePath + ".");
                    continue;
                }
            }
            catch {
                Debug.Log("Loading custom sprite failed! Reason: MetaData not deserializable for " + pictureFilePath + ".");
                continue;
            }
        }
        return loadedSprites.ToArray();
    }
    public static Dictionary<string, string> LoadXMLsForMod(string mod) {
        Dictionary<string, string> xmlTypeToXMLString = new Dictionary<string, string>();
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath, mod);
        string[] xmls = Directory.GetFiles(fullPath, customXMLExtension, SearchOption.AllDirectories);
        foreach (string file in xmls) {
            try {
                string name = Path.GetFileNameWithoutExtension(file);
                xmlTypeToXMLString[name] = File.ReadAllText(file);
            }
            catch {
                Debug.Log("Loading custom xml failed! Reason: File could not be read for " + mod + " at " + file + ".");
                continue;
            }
        }
        return xmlTypeToXMLString;
    }

    public static List<SoundMetaData> LoadSoundsForMod(string mod) {
        List<SoundMetaData> loadedSoundMetaDatas = new List<SoundMetaData>();
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath, mod);
        string[] soundfiles = Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories)
            .Where(file => customSoundExtension.Contains(Path.GetExtension(file)))
                            .ToArray();
        foreach (string soundFilePath in soundfiles) {
            try {
                string metaDataFilePath = Path.Combine(Path.GetDirectoryName(soundFilePath), Path.GetFileNameWithoutExtension(soundFilePath) + customMetaDataExtension);
                if (File.Exists(metaDataFilePath) == false) {
                    Debug.Log("Loading custom sound failed! Reason: MetaData does not exist for " + soundFilePath + ".");
                    continue;
                }
                SoundMetaData metaData = JsonConvert.DeserializeObject<SoundMetaData>(File.ReadAllText(metaDataFilePath), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
                try {
                    string soundName = Path.GetFileNameWithoutExtension(soundFilePath);
                    if (metaData.name == null)
                        metaData.name = soundName;
                    metaData.file = soundFilePath;
                    loadedSoundMetaDatas.Add(metaData);
                }
                catch {
                    Debug.Log("Loading custom sound failed! Reason: Sound could not be created for " + soundFilePath + ".");
                    continue;
                }
            }
            catch {
                Debug.Log("Loading custom sound failed! Reason: MetaData not deserializable for " + soundFilePath + ".");
                continue;
            }
        }
        return loadedSoundMetaDatas;
    }
    public static Sprite[] LoadSprites(SpriteType type) {
        List<Sprite> loadedSprites = new List<Sprite>();
        foreach (Mod mod in LoadedMods) {
            loadedSprites.AddRange(mod.spriteTypeToSprite[type]);
        }
        return loadedSprites.ToArray();
    }

    public static void LoadXMLs(string type, Action<string> readFromXML) {
        foreach (Mod mod in LoadedMods) {
            try {
                if (mod.xmlTypeToXMLString.ContainsKey(type) == false)
                    continue;
                readFromXML(mod.xmlTypeToXMLString[type]);
            } catch(Exception e) {
                Debug.Log("Loading XML-Mod failed: Reason XML faulty for mod " + mod.name + " " + type + ". " + e.StackTrace);
            }
        }
    }

    public static Sprite[] LoadIcons() {
        List<Sprite> loadedIcons = new List<Sprite>();
        foreach (Mod mod in LoadedMods) {
            loadedIcons.AddRange(mod.iconSprites);
        }
        return loadedIcons.ToArray();
    }
    public static SoundMetaData[] LoadSoundMetaDatas() {
        List<SoundMetaData> loadedIcons = new List<SoundMetaData>();
        foreach (Mod mod in LoadedMods) {
            loadedIcons.AddRange(mod.soundDatas);
        }
        return loadedIcons.ToArray();
    }
    public static bool IsModActive(string modName) {
        return ActiveMods.Contains(modName);
    }

    public static List<Mod> AvaibleMods() {
        string fullPath = Path.Combine(ConstantPathHolder.StreamingAssets, inStreamingAssetsPath);
        List<Mod> modNames = new List<Mod>();
        foreach (string path in Directory.GetDirectories(fullPath)) {
            if(Directory.GetFileSystemEntries(path, "*",SearchOption.AllDirectories).Length==0) {
                continue;
            }
            string modinfopath = Path.Combine(path, "modinfo");
            if (File.Exists(modinfopath) == false)
                continue;
            Mod mod = JsonConvert.DeserializeObject<Mod>(File.ReadAllText(modinfopath));
            if (mod.gameversion.Trim() != PrototypController.GameVersion)
                continue;
            modNames.Add (mod);
        }
        return modNames;
    }

    internal static void ChangeModStatus(string modname) {
        if (ActiveMods.Contains(modname))
            ActiveMods.Remove(modname);
        else
            ActiveMods.Add(modname);
    }

    public static void LoadSavedActiveMods() {
        string path = Application.dataPath.Replace("/Assets", "");
        string filePath = System.IO.Path.Combine(path, savedActiveModsFile);
        if (File.Exists(filePath) == false)
            return;
        ActiveMods = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));
    }

    public static void SaveActiveMods() {
        string path = Application.dataPath.Replace("/Assets", "");
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        }
        if (ActiveMods == null) {
            return;
        }
        string filePath = System.IO.Path.Combine(path, savedActiveModsFile);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(ActiveMods));
    }

    internal static List<string> GetActiveMods() {
        if (ActiveMods.Count == 0)
            return null;
        return ActiveMods;
    }
}

[JsonObject(MemberSerialization.OptIn)]
public struct Mod {
    [JsonPropertyAttribute]
    public string name;
    [JsonPropertyAttribute]
    public string author;
    [JsonPropertyAttribute]
    public string description;
    [JsonPropertyAttribute]
    public string gameversion;
    [JsonPropertyAttribute]
    public string modversion;

    public Dictionary<SpriteType, Sprite[]> spriteTypeToSprite;
    public Dictionary<string, string> xmlTypeToXMLString;
    public List<Sprite> iconSprites;
    public List<SoundMetaData> soundDatas;
}

public enum SpriteType { Structure, Worker, Unit, Tile }
public enum SpriteMode { Single, Multiple }
public class CustomSpriteMetaData {
    public SpriteType type;
    public SpriteMode spriteMode;
    public int pixelsPerUnit;
    public int height;
    public int width;
    public int xPivot;
    public int yPivot;
    public bool generateMipMap = true;
    public TextureFormat format = TextureFormat.RGBA32;
}