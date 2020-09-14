using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconSpriteController : MonoBehaviour {

    static Dictionary<string, Sprite> idToIcon;
    static string iconNameAdd = "_icon";
    void Awake() {
        LoadSprites();
    }
    public static bool HasIcon(string id) {
        return idToIcon.ContainsKey(id+iconNameAdd);
    }
    public static Sprite GetIcon(string id) {
        id += iconNameAdd;
        if (idToIcon.ContainsKey(id)) {
            return idToIcon[id];
        }
        Debug.LogWarning("Missing Icon " + id);
        return null;
    }
    static void LoadSprites() {
        idToIcon = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Icons/");
        foreach (Sprite s in sprites) {
            idToIcon[s.name] = s;
        }
        Sprite[] custom = ModLoader.LoadIcons();
        if (custom == null)
            return;
        foreach (Sprite s in custom) {
            idToIcon[s.name] = s;
        }
    }


}
