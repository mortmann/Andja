using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MilitaryBuildingPrototypeData : StructurePrototypeData {
    public Unit[] canBeBuildUnits;
    public float buildTimeModifier;
    public int buildQueueLength = 1;
}

[JsonObject(MemberSerialization.OptIn)]
public class MilitaryBuilding : Structure {
    [JsonPropertyAttribute] float buildTimer;
    [JsonPropertyAttribute] Queue<Unit> toBuildUnits;

    List<Tile> toPlaceUnitTiles;
    public float ProgressPercentage => CurrentlyBuildingUnit!=null ? buildTimer / CurrentlyBuildingUnit.BuildTime : 0;
    public Unit[] CanBeBuildUnits => MilitaryBuildingData.canBeBuildUnits;
    public float BuildTimeModifier => MilitaryBuildingData.buildTimeModifier;
    public int BuildQueueLength => MilitaryBuildingData.buildQueueLength;
    public Unit CurrentlyBuildingUnit => toBuildUnits.Count>0 ? toBuildUnits.Peek() : null;

    protected MilitaryBuildingPrototypeData _militaryBuildingData;
    public MilitaryBuildingPrototypeData MilitaryBuildingData {
        get {
            if (_militaryBuildingData == null) {
                _militaryBuildingData = (MilitaryBuildingPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _militaryBuildingData;
        }
    }
    public MilitaryBuilding() {
    }
    public MilitaryBuilding(MilitaryBuilding mb) {
        BaseCopyData(mb);
    }

    internal bool HasEnoughResources(Unit u) {
        if (PlayerController.Instance.HasEnoughMoney(PlayerNumber, u.BuildCost) == false) {
            return false;
        }
        return City.HasEnoughOfItems(u.BuildingItems);
    }

    public MilitaryBuilding(int iD, MilitaryBuildingPrototypeData mpd) {
        ID = iD;
        this._militaryBuildingData = mpd;
    }

    public override Structure Clone() {
        return new MilitaryBuilding(this);
    }

    public override void OnBuild() {
        toBuildUnits = new Queue<Unit>();
        toPlaceUnitTiles = new List<Tile>();
        foreach (Tile t in neighbourTiles) {
            t.RegisterTileStructureChangedCallback(OnNeighbourTileStructureChange);
            if (t.Structure != null && t.Structure.IsWalkable == false) {
                return;
            }
            if(MustBeBuildOnShore && t.Type != TileType.Ocean) {
                continue;
            }
            toPlaceUnitTiles.Add(t);
        }
    }
    public void OnNeighbourTileStructureChange(Tile tile,Structure str) {
        if (str != null && str.IsWalkable == false) {
            if (toPlaceUnitTiles.Contains(tile)) {
                toPlaceUnitTiles.Remove(tile);
            }
        }
        toPlaceUnitTiles.Add(tile);
    }
    public override void Update(float deltaTime) {
        if(CurrentlyBuildingUnit == null) {
            return;
        }
        buildTimer += deltaTime * BuildTimeModifier;
        if(buildTimer > CurrentlyBuildingUnit.BuildTime) {
            //Spawn Unit here and reset the timer!
            buildTimer = 0;
            SpawnUnit(toBuildUnits.Dequeue());
        }
    }
    public bool AddUnitToBuildQueue(Unit u) {
        //cant build more -> if we make a buildqueue!
        if (toBuildUnits.Count >= BuildQueueLength) {
            return false;
        }
        //we need to know if we have all the resources!
        if (City.HasEnoughOfItems(u.BuildingItems) == false) {
            return false;
        }
        City.RemoveRessources(u.BuildingItems);
        PlayerController.Instance.ReduceMoney(u.BuildCost,PlayerNumber);
        toBuildUnits.Enqueue(u);
        return true;
    }
    private void SpawnUnit(Unit unit) {
        if (toPlaceUnitTiles.Count == 0)
            return;
        World.Current.CreateUnit(unit.Clone(PlayerNumber, toPlaceUnitTiles[0]));
    }
}
