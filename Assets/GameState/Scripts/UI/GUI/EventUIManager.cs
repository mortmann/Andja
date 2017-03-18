using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class EventUIManager : MonoBehaviour {
	public float onScreenTimer = 30f;
	Dictionary<int, GameObject> idToEventGO;
	//Mayber move this to EventManager
	Dictionary<int, float> idToCountTimer;

	public GameObject EventMessagePrefab;
	public Transform contentTransform;

	void Start(){
		idToEventGO = new Dictionary<int, GameObject> ();
		idToCountTimer = new Dictionary<int, float> ();
		foreach (Transform item in contentTransform) {
			GameObject.Destroy (item.gameObject); 
		}
		AddEVENT (1,"TestEvent With a really long Name What is Happening now!?", new Vector2(50,50));	
	}

	public void AddEVENT (int id, string name, Vector2 position) {
		GameObject ego = Instantiate (EventMessagePrefab);
		ego.transform.SetParent (contentTransform);
		ego.GetComponent<EventMessage>().Setup (name,position);
		idToEventGO.Add (id,ego);
		idToCountTimer.Add (id,onScreenTimer);
	}
	
	// Update is called once per frame
	void Update () {
		if(WorldController.Instance.IsPaused){
			return;
		}
		for (int i = idToCountTimer.Count-1; i > 0; i--) {
			idToCountTimer [i] = idToCountTimer [i] - Time.deltaTime;
			if(idToCountTimer[i]<=0){
				idToCountTimer.Remove (i);
			}
		}
	}
}
