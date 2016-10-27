using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

public class SoundController : MonoBehaviour {
	public AudioMixer mixer;	
	public static SoundController Instance;


	// Use this for initialization
	void Start () {
		if(Instance!=null){
			Debug.LogError("Only 1 Instance should be created.");
			return;
		}
		Instance = this;
	

	}
	
	// Update is called once per frame
	void Update () {
	
	}
		


}