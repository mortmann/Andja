using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameEventPrototypData : LanguageVariables {
    public int ID = -1;

    public float probability = 10;
    public float minDuration = 50;
    public float maxDuration = 100;
    public float minRange = 50;
    public float maxRange = 100;
    public Effect[] effects;
    public Dictionary<Target, List<int>> specialRange;

}

[JsonObject(MemberSerialization.OptIn)]
public class GameEvent {
    [JsonPropertyAttribute] public int ID;

    protected GameEventPrototypData _PrototypData;
    public GameEventPrototypData PrototypData {
        get {
            if (_PrototypData == null) {
                _PrototypData = (GameEventPrototypData)PrototypController.Instance.GetGameEventPrototypDataForID(ID);
            }
            return _PrototypData;
        }
    }
    public Dictionary<Target, List<int>> SpecialRange => PrototypData.specialRange;

    public EventType EventType { protected set; get; }

    public Effect[] Effects => PrototypData.effects;
    public float Probability => PrototypData.probability;
    public float MinDuration => PrototypData.minDuration;
    public float MaxDuration => PrototypData.maxDuration;
    public bool IsDone { get { return currentDuration <= 0; } }
    public bool IsOneTime { get { return MaxDuration <= 0; } }
    public string Name { get { return EventType.ToString() + " - " + "EMPTY FOR NOW"; } }
    TargetGroup _Targeted;
    public TargetGroup Targeted {
        get {
            if(_Targeted == null) {
                _Targeted = new TargetGroup();
                foreach (Effect e in Effects) {
                    Targeted.AddTargets(e.Targets);
                }
            }
            return _Targeted;
        }
    }

    [JsonPropertyAttribute] public float currentDuration;
    //MAYBE range can also be a little random...?
    //around this as middle? Range+(-1^RandomInt(1,2)*Random(0,(Random(2,3)*Range)/(Range*Random(0.75,1)));
    [JsonPropertyAttribute] public float range;
    [JsonPropertyAttribute] public Vector2 position;
    // this one says what it is... 
    // so if complete island/city/player or only a single structuretype is the goal
    // can be null if its not set to which type
    [JsonPropertyAttribute] public IGEventable target;  //TODO make a check for it!
    [JsonPropertyAttribute] internal uint eventID;

    /// <summary>
    /// Needed for Serializing
    /// </summary>
    public GameEvent() {

    }
    public GameEvent(int id) {
        ID = id;
    }
    public GameEvent(GameEvent ge) {
        ID = ge.ID;
    }
    public GameEvent Clone() {
        return new GameEvent(this);
    }
    public void StartEvent() {
        //DO smth on start event?!
    }
    public void StartEvent(Vector2 pos) {
        if(target != null) {
            Debug.LogError("Events that have a position/range can't only target specific target.");
            return;
        }
        position = pos;
        currentDuration = WeightedRandomDuration();
    }
    public void Update(float delta) {
        if (currentDuration <= 0) {
            Debug.LogWarning("This Event is over, but still being updated (active)!");
        }
        currentDuration -= delta;
    }
    /// <summary>
    /// Weights around the middle of the range higher.
    /// SO its more likely to have the (max-min)/2 than max|min
    /// </summary>
    /// <returns>The random.</returns>
    /// <param name="numDice">Number dice.</param>
    float WeightedRandomDuration(int numDice = 5) {
        float num = 0;
        for (var i = 0; i < numDice; i++) {
            num += UnityEngine.Random.Range(0, 1.1f) * ((MaxDuration - MinDuration) / numDice);
        }
        num += MinDuration;
        return num;
    }
    public bool HasWorldEffect() {
        foreach (Effect item in Effects) {
            if (item.InfluenceRange == InfluenceRange.World) {
                return true;
            }
        }
        return false;
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
        if(SpecialRange != null) {
            foreach (Target target in t.TargetGroups.Targets) {
                if (SpecialRange.ContainsKey(target)) {
                    if(SpecialRange[target].Contains(t.GetID()) == false) {
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
            Debug.LogError("Influence is null!");
            return;
        }
        t.AddEffects(effectsForTarget);
    }

    public Effect[] GetEffectsForTarget(IGEventable t) {
        List<Effect> effectsForTarget = new List<Effect>();
        foreach (Effect eff in Effects) {
            if (t.TargetGroups.IsTargeted(eff.Targets) == false) {
                continue;
            }
            effectsForTarget.Add(eff);
        }
        return effectsForTarget.ToArray();
    }

}
