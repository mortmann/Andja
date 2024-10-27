using Andja.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Andja.Model.Components;
using Andja.UI;
using Andja.Utility;

namespace Andja.Controller {
    //TODO:
    //-We need a random generator that decides what happens
    // -> for that we need to decide how frequently what happens
    // -> on the Constant Data Object are the diffrent kinds of catastrophes that can happen in this >round<
    // -> depends on player-city advances in structures can have effect on what can happen -> should it check it or city itself
    //-decide how to implement the diffrent kinds of effects on the structures/units
    // -> how to make units faster/slower depending where they are?
    //-is this also for quests? Will there be even some? If so there must be a ton to keep them from repetitive

    /// <summary>
    /// Event type. Specify which kind of Event happens.
    /// TODO: TO DECIDE IF QUEST ARE HANDLED HERE
    /// </summary>
    public enum EventType { Weather, City, Structure, Quest, Disaster, Other }

    public enum InfluenceRange { World, Island, City, Structure, Range, Player }

    public enum InfluenceTyp { Structure, Unit }

    /*
     *Every Event has to have:
     * -Position (Center, Origin)
     * -Range (City, Island, Tile distance,...)
     * -duration (Min-Max, onetime)
     * -influence Typ Array (multiple possible)
     * 	-BuildingTyp
     * 	  -influence Typ
     * 	  -influence amount if needed
     *  -Unit
     *    -influence typ? or only movement?
     * 		-amount
     * -restricts building on island --> has to be implemented
     * -
     *
    */

    public class EventController : MonoBehaviour {
        public static EventController Instance { get; protected set; }
        public static float RandomTickTime = 1f;
        public EventHoldingScript EventRangePrefab;
        private uint _lastId = 0;
        private Dictionary<EventType, List<GameEvent>> _typeToEvents;
        private Dictionary<uint, GameEvent> _idToActiveEvent;
        public IEnumerable<GameEvent> AllActiveEvents => _idToActiveEvent.Values;
        //Every EventType has a chance to happen
        private Dictionary<EventType, float> _chanceToEvent;

        private Action<GameEvent> _cbEventCreated;
        private Action<GameEvent> _cbEventEnded;

