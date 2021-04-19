using Andja.Model;
using UnityEngine;

namespace Andja.UI.Model {

    public class CurrentlyBuildingUnitUI : MonoBehaviour {
        public GameObject nextBuild;
        public GameObject unitQueue;

        public CircleProgressBar progressBar;
        public UnitBuildUI currently;
        private MilitaryStructure uiStructure;

        // Use this for initialization
        public void Show(MilitaryStructure mb) {
            currently.Show(mb.CurrentlyBuildingUnit);
            uiStructure = mb;
            progressBar.SetProgress(uiStructure.ProgressPercentage);
        }

        // Update is called once per frame
        private void Update() {
            if (uiStructure.CurrentlyBuildingUnit != null) {
                currently.Show(uiStructure.CurrentlyBuildingUnit);
            }
            progressBar.SetProgress(uiStructure.ProgressPercentage);
        }
    }
}