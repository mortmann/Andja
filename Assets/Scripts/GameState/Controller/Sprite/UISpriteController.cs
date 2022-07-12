using Andja.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public enum CommonIcon { Income, Money, Upkeep, People, CurrentDamage, MaximumDamage, Speed }

    public class UISpriteController : MonoBehaviour {
        private static Dictionary<string, Sprite> _idToUI;
        private static Dictionary<string, Sprite> _idToIcon;
        private static Dictionary<string, Sprite> _idToItemIcons;
        public static string iconNameAdd = "_icon";
        public static string uiNameAdd = "_ui";

        public void Awake() {
            LoadSprites();
            MouseController.ChangeCursorType(CursorType.Pointer);
        }
        public void OnDisable() {
            _idToUI.Clear();
            _idToIcon.Clear();
            _idToItemIcons.Clear();
        }
        public static bool HasIcon(string id) {
            return _idToIcon.ContainsKey(id + iconNameAdd);
        }

        public static Sprite GetIcon(CommonIcon icon) {
            return GetIcon(icon.ToString());
        }

        public static Sprite GetIcon(string id) {
            id += iconNameAdd;
            if (_idToIcon.ContainsKey(id)) {
                return _idToIcon[id];
            }
            Debug.LogWarning("Missing Icon " + id);
            return null;
        }

        internal static Sprite GetIcon(object spriteName) {
            throw new NotImplementedException();
        }

        public static bool HasUISprite(string id) {
            return _idToUI.ContainsKey(id + uiNameAdd);
        }

        public static Sprite GetUISprite(string id) {
            id += uiNameAdd;
            if (_idToUI.ContainsKey(id)) {
                return _idToUI[id];
            }
            Debug.LogWarning("Missing Icon " + id);
            return null;
        }

        public static Sprite GetItemImageForID(string id) {
            if (_idToItemIcons.ContainsKey(id) == false) {
                Debug.LogWarning("Item " + id + " is missing image!");
                return null;
            }
            return _idToItemIcons[id];
        }

        private static void LoadSprites() {
            _idToUI = new Dictionary<string, Sprite>();
            _idToIcon = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Icons/");
            foreach (Sprite s in sprites) {
                _idToIcon[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Icon);
            if (custom != null) {
                foreach (Sprite s in custom) {
                    _idToIcon[s.name] = s;
                }
            }
            sprites = Resources.LoadAll<Sprite>("Textures/UI/");
            foreach (Sprite s in sprites) {
                _idToUI[s.name + "_ui"] = s;
            }
            custom = ModLoader.LoadSprites(SpriteType.UI);
            if (custom != null) {
                foreach (Sprite s in custom) {
                    _idToUI[s.name] = s;
                }
            }
            sprites = Resources.LoadAll<Sprite>("Textures/Items/");
            //Debug.Log(sprites.Length + " Item Sprite");
            _idToItemIcons = new Dictionary<string, Sprite>();
            foreach (Sprite item in sprites) {
                _idToItemIcons[item.name] = item;
            }
            custom = ModLoader.LoadSprites(SpriteType.Item);
            if (custom == null) return;
            foreach (Sprite item in custom) {
                _idToItemIcons[item.name] = item;
            }
        }
    }
}