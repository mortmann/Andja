using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentlyBuildingUnitUI : MonoBehaviour {
    public GameObject nextBuild;
    public GameObject unitQueue;

    public CircleProgressBar progressBar;
    public UnitBuildUI currently;
    MilitaryBuilding uiBuilding;
    // Use this for initialization
    public void Show (MilitaryBuilding mb) {
        currently.Show(mb.CurrentlyBuildingUnit);
        uiBuilding = mb;
        progressBar.SetProgress(uiBuilding.ProgressPercentage);
    }

    // Update is called once per frame
    void Update () {
        if(uiBuilding.CurrentlyBuildingUnit != null) {
            currently.Show(uiBuilding.CurrentlyBuildingUnit);
        }
        progressBar.SetProgress(uiBuilding.ProgressPercentage);
    }
}
