using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class MinePrototypeData : OutputPrototypData {
}
public enum RessourceMode { None, PerProduce, PerMine }
[JsonObject(MemberSerialization.OptIn)]
public class MineStructure : OutputStructure {
    #region Serialize
    #endregion
    #region RuntimeOrOther

    public string Ressource { get {
            if (OutputData.output[0] == null) return null;
            return OutputData.output[0].ID; } }

    public override float EfficiencyPercent {
        get {
            if (BuildTile.Island.HasRessource(Ressource)) {
                return 100;
            }
            return 0;
        }
    }

    protected MinePrototypeData _mineData;
    public MinePrototypeData MineData {
        get {
            if (_mineData == null) {
                _mineData = (MinePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _mineData;
        }
    }
    #endregion
    public static RessourceMode CurrentRessourceMode;
    public MineStructure(string pid, MinePrototypeData MineData) {
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
        if (BuildTile.Island.HasRessource(Ressource) == false) {
            return false;
        }
        return true;
    }

    public override void OnUpdate(float deltaTime) {
        if (Output[0].count >= MaxOutputStorage) {
            return;
        }
        if (CurrentRessourceMode == RessourceMode.PerProduce && BuildTile.Island.HasRessource(Ressource) == false) {
            return;
        }
        produceCountdown += deltaTime;
        if (produceCountdown >= ProduceTime) {
            produceCountdown = 0;
            Output[0].count += OutputData.output[0].count;
            if(CurrentRessourceMode==RessourceMode.PerProduce)
                City.Island.RemoveRessources(Ressource, OutputData.output[0].count);
            cbOutputChange?.Invoke(this);
        }
    }

    public override Structure Clone() {
        return new MineStructure(this);
    }
    public override void OnBuild() {
        if (CurrentRessourceMode == RessourceMode.PerMine) {
            City.Island.RemoveRessources(Ressource, 1);
        }
    }
    protected override void OnDestroy() {
        if (CurrentRessourceMode == RessourceMode.PerMine) {
            City.Island.AddRessources(Ressource, 1);
        }
    }
}
