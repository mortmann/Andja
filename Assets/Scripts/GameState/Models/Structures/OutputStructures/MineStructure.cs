using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Andja.Model {

    public class MinePrototypeData : OutputPrototypData {
    }

    public enum ResourceMode { None, PerProduce, PerMine }

    [JsonObject(MemberSerialization.OptIn)]
    public class MineStructure : OutputStructure {

        #region RuntimeOrOther

        public string Resource {
            get {
                if (OutputData.output[0] == null) return null;
                return OutputData.output[0].ID;
            }
        }

        public override float EfficiencyPercent {
            get {
                if (BuildTile.Island.HasResource(Resource)) {
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

        #endregion RuntimeOrOther

        public static ResourceMode CurrentResourceMode;

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
            if (BuildTile.Island.HasResource(Resource) == false) {
                return false;
            }
            return true;
        }

        public override void OnUpdate(float deltaTime) {
            if (IsActiveAndWorking == false || Output[0].count >= MaxOutputStorage) {
                return;
            }
            if (CurrentResourceMode == ResourceMode.PerProduce && BuildTile.Island.HasResource(Resource) == false) {
                return;
            }
            ProduceTimer += deltaTime;
            if (ProduceTimer >= ProduceTime) {
                ProduceTimer = 0;
                Output[0].count += OutputData.output[0].count;
                if (CurrentResourceMode == ResourceMode.PerProduce)
                    City.Island.RemoveResources(Resource, OutputData.output[0].count);
                cbOutputChange?.Invoke(this);
            }
        }

        public override Structure Clone() {
            return new MineStructure(this);
        }

        public override void OnBuild() {
            if (CurrentResourceMode == ResourceMode.PerMine) {
                City.Island.RemoveResources(Resource, 1);
            }
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _mineData = null;
        }
        protected override void OnDestroy() {
            if (CurrentResourceMode == ResourceMode.PerMine) {
                City.Island.AddResources(Resource, 1);
            }
        }
    }
}