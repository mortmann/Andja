using Andja.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class ProducerPrototypeData : ElementData {
        public float produceTime;

        public override Element GetNewElement(BaseThing thing) {
            return new Producer(thing as Structure);
        }
    }
    
    public class Producer : Element  {
        private ProducerPrototypeData _data;
        private ProducerPrototypeData Data => _data ??= Parent.GetElementData<ProducerPrototypeData>();
        public float ProduceTime => Parent.CalculateRealValue(nameof(Data.produceTime), Data.produceTime);
        [JsonProperty] public float ProduceTimer { get; protected set; }

        private Output _output;
        private Input _input;
        
        public Producer(Structure structure) : base(structure) {
        }

        public override void OnStart(bool loading = false) {
            throw new System.NotImplementedException();
        }

        public override void OnDestroy() {
            throw new System.NotImplementedException();
        }

        public override void OnUpdate(float deltaTime) {
            if (_output.IsFull()) {
                return;
            }
            ProduceTimer += deltaTime;
            if ((ProduceTimer >= ProduceTime) == false) {
                return;
            }
            ProduceTimer = 0;
            if (_input?.Missing() == true) {
                return;
            }
            _input?.Decrease();
            _output.Increase();
        }

        public override void OnLoad() {
            throw new System.NotImplementedException();
        }
    }
}