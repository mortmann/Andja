using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public enum InputTyp { AND, OR }

    public class ProductionPrototypeData : OutputPrototypData {
        public Item[] intake;
        public InputTyp inputTyp;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ProductionStructure : OutputStructure {

        #region Serialize

        private Item[] _intake;

        [JsonPropertyAttribute]
        public Item[] Intake {
            get {
                if (_intake == null) {
                    if (ProductionData.intake == null) {
                        return null;
                    }
                    if (ProductionData.inputTyp == InputTyp.AND) {
                        _intake = new Item[ProductionData.intake.Length];
                        for (int i = 0; i < ProductionData.intake.Length; i++) {
                            _intake[i] = ProductionData.intake[i].Clone();
                        }
                    }
                    if (ProductionData.inputTyp == InputTyp.OR) {
                        _intake = new Item[1];
                        _intake[0] = ProductionData.intake[0].Clone();
                    }
                }
                return _intake;
            }
            set {
                _intake = value;
            }
        }

        #endregion Serialize

        #region RuntimeOrOther

        private int _orItemIndex = int.MinValue; //TODO think about to switch to short if it needs to save space

        public int OrItemIndex {
            get {
                if (_orItemIndex == int.MinValue) {
                    for (int i = 0; i < ProductionData.intake.Length; i++) {
                        Item item = ProductionData.intake[i];
                        if (item.ID == Intake[0].ID) {
                            _orItemIndex = i;
                        }
                    }
                }
                return _orItemIndex;
            }
            set {
                _orItemIndex = value;
            }
        }

        public Dictionary<OutputStructure, Item[]> RegisteredStructures;
        private MarketStructure nearestMarketStructure;
        public InputTyp InputTyp { get { return ProductionData.inputTyp; } }

        #endregion RuntimeOrOther

        protected ProductionPrototypeData _productionData;

        public ProductionPrototypeData ProductionData {
            get {
                if (_productionData == null) {
                    _productionData = (ProductionPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _productionData;
            }
        }

        public ProductionStructure(string id, ProductionPrototypeData ProductionData) {
            this.ID = id;
            this._productionData = ProductionData;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        protected ProductionStructure() {
            RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
        }

        protected ProductionStructure(ProductionStructure str) {
            OutputCopyData(str);
        }

        public override Structure Clone() {
            return new ProductionStructure(this);
        }

        public override void OnUpdate(float deltaTime) {
            if (Output == null) {
                return;
            }
            UpdateWorker(deltaTime);
            if (IsActiveAndWorking == false) {
                return;
            }
            for (int i = 0; i < Output.Length; i++) {
                if (Output[i].count == MaxOutputStorage) {
                    return;
                }
            }
            if (HasRequiredInput() == false) {
                return;
            }
            produceTimer += deltaTime;
            if (produceTimer >= ProduceTime) {
                produceTimer = 0;
                if (Intake != null) {
                    for (int i = 0; i < Intake.Length; i++) {
                        Intake[i].count--;
                    }
                }
                for (int i = 0; i < Output.Length; i++) {
                    Output[i].count += OutputData.output[i].count;
                    cbOutputChange?.Invoke(this);
                }
            }
        }

        public bool HasRequiredInput() {
            if (ProductionData.intake == null) {
                return true;
            }
            if (InputTyp == InputTyp.AND) {
                for (int i = 0; i < Intake.Length; i++) {
                    if (ProductionData.intake[i].count > Intake[i].count) {
                        return false;
                    }
                }
            }
            else if (InputTyp == InputTyp.OR) {
                if (ProductionData.intake[OrItemIndex].count > Intake[0].count) {
                    return false;
                }
            }
            return true;
        }

        public override void SendOutWorkerIfCan(float workTime = 1) {
            if (Workers.Count >= MaxNumberOfWorker || jobsToDo.Count == 0 && nearestMarketStructure == null) {
                return;
            }
            Dictionary<Item, int> needItems = new Dictionary<Item, int>();
            for (int i = 0; i < Intake.Length; i++) {
                if (GetMaxIntakeForIntakeIndex(i) > Intake[i].count) {
                    needItems.Add(Intake[i].Clone(), GetMaxIntakeForIntakeIndex(i) - Intake[i].count);
                }
            }
            if (needItems.Count == 0) {
                return;
            }
            if (jobsToDo.Count == 0 && nearestMarketStructure != null) {
                List<Item> getItems = new List<Item>();
                for (int i = Intake.Length - 1; i >= 0; i--) {
                    if (City.HasAnythingOfItem(Intake[i])) {
                        Item item = Intake[i].Clone();
                        item.count = GetMaxIntakeForIntakeIndex(i) - Intake[i].count;
                        getItems.Add(item);
                    }
                }
                if (getItems.Count <= 0) {
                    return;
                }
                Workers.Add(new Worker(this, nearestMarketStructure, workTime, OutputData.workerID, getItems.ToArray(), false));
                World.Current.CreateWorkerGameObject(Workers[0]);
            }
            else {
                base.SendOutWorkerIfCan();
            }
        }

        public void OnOutputChangedStructure(Structure str) {
            if (str is OutputStructure == false) {
                return;
            }
            if (jobsToDo.ContainsKey((OutputStructure)str)) {
                jobsToDo.Remove((OutputStructure)str);
            }
            OutputStructure ustr = ((OutputStructure)str);
            List<Item> getItems = new List<Item>();
            List<Item> items = new List<Item>(ustr.Output);
            foreach (Item item in RegisteredStructures[(OutputStructure)str]) {
                Item i = items.Find(x => x.ID == item.ID);
                if (i.count > 0) {
                    getItems.Add(i);
                }
            }
            if (((OutputStructure)str).outputClaimed == false) {
                jobsToDo.Add(ustr, getItems.ToArray());
            }
        }

        public bool AddToIntake(Inventory toAdd) {
            if (Intake == null) {
                return false;
            }
            for (int i = 0; i < Intake.Length; i++) {
                if ((Intake[i].count + toAdd.GetAmountForItem(Intake[i])) > GetMaxIntakeForIntakeIndex(i)) {
                    return false;
                }
                Intake[i].count += toAdd.GetAmountForItem(Intake[i]);
                toAdd.SetItemCountNull(Intake[i]);
                CallbackChangeIfnotNull();
            }

            return true;
        }

        public override Item[] GetRequieredItems(OutputStructure str, Item[] items) {
            List<Item> all = new List<Item>();
            for (int i = Intake.Length - 1; i >= 0; i--) {
                string id = Intake[i].ID;
                for (int s = 0; s < items.Length; s++) {
                    if (items[i].ID == id) {
                        Item item = items[i].Clone();
                        item.count = GetMaxIntakeForIntakeIndex(i) - Intake[i].count;
                        if (item.count > 0)
                            all.Add(item);
                    }
                }
            }
            return all.ToArray();
        }

        public override void OnBuild() {
            jobsToDo = new Dictionary<OutputStructure, Item[]>();
            RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
            //		for (int i = 0; i < intake.Length; i++) {
            //			intake [i].count = maxIntake [i];
            //		}
            if (RangeTiles != null) {
                foreach (Tile rangeTile in RangeTiles) {
                    if (rangeTile.Structure == null) {
                        continue;
                    }
                    if (rangeTile.Structure is OutputStructure) {
                        if (rangeTile.Structure is MarketStructure) {
                            FindNearestMarketStructure(rangeTile);
                            continue;
                        }
                        if (RegisteredStructures.ContainsKey((OutputStructure)rangeTile.Structure) == false) {
                            Item[] items = HasNeedItem(((OutputStructure)rangeTile.Structure).Output);
                            if (items.Length == 0) {
                                continue;
                            }
                            ((OutputStructure)rangeTile.Structure).RegisterOutputChanged(OnOutputChangedStructure);
                            RegisteredStructures.Add((OutputStructure)rangeTile.Structure, items);
                        }
                    }
                }
                City.RegisterStructureAdded(OnStructureBuild);
            }
            //FIXME this is a temporary fix to a stupid bug, which cause
            //i cant find because it works otherwise
            // bug is that myHome doesnt get set by json for this kind of structures
            // but it works for warehouse for example
            // to save save space we could always set it here but that would mean for every kind extra or in place structure???
            if (Workers != null) {
                foreach (Worker w in Workers) {
                    w.Home = this;
                }
            }
        }

        public void ChangeInput(Item change) {
            if (change == null) {
                return;
            }
            if (InputTyp == InputTyp.AND) {
                return;
            }
            for (int i = 0; i < ProductionData.intake.Length; i++) {
                Item item = ProductionData.intake[i];
                if (item.ID == change.ID) {
                    Intake[0] = item.Clone();
                    break;
                }
            }
        }

        /// <summary>
        /// Give an index for the needed Item, so only use in for loops
        /// OR
        /// for OR Inake use with orItemIndex
        /// </summary>
        /// <param name="i">The index.</param>
        public int GetMaxIntakeForIntakeIndex(int itemIndex) {
            if (itemIndex < 0 || itemIndex > ProductionData.intake.Length) {
                Debug.LogError("GetMaxIntakeMultiplier received an invalid number " + itemIndex);
                return -1;
            }
            return ProductionData.intake[itemIndex].count * 5; //TODO THINK ABOUT THIS
        }

        public Item[] HasNeedItem(Item[] output) {
            List<Item> items = new List<Item>();
            for (int i = 0; i < output.Length; i++) {
                for (int s = 0; s < Intake.Length; s++) {
                    if (output[i].ID == Intake[s].ID) {
                        items.Add(output[i]);
                    }
                }
            }
            return items.ToArray();
        }

        public void OnStructureBuild(Structure str) {
            if (str is OutputStructure == false) {
                return;
            }
            bool inRange = false;
            for (int i = 0; i < str.Tiles.Count; i++) {
                if (RangeTiles.Contains(str.Tiles[i]) == true) {
                    inRange = true;
                    break;
                }
            }
            if (inRange == false) {
                return;
            }
            if (str is MarketStructure) {
                FindNearestMarketStructure(str.BuildTile);
                return;
            }
            Item[] items = HasNeedItem(((OutputStructure)str).Output);
            if (items.Length > 0) {
                ((OutputStructure)str).RegisterOutputChanged(OnOutputChangedStructure);
                RegisteredStructures.Add((OutputStructure)str, items);
            }
        }
        public void FindNearestMarketStructure(Tile tile) {
            if (tile.Structure is MarketStructure) {
                if (nearestMarketStructure == null) {
                    nearestMarketStructure = (MarketStructure)tile.Structure;
                }
                else {
                    float firstDistance = nearestMarketStructure.Center.magnitude - Center.magnitude;
                    float secondDistance = tile.Structure.Center.magnitude - Center.magnitude;
                    if (Mathf.Abs(secondDistance) < Mathf.Abs(firstDistance)) {
                        nearestMarketStructure = (MarketStructure)tile.Structure;
                    }
                }
            }
        }

        public override void Load() {
            base.Load();
            if (_intake != null) {
                if(InputTyp == InputTyp.AND) {
                    _intake = _intake.ReplaceKeepCounts(ProductionData.intake);
                } else {
                    if(Array.Exists(ProductionData.intake, x=>x.ID == _intake[0].ID) == false) {
                        Debug.LogWarning("Prototype Intake Data changed for " + ID + " 'OR' does not contain last produced. " +
                            "Updated to first in array.");
                        _intake[0] = ProductionData.intake[0].Clone();
                    }
                }
            }
        }

    }
}