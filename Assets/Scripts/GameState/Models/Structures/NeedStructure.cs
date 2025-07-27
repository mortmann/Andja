using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class NeedStructurePrototypeData : TargetStructurePrototypeData {
        [Ignore] public List<Need> SatisfiesNeeds = new List<Need>();
        [Ignore] public int MaxHomesInRange {
            get {
                HomeStructure home = PrototypController.Instance.BuildableHomeStructure;
                return Mathf.CeilToInt((float)PrototypeRangeTiles.Count / 
                    (home.TileWidth * home.TileHeight));
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NeedStructure : TargetStructure {
        private NeedStructurePrototypeData _needStructureData;
        public NeedStructurePrototypeData NeedStructureData =>
            _needStructureData ??= (NeedStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(((IBaseThing)this).ID);

        public NeedStructure(string pid, NeedStructurePrototypeData nspd) {
            ((IBaseThing)this).ID = pid;
            this._needStructureData = nspd;
        }

        public NeedStructure(string pid) {
            ((IBaseThing)this).ID = pid;
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

        public override void OnBuild(bool loading = false) {
            foreach (Tile t in RangeTiles) {
                t.AddNeedStructure(this);
            }
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
        }
        protected override void OnUpdate(float deltaTime) {
        }
    }
}