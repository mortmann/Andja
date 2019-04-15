﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MinePrototypData : OutputPrototypData {
}

[JsonObject(MemberSerialization.OptIn)]
public class MineStructure : OutputStructure {
    #region Serialize
    #endregion
    #region RuntimeOrOther

    public int Ressource { get {
            if (OutputData.output[0] == null) return -1;
            return OutputData.output[0].ID; } }

    public override float EfficiencyPercent {
        get {
            if (BuildTile.MyIsland.HasRessource(Ressource)) {
                return 100;
            }
            return 0;
        }
    }

    protected MinePrototypData _mineData;
    public MinePrototypData MineData {
        get {
            if (_mineData == null) {
                _mineData = (MinePrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _mineData;
        }
    }
    #endregion

    public MineStructure(int pid, MinePrototypData MineData) {
        this.ID = pid;
        _mineData = MineData;
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public MineStructure() {
    }
    protected MineStructure(MineStructure ms) {
        OutputCopyData(ms);
    }

    public override bool SpecialCheckForBuild(List<Tile> tiles) {
        for (int i = 0; i < tiles.Count; i++) {
            if (tiles[i].Type == TileType.Mountain) {
                if (BuildTile.MyIsland.HasRessource(Ressource) == false) {
                    return false;
                }
            }
        }
        return true;
    }

    public override void OnUpdate(float deltaTime) {
        if (BuildTile.MyIsland.HasRessource(Ressource) == false) {
            return;
        }
        if (Output[0].count >= MaxOutputStorage) {
            return;
        }
        produceCountdown += deltaTime;
        if (produceCountdown >= ProduceTime) {
            produceCountdown = 0;
            Output[0].count += OutputData.output[0].count;
            City.island.RemoveRessources(Ressource, OutputData.output[0].count);
            cbOutputChange?.Invoke(this);
        }
    }

    public override Structure Clone() {
        return new MineStructure(this);
    }
    public override void OnBuild() {

    }

}
