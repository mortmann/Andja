using Andja.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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

        private uint lastID = 0;
        private Dictionary<EventType, List<GameEvent>> typeToEvents;
        private Dictionary<uint, GameEvent> idToActiveEvent;

        //Every EventType has a chance to happen
        private Dictionary<EventType, float> chanceToEvent;

        private Action<GameEvent> cbEventCreated;
        private Action<GameEvent> cbEventEnded;

        private float timeSinceLastEvent = 0;
        private World world;
        float nextRandomTick = RandomTickTime;
        // Use this for initialization
        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two event controllers.");
            }
            Instance = this;

            //for (int i = 0; i < s; i++) {
            //    x += (UnityEngine.Random.Range(0, 2) * 500 + GetWeightedYFromRandomX(500, 750, UnityEngine.Random.Range(0, 500)));
            //    y += (UnityEngine.Random.Range(0, 2) * 500 + GetWeightedYFromRandomX(500, 750, UnityEngine.Random.Range(0, 500)));
            //}

        }

        private void Start() {
            world = World.Current;
            idToActiveEvent = new Dictionary<uint, GameEvent>();
            chanceToEvent = new Dictionary<EventType, float>();
            typeToEvents = new Dictionary<EventType, List<GameEvent>>();
            foreach(EventType et in Enum.GetValues(typeof(EventType))) {
                typeToEvents[et] = new List<GameEvent>();
            }
            foreach(var gpd in PrototypController.Instance.GameEventPrototypeDatas.Values) {
                typeToEvents[gpd.type].Add(new GameEvent(gpd.ID));
            }
            foreach(var tte in typeToEvents) {
                chanceToEvent[tte.Key] = tte.Value.Sum(x => x.Probability);
            }
            float allChance = chanceToEvent.Values.Sum();
            foreach (var tte in typeToEvents) {
                chanceToEvent[tte.Key] /= allChance;
            }
        }

        // Handle here Events that will effect whole islands or
        // World-Segments, maybe fire and similars
        private void FixedUpdate() {
            if (WorldController.Instance.IsPaused) {
                return;
            }
            //update and remove inactive events
            List<uint> ids = new List<uint>(idToActiveEvent.Keys);
            foreach (uint i in ids) {
                if (idToActiveEvent[i].IsDone) {
                    StopGameEvent(i);
                }
                else {
                    idToActiveEvent[i].Update(WorldController.Instance.FixedDeltaTime);
                }
            }
            if(nextRandomTick > 0) {
                nextRandomTick = Mathf.Clamp01(nextRandomTick - Time.fixedDeltaTime);
                return;
            }
            nextRandomTick = RandomTickTime;
            //Now will there be an event or not?
            if (RandomIf() == false) {
                return;
            }
            //Debug.Log("event " + GameData.Instance.playTime);
            if(CreateRandomEvent()) {
                timeSinceLastEvent = 0;
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
            idToActiveEvent = ges.idToActiveEvent;
            nextRandomTick = ges.nextRandomTick;
        }

        public bool CreateGameEvent(GameEvent ge) {
            if (ge == null || ge.IsValid() == false) {
                return false;
            }
            if (ge.Targeted != null && ge.target == null) {
                return false;
            }
            Debug.Log("Created event " + ge.eventID);
            //fill the type
            idToActiveEvent.Add(lastID, ge);
            ge.eventID = lastID;
            ge.StartEvent();
            cbEventCreated(ge);
            lastID++;
            return true;
        }

        /// <summary>
        /// Random Player or ALL will be chosen depending on TargetTypes
        /// </summary>
        /// <param name="id"></param>
        internal bool TriggerEvent(string id) {
            GameEvent gameEvent = new GameEvent(id);
            return TriggerEventForPlayer(gameEvent, PlayerController.Instance.GetRandomPlayer());
        }

        internal bool TriggerEventForPlayer(GameEvent gameEvent, Player player) {
            List<IGEventable> playerTargets = GetPlayerTargets(gameEvent.Targeted, player);
            if (playerTargets.Count == 0)
                return false;
            return TriggerEventForEventable(gameEvent, playerTargets[UnityEngine.Random.Range(0, playerTargets.Count)]);
        }

        internal bool TriggerEventForEventable(GameEvent gameEvent, IGEventable eventable) {
            gameEvent.target = eventable;
            return CreateGameEvent(gameEvent);
        }

        internal bool StopGameEvent(uint id) {
            if (idToActiveEvent.ContainsKey(id) == false)
                return false;
            cbEventEnded(idToActiveEvent[id]);
            return idToActiveEvent.Remove(id);
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
                        targets.AddRange(player.Cities);
                        break;

                    case Target.AllStructure:
                        targets.AddRange(player.AllStructures);
                        break;
                    case Target.DamagableStructure:
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

        private IGEventable GetEventTargetForEventType(EventType type) {
            IGEventable ige = null;
            //some times should be target all cities...
            //idk how todo do it tho...
            switch (type) {
                case EventType.City:
                    List<City> cities = new List<City>();
                    foreach (Island item in world.Islands) {
                        cities.AddRange(item.Cities);
                    }
                    ige = RandomItemFromList<City>(cities);
                    break;

                case EventType.Disaster:
                    ige = RandomItemFromList<Island>(world.Islands);
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
                    Island i = RandomItemFromList<Island>(world.Islands);
                    City c = RandomItemFromList<City>(i.Cities);
                    if (c.PlayerNumber == -1) { // random decided there will be no event?
                                                // are there events for wilderniss structures?
                    }
                    else {
                        ige = RandomItemFromList<Structure>(c.Structures);
                    }
                    break;

                case EventType.Quest:
                    Debug.LogWarning("Not yet implemented");
                    break;

                case EventType.Other:
                    Debug.LogWarning("Not yet implemented");
                    break;
            }
            return ige;
        }

        private bool RandomIf() {
            timeSinceLastEvent += WorldController.Instance.DeltaTime;
            return UnityEngine.Random.Range(-1, 2) *
                (UnityEngine.Random.Range(-1f, 0.901f + Mathf.Max(0f, Mathf.Exp(0.01f * timeSinceLastEvent / 15f) - 1f))
                + Mathf.Max(0, (Mathf.Exp((1f / 180f) * (timeSinceLastEvent / 15f)) - 2f))) > 1;
        }

        private GameEvent RandomEvent(EventType type) {
            if (typeToEvents.ContainsKey(type) == false)
                return null;
            if (typeToEvents[type].Count == 0)
                return null;
            List<GameEvent> ges = typeToEvents[type];
            //TODO move this to the load -> dic<type,sum>
            float sumOfProbability = 0;
            foreach (GameEvent item in ges) {
                sumOfProbability += item.Probability;
            }
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
            foreach (EventType item in chanceToEvent.Keys) {
                sum += chanceToEvent[item];
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
            foreach (uint id in idToActiveEvent.Keys) {
                string target;
                if(idToActiveEvent[id].target == null) {
                    target = (idToActiveEvent[id].position + " " + idToActiveEvent[id].range);
                } else {
                    target = idToActiveEvent[id].target.ToString();
                }
                list += id + " - " + idToActiveEvent[id].ID + " ("+idToActiveEvent[id].Type+")" + " - " + target
                    + " - " + idToActiveEvent[id].currentDuration + "/" + idToActiveEvent[id].Duration + "\n";
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
            cbEventCreated += create;
            cbEventEnded += ending;
        }

        public static Type TargetToType(Target target) {
            switch (target) {
                case Target.World:
                    return typeof(World);
                case Target.Player:
                    return typeof(Player);
                case Target.Island:
                    return typeof(Island);
                case Target.City:
                    return typeof(City);
                case Target.AllUnit:
                    return typeof(Unit);
                case Target.Ship:
                    return typeof(Ship);
                case Target.LandUnit://default type is unit -- so dunno what todo in this case
                    return typeof(Unit);
                case Target.AllStructure:
                    return typeof(Structure);
                case Target.DamagableStructure://is selected over bool -- so dunno what todo in this case
                    return typeof(Structure);
                case Target.RoadStructure:
                    return typeof(RoadStructure);
                case Target.NeedStructure:
                    return typeof(NeedStructure);
                case Target.MilitaryStructure:
                    return typeof(MilitaryStructure);
                case Target.HomeStructure:
                    return typeof(HomeStructure);
                case Target.ServiceStructure:
                    return typeof(ServiceStructure);
                case Target.GrowableStructure:
                    return typeof(GrowableStructure);
                case Target.OutputStructure:
                    return typeof(OutputStructure);
                case Target.MarketStructure:
                    return typeof(MarketStructure);
                case Target.WarehouseStructure:
                    return typeof(WarehouseStructure);
                case Target.MineStructure:
                    return typeof(MineStructure);
                case Target.FarmStructure:
                    return typeof(FarmStructure);
                case Target.ProductionStructure:
                    return typeof(ProductionStructure);
                default:
                    return null;
            }
        }

        private void OnDestroy() {
            Instance = null;
        }

        public GameEventSave GetSaveGameEventData() {
            GameEventSave ges = new GameEventSave(idToActiveEvent, nextRandomTick);
            return ges;
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