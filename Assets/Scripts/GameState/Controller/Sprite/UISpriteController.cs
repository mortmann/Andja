using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISpriteController : MonoBehaviour {
    static Dictionary<string, Sprite> idToUI;
    static Dictionary<string, Sprite> idToIcon;
    static Dictionary<string, Sprite> idToItemIcons;
    public static string iconNameAdd = "_icon";
    public static string uiNameAdd = "_ui";

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

    public static bool HasUISprite(string id) {
        return idToUI.ContainsKey(id + uiNameAdd);
    }

    public static Sprite GetUISprite(string id) {
        id += uiNameAdd;
        if (idToUI.ContainsKey(id)) {
            return idToUI[id];
        }
        Debug.LogWarning("Missing Icon " + id);
        return null;
    }
    public static Sprite GetItemImageForID(string id) {
        if (idToItemIcons.ContainsKey(id) == false) {
            Debug.LogWarning("Item " + id + " is missing image!");
            return null;
        }
        return idToItemIcons[id];
    }

    static void LoadSprites() {
        idToUI = new Dictionary<string, Sprite>();
        idToIcon = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Icons/");
        foreach (Sprite s in sprites) {
            idToIcon[s.name] = s;
        }
        Sprite[] custom = ModLoader.LoadSprites(SpriteType.Icon);
        if(custom != null) {
            foreach (Sprite s in custom) {
                idToIcon[s.name] = s;
            }
        }
        sprites = Resources.LoadAll<Sprite>("Textures/UI/");
        foreach (Sprite s in sprites) {
            idToUI[s.name + "_ui"] = s;
        }
        custom = ModLoader.LoadSprites(SpriteType.UI);
        if (custom != null) {
            foreach (Sprite s in custom) {
                idToUI[s.name] = s;
            }
        }
        sprites = Resources.LoadAll<Sprite>("Textures/Items/");
        //Debug.Log(sprites.Length + " Item Sprite");
        idToItemIcons = new Dictionary<string, Sprite>();
        foreach (Sprite item in sprites) {
            idToItemIcons[item.name] = item;
        }
        custom = ModLoader.LoadSprites(SpriteType.Item);
        if(custom != null) {
            foreach (Sprite item in custom) {
                idToItemIcons[item.name] = item;
            }
        }
    }


}