        private float _timeSinceLastEvent;
        private float _nextRandomTick = RandomTickTime;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two event controllers.");
            }
            Instance = this;
        }

        public void Start() {
            _idToActiveEvent = new Dictionary<uint, GameEvent>();
            _chanceToEvent = new Dictionary<EventType, float>();
            _typeToEvents = new Dictionary<EventType, List<GameEvent>>();
            foreach(EventType et in Enum.GetValues(typeof(EventType))) {
                _typeToEvents[et] = new List<GameEvent>();
            }
            foreach(var gpd in PrototypController.Instance.GameEventPrototypeDatas.Values) {
                _typeToEvents[gpd.type].Add(new GameEvent(gpd.ID));
            }
            foreach(var tte in _typeToEvents) {
                _chanceToEvent[tte.Key] = tte.Value.Sum(x => x.Probability);
            }
            float allChance = _chanceToEvent.Values.Sum();
            foreach (var tte in _typeToEvents) {
                _chanceToEvent[tte.Key] /= allChance;
            }
        }

        // Handle here Events that will effect whole islands or
        // World-Segments, maybe fire and similars
        public void FixedUpdate() {
            if (WorldController.Instance.IsPaused) {
                return;
            }
            //update and remove inactive events
            foreach (uint i in _idToActiveEvent.Keys.ToArray()) {
                if (_idToActiveEvent[i].IsDone) {
                    StopGameEvent(i);
                }
                else {
                    _idToActiveEvent[i].Update(WorldController.Instance.FixedDeltaTime);
                }
            }
            if(_nextRandomTick > 0) {
                _nextRandomTick = Mathf.Clamp01(_nextRandomTick - Time.fixedDeltaTime);
                return;
            }
            _nextRandomTick = RandomTickTime;
            //Now will there be an event or not?
            if (RandomIf() == false) {
                return;
            }
            //Debug.Log("event " + GameData.Instance.playTime);
            if(CreateRandomEvent()) {
                _timeSinceLastEvent = 0;
            }
        }

        public bool CreateRandomEvent() {
            //TODO Randomize which event it is better
            return CreateRandomTypeEvent(RandomType());
        }

        public bool CreateRandomTypeEvent(EventType type) {
            //now find random the target of the GameEvent
            return CreateGameEvent(RandomEvent(type));
        }

        internal void SetGameEventData(GameEventSave ges) {
            _idToActiveEvent = ges.idToActiveEvent;
            _nextRandomTick = ges.nextRandomTick;
        }

        public bool CreateGameEvent(GameEvent ge) {
            if (ge == null || ge.IsValid() == false) {
                return false;
            }
            Debug.Log("Created event " + ge.eventID);
            //fill the type
            _idToActiveEvent.Add(_lastId, ge);
            ge.eventID = _lastId;
            switch (ge.Type) {
                case EventType.Weather:
                    ge.StartEvent(GetRandomVector2());
                    break;
                case EventType.Disaster:
                    ge.StartEvent(GetRandomValidIsland(ge));
                    break;
                case EventType.City:
                case EventType.Structure:
                case EventType.Quest:
                case EventType.Other:
                    ge.StartEvent();
                    break;
            }
            if (ge.Targeted.HasUnitTarget()) {
                CreateEventHoldingGameObject(ge);
            }
            _cbEventCreated(ge);
            _lastId++;
            return true;
        }

        private IGEventable GetRandomValidIsland(GameEvent ge) {
            if (ge.ID != "volcanic_eruption")
                Log.GAME_WARNING("GetRandomValidIsland is not fully implemented yet");
            List<Island> islands = World.Current.Islands.FindAll(i => i.Features?.Exists(f => f.type == FeatureType.Volcano) == true);
            if(islands.Count == 0) {
                return null;
            }
            return islands.RandomElement();
        }

        private void CreateEventHoldingGameObject(GameEvent ge) {
            EventHoldingScript eventHoldingScript = Instantiate(EventRangePrefab);
            eventHoldingScript.gameEvent = ge;
        }

        /// <summary>
        /// Random Player or ALL will be chosen depending on TargetTypes
        /// </summary>
        /// <param name="id"></param>
        internal bool TriggerEvent(string id) {
            GameEvent gameEvent = new GameEvent(id);
            if(gameEvent.Type == EventType.Weather || gameEvent.Type == EventType.Disaster) {
                return CreateGameEvent(gameEvent); 
            }
            return TriggerEventForPlayer(gameEvent, PlayerController.Instance.GetRandomPlayer());
        }

        internal bool TriggerEventForPlayer(GameEvent gameEvent, Player player) {
            List<IGEventable> playerTargets = GetPlayerTargets(gameEvent.Targeted, player);
            return playerTargets.Count != 0 && TriggerEventForEventable(gameEvent, playerTargets[UnityEngine.Random.Range(0, playerTargets.Count)]);
        }

        internal bool TriggerEventForEventable(GameEvent gameEvent, IGEventable eventable) {
            gameEvent.target = eventable;
            return CreateGameEvent(gameEvent);
        }
        public List<uint> GetActiveEventsIDs() {
            return _idToActiveEvent.Keys.ToList();
        }
        internal bool StopGameEvent(uint id) {
            if (_idToActiveEvent.ContainsKey(id) == false)
                return false;
            _cbEventEnded(_idToActiveEvent[id]);
            _idToActiveEvent[id].Stop();
            return _idToActiveEvent.Remove(id);
        }

        public List<IGEventable> GetPlayerTargets(TargetGroup targetGroup, Player player) {
            List<IGEventable> targets = new List<IGEventable>();
            foreach (Target target in targetGroup.Targets) {
                switch (target) {
                    case Target.AllUnit:
                        targets.AddRange(player.Units);
                        break;

                    case Target.Ship:
                        targets.AddRange(player.GetShipUnits());
                        break;

                    case Target.LandUnit:
                        targets.AddRange(player.GetLandUnits());
                        break;

                    case Target.Island:
                        targets.AddRange(player.GetIslandList());
                        break;

                    case Target.City:
                        targets.AddRange(player.Cities.ConvertAll(x=>(City)x));
                        break;

                    case Target.AllStructure:
                        targets.AddRange(player.AllStructures);
                        break;
                    case Target.DamageableStructure:
                        targets.AddRange(player.AllStructures.Where(x => x.CanTakeDamage));
                        break;
                    case Target.BurnableStructure:
                        targets.AddRange(player.AllStructures.Where(x => x.CanStartBurning));
                        break;
                    case Target.RoadStructure:
                        targets.AddRange(player.AllStructures.OfType<RoadStructure>());
                        break;
                    case Target.NeedStructure:
                        targets.AddRange(player.AllStructures.OfType<NeedStructure>());
                        break;
                    case Target.MilitaryStructure:
                        targets.AddRange(player.AllStructures.OfType<MilitaryStructure>());
                        break;
                    case Target.HomeStructure:
                        targets.AddRange(player.AllStructures.OfType<HomeStructure>());
                        break;
                    case Target.ServiceStructure:
                        targets.AddRange(player.AllStructures.OfType<ServiceStructure>());
                        break;
                    case Target.GrowableStructure:
                        targets.AddRange(player.AllStructures.OfType<GrowableStructure>());
                        break;
                    case Target.OutputStructure:
                        targets.AddRange(player.AllStructures.OfType<OutputStructure>());
                        break;
                    case Target.MarketStructure:
                        targets.AddRange(player.AllStructures.OfType<MarketStructure>());
                        break;
                    case Target.WarehouseStructure:
                        targets.AddRange(player.AllStructures.OfType<WarehouseStructure>());
                        break;
                    case Target.MineStructure:
                        targets.AddRange(player.AllStructures.OfType<MineStructure>());
                        break;
                    case Target.FarmStructure:
                        targets.AddRange(player.AllStructures.OfType<FarmStructure>());
                        break;
                    case Target.ProductionStructure:
                        targets.AddRange(player.AllStructures.OfType<ProductionStructure>());
                        break;
                    //non player targets
                    case Target.World:
                        break;
                    case Target.Player:
                        break;
                }
            }
            return targets;
        }

        private GEventable GetEventTargetForEventType(EventType type) {
            GEventable ige = null;
            //some times should be target all cities...
            //idk how todo do it tho...
            switch (type) {
                case EventType.City:
                    List<ICity> cities = new List<ICity>();
                    foreach (Island item in World.Current.Islands) {
                        cities.AddRange(item.Cities);
                    }
                    ige = RandomItemFromList(cities.ConvertAll(x=>(City)x));
                    break;

                case EventType.Disaster:
                    ige = RandomItemFromList(World.Current.Islands);
                    break;

                case EventType.Weather:
                    //????
                    //range?
                    //position?
                    //island?
                    break;

                case EventType.Structure:
                    //probably go like:
                    //  random island
                    //  random city
                    //  random structure
                    //  random effect for structure type?
                    float r = UnityEngine.Random.Range(0f, 1f);
                    if (r < 0.4f) { //idk
                        return null; // there is no specific target
                    }
                    IIsland i = RandomItemFromList(World.Current.Islands);
                    ICity c = RandomItemFromList(i.Cities);
                    if (c.PlayerNumber == -1) { // random decided there will be no event?
                                                // are there events for wilderniss structures?
                    }
                    else {
                        ige = RandomItemFromList(c.Structures);
                    }
                    break;

                case EventType.Quest:
                    Debug.LogWarning("Not yet implemented");
                    break;

                case EventType.Other:
                    Debug.LogWarning("Not yet implemented");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return ige;
        }

        private bool RandomIf() {
            _timeSinceLastEvent += WorldController.Instance.DeltaTime;
            return UnityEngine.Random.Range(-1, 2) *
                (UnityEngine.Random.Range(-1f, 0.901f + Mathf.Max(0f, Mathf.Exp(0.01f * _timeSinceLastEvent / 15f) - 1f))
                + Mathf.Max(0, (Mathf.Exp((1f / 180f) * (_timeSinceLastEvent / 15f)) - 2f))) > 1;
        }

        private GameEvent RandomEvent(EventType type) {
            if (_typeToEvents.ContainsKey(type) == false)
                return null;
            if (_typeToEvents[type].Count == 0)
                return null;
            List<GameEvent> ges = _typeToEvents[type];
            //TODO move this to the load -> dic<type,sum>
            float sumOfProbability = ges.Sum(item => item.Probability);
            float randomNumber = UnityEngine.Random.Range(0, sumOfProbability);
            float sum = 0;
            foreach (GameEvent item in ges) {
                sum += item.Probability;
                if (sum <= randomNumber) {
                    return item.Clone();
                }
            }
            Debug.LogWarning("No event found for this!");
            return null;
        }

        private EventType RandomType() {
            float r = UnityEngine.Random.Range(0f, 1f);
            float sum = 0;
            foreach (EventType item in _chanceToEvent.Keys) {
                sum += _chanceToEvent[item];
                if (sum >= r) {
                    return item;
                }
            }
            Debug.LogError("No type found for this event!");
            return EventType.City;
        }

        internal void ListAllActiveEvents() {
            string list = "Active Events:\n";
            list += "EventID - ID(Type) - Target - CurrentDuration/Duration\n";
            foreach (uint id in _idToActiveEvent.Keys) {
                string target;
                if(_idToActiveEvent[id].target == null) {
                    target = (_idToActiveEvent[id].DefinedPosition + " " + _idToActiveEvent[id].Range);
                } else {
                    target = _idToActiveEvent[id].target.ToString();
                }
                list += id + " - " + _idToActiveEvent[id].ID + " ("+_idToActiveEvent[id].Type+")" + " - " + target
                    + " - " + _idToActiveEvent[id].currentDuration + "/" + _idToActiveEvent[id].Duration + "\n";
            }
            list += "END";
            Debug.Log(list);
        }

        private T RandomItemFromList<T>(List<T> iges) {
            return iges[UnityEngine.Random.Range(0, iges.Count - 1)];
        }

        public float GetWeightedYFromRandomX(float size, float dist, float randomX) {
            float x1 = 0f;
            float y1 = 0f;
            float x2 = dist;
            float y2 = size;
            float x3 = dist * 2;
            float y3 = 0f;
            float denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
            float A = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
            float B = (x3 * x3 * (y1 - y2) + x2 * x2 * (y3 - y1) + x1 * x1 * (y2 - y3)) / denom;
            float C = (x2 * x3 * (x2 - x3) * y1 + x3 * x1 * (x3 - x1) * y2 + x1 * x2 * (x1 - x2) * y3) / denom;
            //		Debug.Log (A+"x^2 + "+ B +"x +"+C);
            return A * Mathf.Pow(randomX, 2) + B * randomX + C;
        }

        public Vector2 GetRandomVector2() {
            Vector2 vec = new Vector2();
            float w = (float)World.Current.Width / 2f;
            float h = (float)World.Current.Height / 2f;

            vec.x = (UnityEngine.Random.Range(0, 2) * w + GetWeightedYFromRandomX(w, w * 1.25f, UnityEngine.Random.Range(0, w)));
            vec.y = (UnityEngine.Random.Range(0, 2) * h + GetWeightedYFromRandomX(h, h * 1.25f, UnityEngine.Random.Range(0, h)));
            return vec;
        }

        public static Action<TParam1> CreateReusableAction<TParam1>(string methodName) {
            var method = typeof(GameEventFunctions).GetMethod(methodName);
            var del = Delegate.CreateDelegate(typeof(Action<TParam1>), method);
            Action<TParam1> caller = (instance) => del.DynamicInvoke(instance);
            return caller;
        }

        public static Action<object, object> CreateReusableAction<TParam1, TParam2>(string methodName) {
            var method = typeof(GameEventFunctions).GetMethod(methodName, new Type[] { typeof(TParam1), typeof(TParam2) });
            var del = Delegate.CreateDelegate(typeof(Action<TParam1, TParam2>), method);
            Action<object, object> caller = (instance, param) => del.DynamicInvoke(instance, param);
            return caller;
        }

        public static Action<object, object, object> CreateReusableAction<TParam1, TParam2, TParam3>(string methodName) {
            var method = typeof(GameEventFunctions).GetMethod(methodName, new Type[] { typeof(TParam1), typeof(TParam2), typeof(TParam3) });
            var del = Delegate.CreateDelegate(typeof(Action<TParam1, TParam2, TParam3>), method);
            Action<object, object, object> caller = (param1, param2, param3) => del.DynamicInvoke(param1, param2, param3);
            return caller;
        }

        public void RegisterOnEvent(Action<GameEvent> create, Action<GameEvent> ending) {
            _cbEventCreated += create;
            _cbEventEnded += ending;
        }

        public static Type TargetToType(Target target) {
            return target switch {
                Target.World => typeof(World),
                Target.Player => typeof(Player),
                Target.Island => typeof(Island),
                Target.City => typeof(City),
                Target.AllUnit => typeof(Unit),
                Target.Ship => typeof(Ship),
                //default type is unit -- so dunno what todo in this case
                Target.LandUnit => typeof(Unit),
                Target.AllStructure => typeof(Structure),
                //is selected over bool -- so dunno what todo in this case
                Target.DamageableStructure => typeof(Structure),
                Target.RoadStructure => typeof(RoadStructure),
                Target.NeedStructure => typeof(NeedStructure),
                Target.MilitaryStructure => typeof(MilitaryStructure),
                Target.HomeStructure => typeof(HomeStructure),
                Target.ServiceStructure => typeof(ServiceStructure),
                Target.GrowableStructure => typeof(GrowableStructure),
                Target.OutputStructure => typeof(OutputStructure),
                Target.MarketStructure => typeof(MarketStructure),
                Target.WarehouseStructure => typeof(WarehouseStructure),
                Target.MineStructure => typeof(MineStructure),
                Target.FarmStructure => typeof(FarmStructure),
                Target.ProductionStructure => typeof(ProductionStructure),
                _ => null
            };
        }

        public void OnDestroy() {
            Instance = null;
        }

        public GameEventSave GetSaveGameEventData() {
            GameEventSave ges = new GameEventSave(_idToActiveEvent, _nextRandomTick);
            return ges;
        }

        public GameEvent GetEventByID(uint gameEventID) {
            return _idToActiveEvent[gameEventID];
        }
    }

    public class GameEventSave : BaseSaveData {
        public Dictionary<uint, GameEvent> idToActiveEvent;
        public float nextRandomTick;
        public GameEventSave(Dictionary<uint, GameEvent> idToActiveEvent, float randomTick) {
            this.idToActiveEvent = idToActiveEvent;
            nextRandomTick = randomTick;
        }

        public GameEventSave() {
        }
    }
}