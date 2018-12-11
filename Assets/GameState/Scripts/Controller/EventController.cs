using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;

//TODO:
//-We need a UI displaying most(if not all) Events -> left side on the screen?
//-We need a random generator that decides what happens
// -> for that we need to decide how frequently what happens
// -> on the Constant Data Object are the diffrent kinds of catastrophes that can happen in this >round<
// -> depends on player-city advances in buildings can have effect on what can happen -> should it check it or city itself
//-decide how to implement the diffrent kinds of effects on the structures/units
// -> how to stop production
// -> how to increase/lower productivity
// -> how to make units faster/slower depending where they are?
//-is this also for quests? Will there be even some? If so there must be a ton to keep them from repetitive


/// <summary>
/// Event type. Specify which kind of Event happens.
/// TODO: TO DECIDE IF QUEST ARE HANDLED HERE
/// </summary>
public enum EventType {Weather, City, Structure, Quest,  Disaster, Other }
public enum InfluenceRange {World, Island, City, Structure, Range, Player } 
public enum InfluenceTyp {Building, Unit}

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

    private uint lastID = 0;
	Dictionary<EventType,GameEvent[]> typeToEvents;
	Dictionary<uint,GameEvent> idToActiveEvent;
	//Every EventType has a chance to happen
	Dictionary<EventType,float> chanceToEvent;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;

	float timeSinceLastEvent=0;
	World world;
	// Use this for initialization
	void Awake () {
		if (Instance != null) {
			Debug.LogError ("There should never be two event controllers.");
		}
		Instance = this;
		float x = 0;
		float y = 0;
		float s = 10;
		for (int i = 0; i < s; i++) {
			x +=(UnityEngine.Random.Range(0,2) * 500 + GetWeightedYFromRandomX (500, 750, UnityEngine.Random.Range (0, 500)));
			y +=(UnityEngine.Random.Range(0,2) * 500 + GetWeightedYFromRandomX (500, 750, UnityEngine.Random.Range (0, 500)));
		}
		x /= s;
		y /= s;
	}
	void Start() {
//		var a = CreateReusableAction<GameEvent,bool,Structure> ("OutputStructure_Efficiency");
		world = World.Current;
		idToActiveEvent = new Dictionary<uint, GameEvent> ();
		chanceToEvent = new Dictionary<EventType, float> ();
		typeToEvents = new Dictionary<EventType, GameEvent[]> ();
	}


	// Update is called once per frame
	// Handle here Events that will effect whole islands or
	// World-Segments, maybe fire and similars
	void Update () {
		if(WorldController.Instance.IsPaused){
			return;
		}
		//update and remove inactive events
		List<uint> ids = new List<uint>(idToActiveEvent.Keys);
		foreach (uint i in ids) {
			Debug.Log ("update " +i); 
			idToActiveEvent [i].Update (WorldController.Instance.DeltaTime);
			if(idToActiveEvent [i].IsDone){
				cbEventEnded (idToActiveEvent [i]);
				idToActiveEvent.Remove (i);
			}
		}
		//Now will there be an event or not?
		if(RandomIf()==false){
			return;
		}
		CreateRandomEvent ();
		timeSinceLastEvent = 0;

	} 

	public void CreateRandomEvent(){
		//TODO Randomize which event it is better
		CreateRandomTypeEvent (RandomType ());
	}
	public void CreateRandomTypeEvent(EventType type){
		//now find random the target of the GameEvent
		CreateGameEvent( RandomEvent (type) );
	}

    internal void SetGameEventData(GameEventSave ges) {
        idToActiveEvent = ges.idToActiveEvent;
        UnityEngine.Random.state = ges.Random;
    }

    public void CreateGameEvent(GameEvent ge){
		//fill the type
		cbEventCreated(ge);

		idToActiveEvent.Add (lastID,ge);
		ge.StartEvent (Vector2.zero);
		lastID++;

	}
	IGEventable GetEventTargetForEventType(EventType type){
		IGEventable ige = null;
		//some times should be target all cities...
		//idk how todo do it tho...


		switch(type){
		case EventType.City:
			List<City> cities = new List<City> ();
			foreach (Island item in world.IslandList) {
				cities.AddRange (item.myCities);
			}
			ige = RandomItemFromList<City>(cities);
			break;
		case EventType.Disaster:
			ige = RandomItemFromList<Island>(world.IslandList);
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

			float r = UnityEngine.Random.Range (0f, 1f);
			if(r<0.4f){ //idk
				return null; // there is no specific target
			}

			Island i = RandomItemFromList<Island> (world.IslandList);
			City c = RandomItemFromList<City> (i.myCities);
			if(c.playerNumber == -1){ // random decided there will be no event? 
				// are there events for wilderniss structures?
			} else {
				ige = RandomItemFromList<Structure> (c.myStructures);
			}
			break;
		case EventType.Quest:
			Debug.LogWarning ("Not yet implemented");
			break;
		case EventType.Other:
			Debug.LogWarning ("Not yet implemented");
			break;
		}
		return ige;
	}
	bool RandomIf(){
		timeSinceLastEvent += WorldController.Instance.DeltaTime;
		return UnityEngine.Random.Range (0.2f, 1.5f) *(Mathf.Exp ((1/180)*timeSinceLastEvent)-1) > 1;
	}

	GameEvent RandomEvent(EventType type){
		GameEvent[] ges = typeToEvents [type];
		//TODO move this to the load -> dic<type,sum>
		float sumOfProbability=0;
		foreach (GameEvent item in ges) {
			sumOfProbability += item.probability;
		}
		float randomNumber = UnityEngine.Random.Range (0, sumOfProbability);
		float sum=0;
		foreach (GameEvent item in ges) {
			sum += item.probability;
			if (sum <= randomNumber){
				return item.Clone ();
			}
		}
		Debug.LogError ("No event found for this!"); 
		return null;
	}
	EventType RandomType(){
		float r = UnityEngine.Random.Range (0f, 1f);
		float sum = 0;
		foreach (EventType item in chanceToEvent.Keys) {
			sum += chanceToEvent[item];
			if(sum>=r){
				return item;
			}
		}
		Debug.LogError ("No type found for this event!"); 
		return EventType.City;
	}

	T RandomItemFromList<T>(List<T> iges){
		return iges [UnityEngine.Random.Range (0, iges.Count - 1)];
	}

	public float GetWeightedYFromRandomX(float size,float dist, float randomX) {
		float x1 = 0f; 
		float y1 = 0f; 
		float x2 = dist;
		float y2 = size; 
		float x3 = dist * 2; 
		float y3 = 0f;
		float denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
		float A     = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
		float B     = (x3*x3 * (y1 - y2) + x2*x2 * (y3 - y1) + x1*x1 * (y2 - y3)) / denom;
		float C     = (x2 * x3 * (x2 - x3) * y1 + x3 * x1 * (x3 - x1) * y2 + x1 * x2 * (x1 - x2) * y3) / denom;
//		Debug.Log (A+"x^2 + "+ B +"x +"+C);
		return A * Mathf.Pow (randomX, 2) + B * randomX + C;
	}
	public Vector2 GetRandomVector2(){
		Vector2 vec = new Vector2 ();
		float w = (float) World.Current.Width / 2f;
		float h = (float) World.Current.Height / 2f;

		vec.x =(UnityEngine.Random.Range(0,2) * w + GetWeightedYFromRandomX (w, w * 1.25f, UnityEngine.Random.Range (0, w)));
		vec.y =(UnityEngine.Random.Range(0,2) * h + GetWeightedYFromRandomX (h, h * 1.25f, UnityEngine.Random.Range (0, h)));
		return vec;
	}
	public static Action<TParam1> CreateReusableAction<TParam1>(string methodName) {
		var method = typeof(GameEventFunctions).GetMethod(methodName);
		var del = Delegate.CreateDelegate(typeof(Action<TParam1>), method);
		Action<TParam1> caller = (instance) => del.DynamicInvoke(instance);
		return caller;
	}
	public static Action<object, object> CreateReusableAction<TParam1, TParam2>(string methodName){
		var method = typeof(GameEventFunctions).GetMethod(methodName, new Type[] { typeof(TParam1),typeof(TParam2) });
		var del = Delegate.CreateDelegate(typeof(Action<TParam1, TParam2>), method);
		Action<object, object> caller = (instance, param) => del.DynamicInvoke(instance, param);
		return caller;
	}
	public static Action<object, object, object> CreateReusableAction<TParam1, TParam2, TParam3>(string methodName){
		var method = typeof(GameEventFunctions).GetMethod(methodName, new Type[] { typeof(TParam1),typeof(TParam2),typeof(TParam3)   });
		var del = Delegate.CreateDelegate(typeof(Action<TParam1, TParam2, TParam3>), method);
		Action<object, object, object> caller = (param1,param2,param3) => del.DynamicInvoke(param1,param2,param3);
		return caller;
	}

	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		cbEventCreated += create;
		cbEventEnded += ending;
	}
    void OnDestroy() {
        Instance = null;
    }

    public GameEventSave GetSaveGameEventData(){
		GameEventSave ges = new GameEventSave (idToActiveEvent);
		return ges;
	}
}

public class GameEventSave : BaseSaveData {
	public Dictionary<uint,GameEvent> idToActiveEvent;
	public UnityEngine.Random.State Random;
	public GameEventSave(Dictionary<uint,GameEvent> idToActiveEvent ){
		this.idToActiveEvent = idToActiveEvent;
	}
	public GameEventSave(){

	}

}