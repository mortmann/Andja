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
        [JsonProperty] public float Timer { get; protected set; }

        private Output _output;
        private Intake _intake;
        
        public Producer(Structure structure) : base(structure) {
        }

        public override void OnStart(bool loading = false) {
            
        }

        public override void OnDestroy() {
            
        }

        public override void OnUpdate(float deltaTime) {
            if (_output.IsFull()) {
                return;
            }
            Timer += deltaTime;
            if (ProduceTime > Timer) {
                return;
            }
            Timer = 0;
            if (_intake?.Missing() == true) {
                return;
            }
            _intake?.Decrease();
            _output.Increase();
        }

        public override void OnLoad() {
            Timer = Mathf.Min(Timer, Data.produceTime);
        }
    }
}