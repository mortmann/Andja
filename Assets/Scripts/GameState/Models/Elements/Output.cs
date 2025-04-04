using System;
using System.Linq;
using Andja.Utility;
using Newtonsoft.Json;

namespace Andja.Model {
    public class OutputPrototypeData : ElementData {
        public Item[] output;
        public int maxOutputStorage;
        public override Element GetNewElement(BaseThing thing) {
            return new Output(thing);
        }
    }
    public class Output : Element {
        protected Action<Structure> cbOutputChange;

        private Item[] _output;
        public int MaxOutputStorage => Parent.CalculateRealValue(nameof(Data.maxOutputStorage), Data.maxOutputStorage); 

        private OutputPrototypeData _data;
        private OutputPrototypeData Data => _data ??= Parent.GetElementData<OutputPrototypeData>();
        [JsonProperty]
        public virtual Item[] Items {
            get => _output ??= Data.output.CloneArray();
            set => _output = value;
        }

        
        public Output(BaseThing baseThing) : base(baseThing) {
        }

        public override void OnStart(bool loading = false) {
        }

        public override void OnDestroy() {
        }

        public override void OnUpdate(float deltaTime) {
        }

        public override void OnLoad() {
            Items = Items.ReplaceKeepCounts(Data.output);
        }

        public bool IsFull() {
            return _output.Any(item => item.count == MaxOutputStorage);
        }

        public void Increase() {
            for (int i = 0; i < Items.Length; i++) {
                Items[i].count += Data.output[i].count;
            }
            cbOutputChange?.Invoke((Structure) Parent);
        }
    }
}