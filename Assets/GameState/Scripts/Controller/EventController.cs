using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

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
public enum EventType {Weather, City, Production, Quest,  Disaster, Other }
public enum InfluenceRange {World, Island, City, Range, Player }
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

	Dictionary<EventType,GameEvent[]> typeToEvents;
	Dictionary<int,GameEvent> idToActiveEvent;
	//Every EventType has a 
	Dictionary<EventType,float> chanceToEvent;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;

	float timeSinceLastEvent=0;

	// Use this for initialization
	void Awake () {
		if (Instance != null) {
			Debug.LogError("There should never be two event controllers.");
		}
		Instance = this;
		GameEventFunctions g = new GameEventFunctions ();
		var a = CreateReusableAction<GameEvent,bool,Structure> ("OutputStructure_Efficiency");
		a (new GameEvent (),true,new MineStructure ());
		idToActiveEvent = new Dictionary<int, GameEvent> ();
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
		for (int i = idToActiveEvent.Count-1; i > 0; i--) {
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

		//TODO Randomize which event it is better
		EventType type = RandomType ();

		GameEvent ge = RandomEvent (type);
//		idToActiveEvent.Add (,ge);
		cbEventCreated(ge);
		timeSinceLastEvent = 0;
//		switch(type){
//		case EventType.City:
//			break;
//		case EventType.Disaster:
//			break;
//		case EventType.Weather:
//			break;
//		case EventType.Production:
//			break;
//		case EventType.Quest:
//			Debug.LogWarning ("Not yet implemented");
//			break;
//		case EventType.Other:
//			Debug.LogWarning ("Not yet implemented");
//			break;
//		}
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

}
