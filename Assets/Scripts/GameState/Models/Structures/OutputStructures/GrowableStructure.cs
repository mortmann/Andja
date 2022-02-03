using Andja.Controller;
using Andja.Editor;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Andja.Model {

    public class GrowablePrototypeData : OutputPrototypData {
        public Fertility fertility;
        public int ageStages = 2;
        public string harvestSound;
        public bool isFloor;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GrowableStructure : OutputStructure {

        #region Serialize

        [JsonPropertyAttribute]
        private float age = 0;

        [EditorSetField(minValue = 0, maxValueName = "AgeStages")]
        [JsonPropertyAttribute] public int currentStage = 0;

        [JsonPropertyAttribute] public bool hasProduced = false;

        #endregion Serialize

        #region RuntimeOrOther

        public Fertility Fertility { get { return GrowableData.fertility; } }
        public int AgeStages { get { return GrowableData.ageStages; } }

        protected GrowablePrototypeData _growableData;
        private float landGrowModifier;
        public override string SortingLayer => GrowableData.isFloor? "Road" : "Structures";

        public GrowablePrototypeData GrowableData {
            get {
                if (_growableData == null) {
                    _growableData = (GrowablePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _growableData;
            }
        }

        protected float TimePerStage => (ProduceTime / (float)AgeStages + 1);
        protected const float GrowTickTime = 1f;
        /// <summary>
        /// Is true when it is in range of a farm that requires it to function
        /// </summary>
        public bool IsBeingWorked => beingWorkedBy > 0;
        private byte beingWorkedBy = 0;
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

        public override void OnBuild() {
            if (Fertility != null && City.HasFertility(Fertility) == false) {
                landGrowModifier = 0;
            }
            else {
                //maybe have ground type be factor? stone etc
                landGrowModifier = 1;
            }
            if (age < currentStage * TimePerStage) {
                age = currentStage * TimePerStage;
            }
        }

        public override void OnUpdate(float deltaTime) {
            if (hasProduced || landGrowModifier <= 0) {
                return;
            }
            age += Efficiency * landGrowModifier * (deltaTime);
            if ((age) > currentStage * TimePerStage) {
                currentStage = Mathf.Clamp(currentStage + 1, 0, AgeStages);
                if (currentStage >= AgeStages) {
                    Produce();
                    return;
                }
                //Debug.Log ("Stage " + currentStage + " @ Time " + age);
                CallbackChangeIfnotNull();
            }
        }

        public override bool SpecialCheckForBuild(System.Collections.Generic.List<Tile> tiles) {
            //this should be only ever 1 but for whateverreason it is not it still checks and doesnt really matter anyway
            foreach (Tile t in tiles) {
                if (t.Structure == null) {
                    continue;
                }
                if (t.Structure.ID == ID) {
                    return false;
                }
            }
            return true;
        }

        protected void Produce() {
            hasProduced = true;
            Output[0].count = 1;
            CallbackChangeIfnotNull();
        }

        public void Harvest() {
            Output[0].count = 0;
            currentStage = 0;
            age = 0f;
            CallbackChangeIfnotNull();
            hasProduced = false;
            cbStructureSound?.Invoke(this, GrowableData.harvestSound, true);
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
                + "Age: " + age + " Current Stage " + currentStage + " \n"
                + " HasProduced " + hasProduced;
        }
        /// <summary>
        /// Will count by how many structures it is worked by.
        /// Ideally only 1 or 2. Never more than 255.
        /// </summary>
        /// <param name="worked"></param>
        internal void SetBeingWorked(bool worked) {
            if(beingWorkedBy == byte.MaxValue) {
                Debug.LogError("Too many farms are working the same growable! This should never happen ...");
                return;
            }
            if(worked) {
                beingWorkedBy++;
            } else {
                beingWorkedBy--;
            }
        }

        #endregion override
    }
}