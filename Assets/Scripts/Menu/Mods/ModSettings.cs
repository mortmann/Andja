using Andja.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.UI.Menu {

    public class ModSettings : MonoBehaviour {
        public Transform content;
        public GS_ModListItem listItemPrefab;

        // Use this for initialization
        private void Start() {
            ModLoader.LoadSavedActiveMods();
            List<Mod> avaibleMods = ModLoader.AvaibleMods();
            foreach (Transform t in content)
                Destroy(t.gameObject);
            foreach (Mod mod in avaibleMods) {
                GS_ModListItem item = Instantiate(listItemPrefab);
                item.SetMod(mod);
                item.transform.SetParent(content);
            }
        }

        private void OnDisable() {
            ModLoader.SaveActiveMods();
        }
    }
}