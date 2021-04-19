using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_Panel : MonoBehaviour {
        public Selectable firstSelected;

        /**
         * Select the specified element so that we can navigate through the panel
         * using a keyboard or gamepad.
         */

        public void SelectFirstElement() {
            firstSelected.Select();
        }
    }
}