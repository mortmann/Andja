using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Andja.Model {

    public class NeedStructurePrototypeData : TargetStructurePrototypeData {
        [Ignore] public List<Need> SatisfiesNeeds = new List<Need>();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NeedStructure : TargetStructure {
        private NeedStructurePrototypeData _needStructureData;
        public NeedStructurePrototypeData NeedStructureData {
            get {
                if (_needStructureData == null) {
                    _needStructureData = (NeedStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _needStructureData;
            }
        }
        public NeedStructure(string pid, NeedStructurePrototypeData nspd) {
            this.ID = pid;
            this._needStructureData = nspd;
        }

        public NeedStructure(string pid) {
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
            foreach (Tile t in RangeTiles) {
                t.AddNeedStructure(this);
            }
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
        }
        public override void OnUpdate(float deltaTime) {
        }
    }
}