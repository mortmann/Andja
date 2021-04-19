using Newtonsoft.Json;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class NeedStructure : TargetStructure {

        public NeedStructure(string pid, StructurePrototypeData spd) {
            this.ID = pid;
            this._prototypData = spd;
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

        public override void OnUpdate(float deltaTime) {
        }
    }
}