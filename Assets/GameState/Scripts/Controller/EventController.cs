using UnityEngine;
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
	private ulong lastID = 0;
	Dictionary<EventType,GameEvent[]> typeToEvents;
	Dictionary<ulong,GameEvent> idToActiveEvent;
	//Every EventType has a chance to happen
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
		var a = CreateReusableAction<GameEvent,bool,Structure> ("OutputStructure_Efficiency");
		a (new GameEvent (),true,new MineStructure ());
		idToActiveEvent = new Dictionary<ulong, GameEvent> ();
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
		for (ulong i = (ulong)idToActiveEvent.Count-1; i > 0; i--) {
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
		//now find random the target of the GameEvent
		switch(type){
		case EventType.City:
			break;
		case EventType.Disaster:
			break;
		case EventType.Weather:
			break;
		case EventType.Production:
			break;
		case EventType.Quest:
			Debug.LogWarning ("Not yet implemented");
			break;
		case EventType.Other:
			Debug.LogWarning ("Not yet implemented");
			break;
		}
		cbEventCreated(ge);

		idToActiveEvent.Add (lastID,ge);
		lastID++;
		timeSinceLastEvent = 0;

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
		public Dictionary<ulong,GameEvent> idToActiveEvent;
		public GameEventSave(Dictionary<ulong,GameEvent> idToActiveEvent ){
			this.idToActiveEvent = idToActiveEvent;
		}
		public GameEventSave(){

		}
		#region save
		public XmlSchema GetSchema() {
			return null;
		}

		public void WriteXml(XmlWriter writer) {

			foreach(ulong p in idToActiveEvent.Keys){
				writer.WriteStartElement ("GameEvent");
				writer.WriteAttributeString ("ID", p + "");
				idToActiveEvent[p].WriteXml (writer);
				writer.WriteEndElement ();
			}

		}

		public void ReadXml(XmlReader reader) {
			if(reader.ReadToDescendant("GameEvent") ) {
				do {
					if(reader.IsStartElement ("GameEvent")==false){
						if(reader.Name == "GameEventSave"){
							return;
						}
						continue;
					}	
					ulong id = ulong.Parse( reader.GetAttribute("ID") );
					GameEvent ge = new GameEvent();
					//load the functions to it etc
					int tt = int.Parse( reader.GetAttribute("TargetType") );
					IGEventable target; 
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
					default: // Structure
						if(tt<Structure.TargetType){
							Debug.LogError("TargetType " + tt + " does not meet any programmed in!");
							return;
						}
						int strBuildID = int.Parse( reader.GetAttribute("BuildID") );
						string tileString = reader.GetAttribute("Island");
						Vector2 vec2Tile = Tile.ToStringToTileVector(tileString);
						if(World.current.GetTileAt(vec2.x,vec2.y).Structure.buildID != strBuildID){
							Debug.Log ("BuildID doesnt match up.");
							continue;
						}
						target = World.current.GetTileAt(vec2Tile.x,vec2Tile.y).Structure;
						break;
					}
					idToActiveEvent.Add(id,ge);
				} while( reader.Read () );
			}

		}
		#endregion
	}
}
