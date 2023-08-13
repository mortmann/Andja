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
        public IEffect[] effects;
        public Dictionary<Target, List<string>> specialRange;

        public ShadowType cloudCoverage;
        public Speed cloudSpeed;
        public Speed oceanSpeed;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameEvent {
        [JsonPropertyAttribute] public string ID;

        protected GameEventPrototypData _PrototypData;
        public Action<GameEvent> CbEventEnded;

        public GameEventPrototypData PrototypeData =>
            _PrototypData ??= (GameEventPrototypData)PrototypController.Instance.GetGameEventPrototypDataForID(ID);

        public Dictionary<Target, List<string>> SpecialRange => PrototypeData.specialRange;

        public EventType Type => PrototypeData.type;

        public IEffect[] Effects => PrototypeData.effects;
        public float Probability => PrototypeData.probability;
        public float MinDuration => PrototypeData.minDuration;
        public float MaxDuration => PrototypeData.maxDuration;
        public bool IsDone => currentDuration <= 0;
        public bool IsOneTime => MaxDuration <= 0;
        public string Name => PrototypeData.Name;
        public string Description => PrototypeData.Description;
        public ShadowType CloudCoverage => PrototypeData.cloudCoverage;
        public Speed CloudSpeed => PrototypeData.cloudSpeed;
        public Speed OceanSpeed => PrototypeData.oceanSpeed;

        private TargetGroup _targeted = new TargetGroup();

        public TargetGroup Targeted {
            get {
                if (Effects == null) return _targeted;
                foreach (IEffect e in Effects) {
                    _targeted.AddTargets(e.Targets);
                }
                return _targeted;
            }
        }

        [JsonPropertyAttribute] public float Duration;
        [JsonPropertyAttribute] public float currentDuration;

        [JsonPropertyAttribute] public float Range;
        public float Radius => Range / 2;


        [JsonPropertyAttribute] public Vector2 DefinedPosition;

        public Vector2 GetRealPosition() {
            return target switch {
                Structure s => s.Center,
                Unit u => u.PositionVector2,
                _ => DefinedPosition
            };
        }

        // this one says what it is...
        // so if complete island/city/player or only a single structuretype is the goal
        // can be null if its not set to which type
        [JsonPropertyAttribute] public IGEventable target;  //TODO make a check for it!

        [JsonPropertyAttribute] public uint eventID;
        [JsonPropertyAttribute] public float triggerEffectCooldown = UnityEngine.Random.Range(0.1f, 1f);

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
                DefinedPosition = ((IIsland)target).Features.Find(x => x.type == FeatureType.Volcano).position;
                CreateVolcanicEruption();
            }
            if(target != null) {
                
            }
            if (Type == EventType.Weather) { 
                if (Targeted.HasStructureTarget()) {
                    //loop through all tiles?
                }
            }
        }

        public void StartEvent(Vector2 pos) {
            if (target != null) {
                Debug.LogError("Events that have a position/range can't only target specific target.");
                return;
            }
            DefinedPosition = pos;
            Range = (PrototypeData.minRange + (PrototypeData.maxRange - PrototypeData.minRange) * UnityEngine.Random.Range(0, 1f));
            StartEvent();
        }
        public void StartEvent(IGEventable target) {
            if (target == null) {
                Debug.LogError("Events needs to be none null.");
                return;
            }
            this.target = target;
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
                num += UnityEngine.Random.Range(0, 1f) * ((MaxDuration - MinDuration) / numDice);
            }
            num += MinDuration;
            return num;
        }

        public bool HasWorldEffect() {
            if (Effects == null)
                return false;
            foreach (IEffect item in Effects) {
                if (item.InfluenceRange == InfluenceRange.World) {
                    return true;
                }
            }
            return false;
        }

        public bool IsValid() {
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

        internal void Stop() {
            CbEventEnded?.Invoke(this);
        }

        public void EffectTarget(GEventable t, bool start) {
            IEffect[] effectsForTarget = GetEffectsForTarget(t);
            if (effectsForTarget == null) {
                return;
            }
            if(start) {
                foreach (IEffect e in effectsForTarget) {
                    t.AddEffect(new Effect(e));
                }
            } else {
                foreach (IEffect e in effectsForTarget) {
                    t.RemoveEffect(new Effect(e));
                }
            }
        }

        public Effect[] GetEffectsForTarget(GEventable t) {
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
            //EventSpriteController.Instance.UpdateEventTileSprites(this, currentDuration / Duration);
            if (currentDuration > Duration) {
                StopVolcanicEruption();
                return;
            }
            if (triggerEffectCooldown > 0)
                return;
            if (UnityEngine.Random.Range(0f, 1f) > 1f - currentDuration / Duration) {
                for (int i = UnityEngine.Random.Range(1, 3); i > 0; i--) {
                    Vector2 goal = DefinedPosition + new Vector2(UnityEngine.Random.Range(-30, 31), UnityEngine.Random.Range(-30, 31));
                    Vector3 move = DefinedPosition - goal;
                    World.Current.OnCreateProjectile(
                        new Projectile(new World.WorldDamage(15), DefinedPosition, null, goal, move.normalized, move.magnitude, false, 4, true)
                        );
                }
            }
            triggerEffectCooldown = UnityEngine.Random.Range(0.1f, 1f);
            //change music (only if it is a player island?)
        }

        private void StopVolcanicEruption() {
            EventSpriteController.Instance.DestroyEventTileSprites(this);
        }
    }
}