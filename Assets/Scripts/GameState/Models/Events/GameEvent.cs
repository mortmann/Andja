using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using EventType = Andja.Controller.EventType;

namespace Andja.Model {

    public class GameEventPrototypData : LanguageVariables {
        public string ID;
        public EventType type;
        public float probability = 10;
        public float minDuration = 50;
        public float maxDuration = 100;
        public float minRange = 50;
        public float maxRange = 100;
        public Effect[] effects;
        public Dictionary<Target, List<string>> specialRange;

        public ShadowType cloudCoverage;
        public Speed cloudSpeed;
        public Speed oceanSpeed;

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameEvent {
        [JsonPropertyAttribute] public string ID;

        protected GameEventPrototypData _PrototypData;

        public GameEventPrototypData PrototypData {
            get {
                if (_PrototypData == null) {
                    _PrototypData = (GameEventPrototypData)PrototypController.Instance.GetGameEventPrototypDataForID(ID);
                }
                return _PrototypData;
            }
        }

        public Dictionary<Target, List<string>> SpecialRange => PrototypData.specialRange;

        public EventType Type => PrototypData.type;

        public Effect[] Effects => PrototypData.effects;
        public float Probability => PrototypData.probability;
        public float MinDuration => PrototypData.minDuration;
        public float MaxDuration => PrototypData.maxDuration;
        public bool IsDone { get { return currentDuration <= 0; } }
        public bool IsOneTime { get { return MaxDuration <= 0; } }
        public string Name => PrototypData.Name;
        public string Description => PrototypData.Description;
        public ShadowType CloudCoverage => PrototypData.cloudCoverage;
        public Speed CloudSpeed => PrototypData.cloudSpeed;
        public Speed OceanSpeed => PrototypData.oceanSpeed;

        private TargetGroup _Targeted = new TargetGroup();

        public TargetGroup Targeted {
            get {
                if (Effects != null) {
                    foreach (Effect e in Effects) {
                        _Targeted.AddTargets(e.Targets);
                    }
                }
                return _Targeted;
            }
        }

        [JsonPropertyAttribute] public float Duration;
        [JsonPropertyAttribute] public float currentDuration;

        //MAYBE range can also be a little random...?
        //around this as middle? Range+(-1^RandomInt(1,2)*Random(0,(Random(2,3)*Range)/(Range*Random(0.75,1)));
        [JsonPropertyAttribute] public float range;

        [JsonPropertyAttribute] public Vector2 position;

        internal Vector2 GetPosition() {
            if (target is Structure s)
                return s.Center;
            if (target is Unit u)
                return u.PositionVector2;
            return position;
        }

        // this one says what it is...
        // so if complete island/city/player or only a single structuretype is the goal
        // can be null if its not set to which type
        [JsonPropertyAttribute] public IGEventable target;  //TODO make a check for it!

        [JsonPropertyAttribute] internal uint eventID;
        [JsonPropertyAttribute] private float triggerEffectCooldown = UnityEngine.Random.Range(0.1f, 1f);

        /// <summary>
        /// Needed for Serializing
        /// </summary>
        public GameEvent() {
        }

        public GameEvent(string id) {
            ID = id;
        }

        public GameEvent(GameEvent ge) {
            ID = ge.ID;
        }

        public GameEvent Clone() {
            return new GameEvent(this);
        }

        public void StartEvent() {
            Duration = WeightedRandomDuration();
            currentDuration = Duration;
            //DO smth on start event?!
            if (ID == "volcanic_eruption") {
                position = ((IIsland)target).Features.Find(x => x.type == FeatureType.Volcano).position;
                CreateVolcanicEruption();
            }
            if(target != null) {

            }
        }

        public void StartEvent(Vector2 pos) {
            if (target != null) {
                Debug.LogError("Events that have a position/range can't only target specific target.");
                return;
            }
            position = pos;
            StartEvent();
        }

        public void Update(float delta) {
            if (currentDuration <= 0) {
                Debug.LogWarning("This Event is over, but still being updated (active)!");
            }
            currentDuration -= delta;
            triggerEffectCooldown -= delta;
            if (ID == "volcanic_eruption") {
                UpdateVolcanicEruption();
            }
        }

        /// <summary>
        /// Weights around the middle of the range higher.
        /// SO its more likely to have the (max-min)/2 than max|min
        /// </summary>
        /// <returns>The random.</returns>
        /// <param name="numDice">Number dice.</param>
        private float WeightedRandomDuration(int numDice = 5) {
            float num = 0;
            for (var i = 0; i < numDice; i++) {
                num += UnityEngine.Random.Range(0, 1.1f) * ((MaxDuration - MinDuration) / numDice);
            }
            num += MinDuration;
            return num;
        }

        public bool HasWorldEffect() {
            if (Effects == null)
                return false;
            foreach (Effect item in Effects) {
                if (item.InfluenceRange == InfluenceRange.World) {
                    return true;
                }
            }
            return false;
        }

        internal bool IsValid() {
            if (target is Island) {
                if (((IIsland)target).Features != null) {
                    if (SpecialRange[Target.Island].Exists(t => ((IIsland)target).Features.Exists(x => x.ID == t))) {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether this instance is target the specified Event targets.
        /// This includes the Player, City and Island.
        /// </summary>
        /// <returns><c>true</c> if this instance is target the specified event otherwise, <c>false</c>.</returns>
        /// <param name="t">T.</param>
        public bool IsTarget(IGEventable t) {
            //when the event is limited to a specific area or player
            if (target != null) {
                if (target is Player && t is Player) {
                    if (target.GetPlayerNumber() != t.GetPlayerNumber()) {
                        return false;
                    }
                }
                else
                //needs to be tested if works if not every city/island needs identification
                if (target != t) {
                    return false;
                }
            }
            //if we are here the IGEventable t is in "range"(specified target eg island andso)
            //or there is no range atall
            //is there an influence targeting t ?
            if (Targeted.IsTargeted(t.TargetGroups) == false) {
                return false;
            }
            if (SpecialRange != null) {
                foreach (Target target in t.TargetGroups.Targets) {
                    if (SpecialRange.ContainsKey(target)) {
                        if (SpecialRange[target].Contains(t.GetID()) == false) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void EffectTarget(IGEventable t, bool start) {
            Effect[] effectsForTarget = GetEffectsForTarget(t);
            if (effectsForTarget == null) {
                return;
            }
            foreach (Effect e in effectsForTarget) {
                t.AddEffect(new Effect(e));
            }
        }

        public Effect[] GetEffectsForTarget(IGEventable t) {
            if (Effects == null)
                return null;
            List<Effect> effectsForTarget = new List<Effect>();
            foreach (Effect eff in Effects) {
                if (t.TargetGroups.IsTargeted(eff.Targets) == false) {
                    continue;
                }
                effectsForTarget.Add(eff);
            }
            return effectsForTarget.ToArray();
        }

        //Spezialfunctions!
        public void CreateVolcanicEruption() {
            //create the image of lava
            EventSpriteController.Instance.CreateEventTileSprites(ID, this);
            //change music (only if it is a player island?)
            //
        }

        public void UpdateVolcanicEruption() {
            //create the image of lava
            EventSpriteController.Instance.UpdateEventTileSprites(this, currentDuration / Duration);
            if (currentDuration > Duration) {
                StopVolcanicEruption();
                return;
            }
            if (triggerEffectCooldown > 0)
                return;
            if (UnityEngine.Random.Range(0f, 1f) > 1f - currentDuration / Duration) {
                for (int i = UnityEngine.Random.Range(1, 3); i > 0; i--) {
                    Vector2 goal = position + new Vector2(UnityEngine.Random.Range(-30, 31), UnityEngine.Random.Range(-30, 31));
                    Vector3 move = position - goal;
                    World.Current.OnCreateProjectile(
                        new Projectile(new World.WorldDamage(200), position, null, goal, move.normalized, move.magnitude, false, 4, true)
                        );
                }
            }
            triggerEffectCooldown = UnityEngine.Random.Range(0.1f, 1f);
            //create projectiles flying from it
            //  -at random times
            //  -to random tiles (how far?)
            //  -make them only "hit" the destination tiles
            //World.Current.OnC
            //create sounds
            //change music (only if it is a player island?)
            //
        }

        private void StopVolcanicEruption() {
            EventSpriteController.Instance.DestroyEventTileSprites(this);
        }
    }
}