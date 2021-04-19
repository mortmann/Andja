using Andja.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_ModListItem : MonoBehaviour {
        private string modname;
        public Text ModNameLabel;
        public Text CreatorNameLabel;

        // Use this for initialization
        public void SetMod(Mod mod) {
            modname = mod.name;
            ModNameLabel.text = modname + "(" + mod.modversion + ")";
            CreatorNameLabel.text = mod.author;
            GetComponentInChildren<Toggle>().isOn = ModLoader.IsModActive(modname);
            GetComponentInChildren<Toggle>().onValueChanged.AddListener(OnToggleChange);
        }

        private void OnToggleChange(bool value) {
            ModLoader.ChangeModStatus(modname);
        }
    }
}