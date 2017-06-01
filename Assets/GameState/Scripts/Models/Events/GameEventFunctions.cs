using UnityEngine;
using System.Collections;


/// <summary>
/// Game event functions.
/// ALL FUNCTIONS should follow these RULES:
/// -must handle both start and end of the event
/// -must follow attributes GameEvent, bool(if its start or ending), AND what it effects
/// -must be as fast as possible
/// -must be able to reverse the effect even when other effects are active
/// </summary>
public class GameEventFunctions {
	

	public static void Test(Structure str){
		Debug.Log ("Test"); 
	}
	public static void Test2(Structure str, bool test){
		Debug.Log ("Test " + test); 
	}
	public static void OutputStructure_Efficiency(GameEvent ge,bool start,Structure str){
		Debug.Log ("Test "+ge+" "+start+" "+str); 
		if(str is OutputStructure == false){
			return;
		}
//		OutputStructure o = (OutputStructure)str;
//		o.efficiencyModifier = 

	}

}
