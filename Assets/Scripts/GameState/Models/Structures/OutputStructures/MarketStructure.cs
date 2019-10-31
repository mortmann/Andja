﻿using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class MarketPrototypData : OutputPrototypData {
    public float takeOverStartGoal = 100;
}

[JsonObject(MemberSerialization.OptIn)]
public class MarketStructure : OutputStructure, ICapturable {

    #region Serialize

    [JsonPropertyAttribute] public int level = 1;
    [JsonPropertyAttribute] public float capturedProgress = 0;

    #endregion
    #region RuntimeOrOther

    public List<Structure> RegisteredSturctures;
    public List<Structure> OutputMarkedSturctures;

    public float TakeOverStartGoal { get { return CalculateRealValue("TakeOverStartGoal", MarketData.takeOverStartGoal); } }

    protected MarketPrototypData _marketData;
    public MarketPrototypData MarketData {
        get {
            if (_marketData == null) {
                _marketData = (MarketPrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _marketData;
        }
    }
    #endregion

    public MarketStructure(string id, MarketPrototypData MarketData) {
        this.ID = id;
        _marketData = MarketData;
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public MarketStructure() {
        RegisteredSturctures = new List<Structure>();
        OutputMarkedSturctures = new List<Structure>();
    }
    protected MarketStructure(MarketStructure str) {
        BaseCopyData(str);
    }
    public override Structure Clone() {
        return new MarketStructure(this);
    }

    public override void OnUpdate(float deltaTime) {

        base.Update_Worker(deltaTime);
        if (currentCaptureSpeed > 0) {
            capturedProgress += currentCaptureSpeed * deltaTime;
        }
        else if(capturedProgress>0) {
            capturedProgress -= decreaseCaptureSpeed * deltaTime;
            capturedProgress = Mathf.Clamp01(capturedProgress);
        }
    }
    public override void OnBuild() {
        workersHasToFollowRoads = true; // DUNNO HOW where to set it without the need to copy it extra
        RegisteredSturctures = new List<Structure>();
        OutputMarkedSturctures = new List<Structure>();
        jobsToDo = new Dictionary<OutputStructure, Item[]>();
        // add all the tiles to the city it was build in
        //dostuff thats happen when build
        City.AddTiles(myRangeTiles);
        foreach (Tile rangeTile in myRangeTiles) {
            if (rangeTile.MyCity != City) {
                continue;
            }
            OnStructureAdded(rangeTile.Structure);
        }
        City.RegisterStructureAdded(OnStructureAdded);
    }
    public void OnOutputChangedStructure(Structure str) {
        if (str is OutputStructure == false) {
            return;
        }
        bool hasOutput = false;
        for (int i = 0; i < ((OutputStructure)str).Output.Length; i++) {
            if (((OutputStructure)str).Output[i].count > 0) {
                hasOutput = true;
                break;
            }
        }
        if (hasOutput == false) {
            if (OutputMarkedSturctures.Contains(str)) {
                OutputMarkedSturctures.Remove(str);
            }
            if (jobsToDo.ContainsKey((OutputStructure)str)) {
                jobsToDo.Remove((OutputStructure)str);
            }
            return;
        }


        if (jobsToDo.ContainsKey((OutputStructure)str)) {
            jobsToDo.Remove((OutputStructure)str);
        }

        List<Route> myRoutes = GetMyRoutes();
        //get the roads around the structure
        foreach (Route item in ((OutputStructure)str).GetMyRoutes()) {
            //if one of them is in my roads
            if (myRoutes.Contains(item)) {
                //if we are here we can get there through atleast 1 road
                if (((OutputStructure)str).outputClaimed == false) {
                    jobsToDo.Add((OutputStructure)str, null);
                }
                if (OutputMarkedSturctures.Contains(str)) {
                    OutputMarkedSturctures.Remove(str);
                }
                return;
            }
        }
        //if were here there is noconnection between here and a the structure
        //so remember it for the case it gets connected to it.
        if (OutputMarkedSturctures.Contains(str)) {
            return;
        }
        OutputMarkedSturctures.Add(str);
    }
    protected override void OnDestroy() {
        base.OnDestroy();
        List<Tile> h = new List<Tile>(myStructureTiles);
        h.AddRange(myRangeTiles);
        City.RemoveTiles(h);
    }


    public void OnStructureAdded(Structure structure) {
        if (structure == null) {
            return;
        }
        if (this == structure) {
            return;
        }
        if (structure.City != City) {
            return;
        }

        if (structure is OutputStructure) {
            if (((OutputStructure)structure).ForMarketplace == false) {
                return;
            }
            foreach (Tile item in structure.myStructureTiles) {
                if (myRangeTiles.Contains(item)) {
                    ((OutputStructure)structure).RegisterOutputChanged(OnOutputChangedStructure);
                    break;
                }
            }
        }
        //IF THIS is a pathfinding structure check for new road
        //if true added that to the myroads

        if (structure.MyStructureTyp == StructureTyp.Pathfinding) {
            List<Route> myRoutes = GetMyRoutes();
            if (myRoutes == null || myRoutes.Count == 0)
                return;
            if (neighbourTiles.Contains(structure.myStructureTiles[0])) {
                if (myRoutes.Contains(((RoadStructure)structure).Route) == false) {
                    myRoutes.Add(((RoadStructure)structure).Route);
                }
            }
            for (int i = 0; i < OutputMarkedSturctures.Count; i++) {
                foreach (Route item in ((OutputStructure)OutputMarkedSturctures[i]).GetMyRoutes()) {
                    if (myRoutes.Contains(item)) {
                        OnOutputChangedStructure(OutputMarkedSturctures[i]);
                        break;//breaks only the innerloop eg the routes loop
                    }
                }
            }

        }
    }

    public override Item[] GetRequieredItems(OutputStructure str, Item[] items) {
        if (items == null) {
            items = str.Output;
        }
        List<Item> all = new List<Item>();
        for (int i = items.Length - 1; i >= 0; i--) {
            int space = City.inventory.GetSpaceFor(items[i]);
            if (space == 0) {

            }
            else {
                Item item = items[i].Clone();
                item.count = space;//Mathf.Clamp (items [i].count, 0, space);
                all.Add(item);
            }
        }
        return all.ToArray();
    }

    public override Item[] GetOutputWithItemCountAsMax(Item[] getItems) {
        Item[] temp = new Item[getItems.Length];
        for (int i = 0; i < getItems.Length; i++) {
            //if(City.inventory.GetAmountForItem (getItems[i]) == 0){
            //	continue;
            //}	
            temp[i] = City.inventory.GetItemWithMaxAmount(getItems[i], getItems[i].count);
        }
        return temp;
    }


    public override Item[] GetOutput(Item[] getItems, int[] maxAmounts) {
        Item[] temp = new Item[getItems.Length];
        for (int i = 0; i < getItems.Length; i++) {
            //if(City.inventory.GetAmountForItem (getItems[i]) == 0){
            //	continue;
            //}	
            if (getItems[i] == null || maxAmounts == null) {
                Debug.Log("s");
            }
            temp[i] = City.inventory.GetItemWithMaxAmount(getItems[i], maxAmounts[i]);
        }
        return temp;
    }

    #region ICapturableImplementation
    float currentCaptureSpeed = 0f;
    //TODO: load this all in
    float decreaseCaptureSpeed = 0.01f;
    float maximumCaptureSpeed = 0.05f;
    public void Capture(IWarfare warfare, float progress) {
        if (Captured) {
            DoneCapturing(warfare);
            return;
        }
        currentCaptureSpeed = Mathf.Clamp(currentCaptureSpeed + progress, 0, maximumCaptureSpeed);
    }

    private void DoneCapturing(IWarfare warfare) {
        //either capture it or destroy based on if is a city of that player on that island
        City c = BuildTile.MyIsland.myCities.Find(x => x.playerNumber == warfare.PlayerNumber);
        if (c != null) {
            OnDestroy();
            City = c;
            OnBuild();
        }
        else {
            Destroy();
        }
    }

    public bool Captured => capturedProgress == 1;
    #endregion
}