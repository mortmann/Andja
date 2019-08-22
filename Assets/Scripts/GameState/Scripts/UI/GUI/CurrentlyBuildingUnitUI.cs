using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentlyBuildingUnitUI : MonoBehaviour {
    public GameObject nextBuild;
    public GameObject unitQueue;

    public CircleProgressBar progressBar;
    public UnitBuildUI currently;
    MilitaryStructure uiStructure;
    // Use this for initialization
    public void Show(MilitaryStructure mb) {
        currently.Show(mb.CurrentlyBuildingUnit);
        uiStructure = mb;
        progressBar.SetProgress(uiStructure.ProgressPercentage);
    }

    // Update is called once per frame
    void Update() {
        if (uiStructure.CurrentlyBuildingUnit != null) {
            currently.Show(uiStructure.CurrentlyBuildingUnit);
        }
        progressBar.SetProgress(uiStructure.ProgressPercentage);
    }
}
