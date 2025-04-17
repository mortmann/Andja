using System;
using System.Linq;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public enum InputTyp { AND, OR }

    public class IntakePrototypeData : ElementData {
        public Item[] input;
        public InputTyp inputTyp;
        public int inputSize = 1;
        public override Element GetNewElement(BaseThing thing) {
            return new Intake(thing);
        }
    }
    
    public class Intake : Element {
        private Item[] _input;
        public InputTyp InputTyp => Data.inputTyp;

        private IntakePrototypeData _data;
        private IntakePrototypeData Data => _data ??= Parent.GetElementData<IntakePrototypeData>();
        [JsonProperty]
        public virtual Item[] Items {
            get => _input ??= Data.input.CloneArray();
            set => _input = value;
        }
        private int _orItemIndex = int.MinValue; //TODO think about to switch to short if it needs to save space

        public int OrItemIndex {
            get {
                if (_orItemIndex == int.MinValue) {
                    _orItemIndex = Array.FindIndex(Data.input, x => x.ID == Items[0].ID);
                }
                return _orItemIndex;
            }
        }
        
        public Intake(BaseThing baseThing) : base(baseThing) {
        }

        public override void OnStart(bool loading = false) {
        }

        public override void OnDestroy() {
        }

        public override void OnUpdate(float deltaTime) {
        }
        
        public bool Missing() {
            if (Data.input == null) {
                return true;
            }
            return InputTyp switch {
                InputTyp.AND => Items.Where((t, i) => Data.input[i].count > t.count).Any() == false,
                InputTyp.OR when Data.input[OrItemIndex].count > Items[0].count => false,
                _ => true
            };
        }
        public override void OnLoad() {
            Items = Items.ReplaceKeepCounts(Data.input);
        }

        public bool IsFull() {
            return Data.input.Where((t, i) => Items[i].count == t.count * Data.inputSize).Any() == false;
        }

        public void Decrease() {
            for (int i = 0; i < Items.Length; i++) {
                Items[i].count -= Mathf.Clamp(Data.input[i].count,1, int.MaxValue);
            }
        }

    }
}