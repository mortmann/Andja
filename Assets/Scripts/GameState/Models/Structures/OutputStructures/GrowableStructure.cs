using Andja.Controller;
using Andja.Editor;
using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class GrowablePrototypeData : OutputPrototypData {
        public Fertility fertility;
        public int ageStages = 2;
        public string harvestSound;
        public bool isFloor;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GrowableStructure : OutputStructure, IGrowableStructure {

        #region Serialize

        [JsonPropertyAttribute]
        private float _age;

        [EditorSetField(minValue = 0, maxValueName = "AgeStages")]
        [JsonPropertyAttribute] public int currentStage;

        [JsonPropertyAttribute] public bool hasProduced;

        #endregion Serialize

        #region RuntimeOrOther

        public Fertility Fertility => GrowableData.fertility;
        public int AgeStages => GrowableData.ageStages;

        private GrowablePrototypeData _growableData;
        public float LandGrowModifier { get; protected set; }
        public override string SortingLayer => GrowableData.isFloor ? "Road" : "Structures";

        public GrowablePrototypeData GrowableData =>
                _growableData ??= (GrowablePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        protected float TimePerStage => (ProduceTime / (float)AgeStages);
        protected const float GrowTickTime = 1f;
        /// <summary>
        /// Is true when it is in range of a farm that requires it to function
        /// </summary>
        public bool IsBeingWorked => BeingWorkedBy > 0;
        public byte BeingWorkedBy { get; protected set; }
        #endregion RuntimeOrOther

        public GrowableStructure(string id, GrowablePrototypeData _growableData) {
            this.ID = id;
            this._growableData = _growableData;
        }

        protected GrowableStructure(GrowableStructure g) {
            BaseCopyData(g);
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public GrowableStructure() { }

        public override Structure Clone() {
            return new GrowableStructure(this);
        }

        public override void OnBuild(bool loading = false) {
            if (Fertility != null && City.HasFertility(Fertility) == false) {
                LandGrowModifier = 0;
            }
            else {
                //maybe have ground type be factor? stone etc
                LandGrowModifier = 1;
            }
            if (_age < currentStage * TimePerStage) {
                _age = currentStage * TimePerStage;
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (hasProduced || LandGrowModifier <= 0) {
                return;
            }
            _age += Efficiency * LandGrowModifier * (deltaTime);
            if ((_age > (currentStage + 1) * TimePerStage) == false) return;
            currentStage = Mathf.Clamp(currentStage + 1, 0, AgeStages);
            if (currentStage >= AgeStages) {
                Produce();
                return;
            }
            //Debug.Log ("Stage " + currentStage + " @ Time " + age);
            CallbackChangeIfNotNull();
        }

        public override bool SpecialCheckForBuild(System.Collections.Generic.List<Tile> tiles) {
            //this should be only ever 1 but for whateverreason it is not it still checks and doesnt really matter anyway
            return tiles.Where(t => t.Structure != null).All(t => t.Structure.ID != ID);
        }

        protected void Produce() {
            hasProduced = true;
            Output[0].count = 1;
            CallbackChangeIfNotNull();
        }

        public void Harvest() {
            Output[0].count = 0;
            currentStage = 0;
            _age = 0f;
            hasProduced = false;
            cbStructureSound?.Invoke(this, GrowableData.harvestSound, true);
            CallbackChangeIfNotNull();
        }

        #region override
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _growableData = null;
        }
        public override string GetSpriteName() {
            return base.GetSpriteName() + "_" + currentStage;
        }

        public override string ToString() {
            if (BuildTile == null) {
                return SpriteName + "@error";
            }
            return SpriteName + "@ X=" + BuildTile.X + " Y=" + BuildTile.Y + "\n "
                + "Age: " + _age + " Current Stage " + currentStage + " \n"
                + " HasProduced " + hasProduced;
        }
        /// <summary>
        /// Will count by how many structures it is worked by.
        /// Ideally only 1 or 2. Never more than 255.
        /// </summary>
        /// <param name="worked"></param>
        public void SetBeingWorked(bool worked) {
            if (BeingWorkedBy == byte.MaxValue) {
                Debug.LogError("Too many farms are working the same growable! This should never happen ...");
                return;
            }
            if (worked) {
                BeingWorkedBy++;
            }
            else {
                BeingWorkedBy--;
            }
        }

        #endregion override
    }
}