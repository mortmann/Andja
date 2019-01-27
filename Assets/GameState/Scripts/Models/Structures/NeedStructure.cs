using UnityEngine;
using System.Collections;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class NeedStructure : TargetStructure {
    #region Serialize
    #endregion
    #region RuntimeOrOther
    #endregion
    public NeedStructure(int pid, StructurePrototypeData spd) {
        this.ID = pid;
        this._prototypData = spd;
    }
    public NeedStructure(int pid) {
        this.ID = pid;
    }
    public NeedStructure(NeedStructure b) {
        BaseCopyData(b);
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public NeedStructure() { }


    public override Structure Clone() {
        return new NeedStructure(this);
    }
    public override void OnBuild() {
        foreach (Tile t in myRangeTiles) {
            t.AddNeedStructure(this);
        }
    }
    public override void Update(float deltaTime) {
    }

}
