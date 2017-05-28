﻿using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
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
			x +=(UnityEngine.Random.Range(0,2) * 500 + CalcParabolaVertex (500, 750, UnityEngine.Random.Range (0, 500)));
			y +=(UnityEngine.Random.Range(0,2) * 500 + CalcParabolaVertex (500, 750, UnityEngine.Random.Range (0, 500)));
		}
		x /= s;
		y /= s;
		Debug.Log (x + " x  y " +y);


	}
	void Start() {
//		var a = CreateReusableAction<GameEvent,bool,Structure> ("OutputStructure_Efficiency");
		world = World.current;
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
			foreach (Island item in world.islandList) {
				cities.AddRange (item.myCities);
			}
			ige = RandomItemFromList<City>(cities);
			break;
		case EventType.Disaster:
			ige = RandomItemFromList<Island>(world.islandList);
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

			Island i = RandomItemFromList<Island> (world.islandList);
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

	public float CalcParabolaVertex(float size,float dist, float randomX) {
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
		vec.x =(UnityEngine.Random.Range(0,2) * 500 + CalcParabolaVertex (500, 750, UnityEngine.Random.Range (0, 500)));
		vec.y =(UnityEngine.Random.Range(0,2) * 500 + CalcParabolaVertex (500, 750, UnityEngine.Random.Range (0, 500)));
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


	public string GetSaveGameEventData(){
		XmlSerializer serializer = new XmlSerializer( typeof(GameEventSave) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer,new GameEventSave(idToActiveEvent));
		writer.Close();
		// Create/overwrite the save file with the xml text.
		return writer.ToString();
	}
	public void LoadGameEventData(string data){

		XmlSerializer serializer = new XmlSerializer( typeof(GameEventSave) );
		TextReader reader = new StringReader( data );
		GameEventSave gcs = (GameEventSave)serializer.Deserialize(reader);
		reader.Close();
		idToActiveEvent = gcs.idToActiveEvent;
	}

	public class GameEventSave : IXmlSerializable {
		public Dictionary<uint,GameEvent> idToActiveEvent;
		public GameEventSave(Dictionary<uint,GameEvent> idToActiveEvent ){
			this.idToActiveEvent = idToActiveEvent;
		}
		public GameEventSave(){

		}
		#region save
		public XmlSchema GetSchema() {
			return null;
		}

		public void WriteXml(XmlWriter writer) {
			writer.WriteElementString ("Random" , JsonUtility.ToJson(UnityEngine.Random.state));
			foreach(uint p in idToActiveEvent.Keys){
				writer.WriteStartElement ("GameEvent");
				writer.WriteAttributeString ("ID", p + "");
				idToActiveEvent[p].WriteXml (writer);
				writer.WriteEndElement ();
			}

		}

		public void ReadXml(XmlReader reader) {
			UnityEngine.Random.state = JsonUtility.FromJson<UnityEngine.Random.State>(reader.ReadElementString ("Random"));
			if(reader.ReadToDescendant("GameEvent") ) {
				do {
					if(reader.IsStartElement ("GameEvent")==false){
						if(reader.Name == "GameEventSave"){
							return;
						}
						continue;
					}	
					uint id = uint.Parse( reader.GetAttribute("ID") );
					GameEvent ge = new GameEvent();
					//load the functions to it etc
					int tt = int.Parse( reader.GetAttribute("TargetType") );
					IGEventable target = null; 
					switch(tt){
					case World.TargetType:
						target = World.current;
						break;
					case City.TargetType:
						string tile = reader.GetAttribute("Island");
						Vector2 vec2 = Tile.ToStringToTileVector(tile);
						Island i = World.current.GetTileAt(vec2.x,vec2.y).myIsland;
						int player = int.Parse( reader.GetAttribute("PlayerNumber") );
						target = i.FindCityByPlayer(player);
						break;
					case Player.TargetType:
						int pnum = int.Parse( reader.GetAttribute("PlayerNumber") );
						PlayerController.Instance.GetPlayer(pnum);
						break;
					case Island.TargetType:
						string tileTemp = reader.GetAttribute("Island");
						Vector2 tileVec = Tile.ToStringToTileVector(tileTemp);
						target = World.current.GetTileAt(tileVec.x,tileVec.y).myIsland;
						break;
					case -1:
						//This case is the null case!
						//if the target should be null the TargetType will be -1
						break;
					default: // Structure
						if(tt<Structure.TargetType){
							Debug.LogError("TargetType " + tt + " does not meet any programmed in!");
							return;
						}
						uint strBuildID = uint.Parse( reader.GetAttribute("BuildID") );
						string tileString = reader.GetAttribute("Island");
						Vector2 vec2Tile = Tile.ToStringToTileVector(tileString);
						if(World.current.GetTileAt(vec2Tile.x,vec2Tile.y).Structure.buildID != strBuildID){
							Debug.Log ("BuildID doesnt match up.");
							continue;
						}
						target = World.current.GetTileAt(vec2Tile.x,vec2Tile.y).Structure;
						break;
					}
					ge.target = target;
					idToActiveEvent.Add(id,ge);
				} while( reader.Read () );
			}

		}
		#endregion
	}
}