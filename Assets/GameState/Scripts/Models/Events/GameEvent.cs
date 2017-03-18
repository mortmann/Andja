using UnityEngine;
using System.Collections.Generic;
using System;
public class GameEvent {
	public EventType EventType { protected set; get; }

	public Influence[] influences { protected set; get; }
	public Dictionary<Type,Influence> targetToInfluence;


	public bool IsDone { get { return currentDuration <= 0;}}
	public bool IsOneTime { get { return maxDuration <= 0;}}
	public int id;
	public string name { get {return EventType.ToString () + " - " + "EMPTY FOR NOW"; }}
	public float probability = 10;
	float minDuration= 50;
	float maxDuration= 100;
	float currentDuration;
	//MAYBE range can also be a little random...?
	//around this as middle? Range+(-1^RandomInt(1,2)*Random(0,(Random(2,3)*Range)/(Range*Random(0.75,1)));
	float Range;
	public Vector2 position;
	// this one says what it is... 
	// so if complete island/city/player or only a single structuretype is the goal
	// can be null if its not set to which type
	public IGEventable target;  //TODO make a check for it!

	public GameEvent(){
		
	}
	public GameEvent(GameEvent ge){
		this.influences = ge.influences;
		this.maxDuration = ge.maxDuration;
		this.minDuration = ge.minDuration;
		this.Range = ge.Range;
	}
	public GameEvent Clone(){
		return new GameEvent (this);
	}
	public void StartEvent(Vector2 pos){
		position = pos;
		currentDuration = weightedRandomDuration ();
	}
	public void Update(float delta){
		if(currentDuration<=0){
			Debug.LogWarning ("This Event is over, but still being updated (active)!");
		}
		currentDuration -= delta;
	}
	/// <summary>
	/// Weights around the middle of the range higher.
	/// SO its more likely to have the (max-min)/2 than max|min
	/// </summary>
	/// <returns>The random.</returns>
	/// <param name="numDice">Number dice.</param>
	float weightedRandomDuration(int numDice=5) {
		float num = 0;
		for (var i = 0; i < numDice; i++) {
			num += UnityEngine.Random.Range (0,1.1f) * ((maxDuration-minDuration)/numDice);
		}    
		num+=minDuration;
		return num;
	}
	public bool HasWorldEffect(){
		foreach (Influence item in influences) {
			if(item.InfluenceTyp==InfluenceTyp.Building){
				return true;
			}
		}
		return false;
	}
	public bool IsTarget(IGEventable t){
		if(target!=null){
			if(target is Player){
				if(target.GetPlayerNumber () == t.GetPlayerNumber ())
					return IsTarget (t.GetType ());
			}
		}
		return IsTarget (t.GetType ());
	}
	bool IsTarget(Type t){
		if(targetToInfluence.ContainsKey (t)==false){
			Debug.LogError ("target was not in influences! why?");
			return false;
		}
		return targetToInfluence.ContainsKey (t);
	}
	public IGEventable GetTarget(Type t){
		return targetToInfluence [t].target;
	}
	public Influence GetInfluenceForTarget(IGEventable t){
		if(targetToInfluence.ContainsKey (t.GetType ())==false){
			Debug.LogError ("target was not in influences! why?");
			return null;
		}
		return targetToInfluence [t.GetType ()];
	}

	public class Influence {
		public InfluenceTyp InfluenceTyp { protected set; get; }
		public InfluenceRange InfluenceRange { protected set; get; }
		public float amount;
		public IGEventable target;
		/// <summary>
		/// Object is the influencetyp that is of type of the targe
		/// </summary>
		public Action<GameEvent,bool,object> Function;
		public Influence( InfluenceTyp it, InfluenceRange ir, float amount, IGEventable t){
			this.InfluenceTyp = it;
			this.InfluenceRange = ir;
			this.amount = amount;
			this.target = t;
		}
	}

	//TODO save the events 
	//TODO LOAD THESE ALSO 
	//DONT KNOW WHERE AND HOW EXACTLY THOUGH
}
