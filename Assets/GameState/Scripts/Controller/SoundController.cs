using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.IO;

enum AmbientSound { Water, North, Middle, South }
// TODO: 
//		-Make sounds for tiles on screen possible eg. beach, river.
//		-sounds for units
public class SoundController : MonoBehaviour {
	public static SoundController Instance;
	public AudioSource musicSource;
	public AudioSource ambientSource;
	public AudioSource windAmbientSource;

	public AudioSource uiSource;
	public AudioSource soundEffectSource;
	public GameObject soundEffect2DGO;
	List<AudioSource> playedAudios;



	public AudioClip placeBuildingSound;
	public AudioClip cityCreateSound;

	CameraController cameraController;
	StructureSpriteController ssc;
	WorkerSpriteController wsc;
	UnitSpriteController usc;

	public static string MusicLocation = "Audio/Music/";
	public static string SoundEffectLocation = "Audio/Game/SoundEffects/";
	public static string AmbientLocation = "Audio/Game/Ambient/";

	AmbientSound currentAmbient;

	// Use this for initialization
	void Start () {
		if(Instance!=null){
			Debug.LogError("Only 1 Instance should be created.");
			return;
		}
		Instance = this;
		cameraController = GameObject.FindObjectOfType<CameraController> ();
		BuildController.Instance.RegisterStructureCreated (OnBuild);
		BuildController.Instance.RegisterCityCreated (OnCityCreate);
		EventController.Instance.RegisterOnEvent (OnEventStart,OnEventEnd);
		playedAudios = new List<AudioSource> ();
		windAmbientSource.loop = true;
		windAmbientSource.clip = Resources.Load (AmbientLocation + "wind-1") as AudioClip;
		windAmbientSource.Play ();
		ssc = FindObjectOfType<StructureSpriteController> ();
		wsc = FindObjectOfType<WorkerSpriteController> ();
		usc = FindObjectOfType<UnitSpriteController> ();
	}
	
	// Update is called once per frame
	void Update () {
		if(musicSource.isPlaying==false){
			musicSource.PlayOneShot (GetMusicAudioClip());
		}
		if(WorldController.Instance.IsPaused){
			return;
		}
		ambientSource.volume = Mathf.Clamp  ((CameraController.maxZoomLevel-cameraController.zoomLevel) / CameraController.maxZoomLevel,0,1f);
		windAmbientSource.volume = Mathf.Clamp (1 - ambientSource.volume-0.7f,0.03f,0.15f);
		UpdateAmbient ();
		UpdateSoundEffects ();
		for (int i = playedAudios.Count-1; i >= 0; i--) {
			if(playedAudios[i].isPlaying == false){
				Destroy (playedAudios[i].gameObject);
				playedAudios.RemoveAt (i);
			}
		} 

	}

	void UpdateSoundEffects (){
		//We add to any gameobject that has been created from
		//the StructurespriteController AND its in the list provided
		//by the CameraController an audiosource so that it 
		//can play a sound if it needs too. 
		foreach (Structure item in cameraController.structureCurrentInCameraView) {
			GameObject go = ssc.GetGameObject (item);
			if(go == null || go.GetComponent<AudioSource> ()!=null){
				continue;
			}
			ADDCopiedAudioSource (go, soundEffectSource);
			item.RegisterOnSoundCallback (PlaySoundEffectStructure);
		}
		foreach (Worker item in wsc.workerToGO.Keys) {
			GameObject go = wsc.workerToGO [item];
			if(go == null || go.GetComponent<AudioSource> ()!=null){
				continue;
			}
			ADDCopiedAudioSource (go, soundEffectSource);
			item.RegisterOnSoundCallback (PlaySoundEffectWorker);
		}
		foreach (Unit item in usc.unitGameObjectMap.Keys) {
			GameObject go = usc.unitGameObjectMap [item];
			if(go == null || go.GetComponent<AudioSource> ()!=null){
				continue;
			}
			ADDCopiedAudioSource (go, soundEffectSource);
			item.RegisterOnSoundCallback (PlaySoundEffectUnit);
		}
	}
	private void ADDCopiedAudioSource(GameObject goal,AudioSource copied){
		if(goal.GetComponent<AudioSource> ()!=null){
			return;
		}
		AudioSource audio = goal.AddComponent<AudioSource>();
		// Copied fields can be restricted with BindingFlags
		System.Reflection.FieldInfo[] fields = audio.GetType ().GetFields(); 
		foreach (System.Reflection.FieldInfo field in fields)
		{
			field.SetValue(audio, field.GetValue(copied));
		}
	}

	public void PlaySoundEffectStructure(Structure str, string filePath ){
		GameObject go = ssc.GetGameObject (str);
		if(go == null){
			str.UnregisterOnSoundCallback (PlaySoundEffectStructure);
			return;
		}
		AudioSource goal = go.GetComponent<AudioSource> ();
		if(goal==null){
			str.UnregisterOnSoundCallback (PlaySoundEffectStructure);
			return;
		}
		AudioClip ac = Resources.Load(SoundEffectLocation+filePath) as AudioClip;
		ac.LoadAudioData ();
		goal.PlayOneShot (ac);
	}
	public void PlaySoundEffectWorker(Worker worker, string filePath ){
		GameObject go = wsc.workerToGO [worker];
		if(go == null){
			worker.UnregisterOnSoundCallback (PlaySoundEffectWorker);
			return;
		}
		AudioSource goal = go.GetComponent<AudioSource> ();
		if(goal==null){
			worker.UnregisterOnSoundCallback (PlaySoundEffectWorker);
			return;
		}
		AudioClip ac = Resources.Load(SoundEffectLocation+filePath) as AudioClip;
		ac.LoadAudioData ();
		goal.PlayOneShot (ac);
	}
	public void PlaySoundEffectUnit(Unit unit, string filePath ){
		GameObject go = usc.unitGameObjectMap [unit];
		if(go == null){
			unit.UnregisterOnSoundCallback (PlaySoundEffectUnit);
			return;
		}
		AudioSource goal = go.GetComponent<AudioSource> ();
		if(goal==null){
			unit.UnregisterOnSoundCallback (PlaySoundEffectUnit);
			return;
		}
		AudioClip ac = Resources.Load(SoundEffectLocation+filePath) as AudioClip;
		ac.LoadAudioData ();
		goal.PlayOneShot (ac);
	}
	AudioClip GetMusicAudioClip(){
		//This function has to decide which Music is to play atm
		//this means if the player is a war or is in combat play
		//Diffrent musicclips then if he is building/at peace.
		//also when there is a disaster it should play smth diffrent
		//TODO CHANGE THIS-
		//for now it will choose a song random from the music folder
		//maybe add a User addable song loader into this
		AudioClip ac = Resources.Load(MusicLocation+"idle-1") as AudioClip;
		ac.LoadAudioData ();
		return ac;
	}

	public void OnBuild(Structure str){
		if(str.playerNumber!=PlayerController.Instance.currentPlayerNumber){
			return;
		}
		string name =  "BuildSound_" + Time.frameCount;
		if(GameObject.Find (name) != null){
			return;
		}
		//Maybe make diffrent sound when diffrent buildingtyps are placed
		GameObject g = Instantiate (soundEffect2DGO);
		g.name = name;
		g.transform.SetParent (soundEffect2DGO.transform);
		AudioSource ac = g.GetComponent<AudioSource> ();
		ac.clip = placeBuildingSound;
		ac.volume = 0.75f;
		ac.Play ();
		playedAudios.Add (ac);
	}
	public void OnCityCreate(City c){
		if(c.playerNumber!=PlayerController.Instance.currentPlayerNumber){
			return;
		}
		//diffrent sounds for diffrent locations of City? North,middle,South?
		GameObject g = Instantiate (soundEffect2DGO);
		g.transform.SetParent (soundEffect2DGO.transform);
		AudioSource ac = g.GetComponent<AudioSource> ();
		ac.clip = cityCreateSound;
		ac.volume = 0.75f;
		ac.Play ();
		playedAudios.Add (ac);
	}
	public void UpdateAmbient(){
		AmbientSound ambient = AmbientSound.Water;
		if(cameraController.nearestIsland!=null){
			switch (cameraController.nearestIsland.myClimate){
			case Climate.Cold:
				ambient = AmbientSound.North;
				break;
			case Climate.Middle:
				ambient = AmbientSound.Middle;
				break;
			case Climate.Warm:
				ambient = AmbientSound.South;
				break;
			}
		} else {
			ambient = AmbientSound.Water;
		}
		//If its the same no need to change the music
		if(ambient==currentAmbient){
			return;
		}
		currentAmbient = ambient;
		string soundFileName="";
		switch (currentAmbient) {
		case AmbientSound.Water:
			soundFileName="water-medium-";
			break;
		case AmbientSound.North:
			soundFileName="north-";
			break;
		case AmbientSound.Middle:
			soundFileName="middle-";
			break;
		case AmbientSound.South:
			soundFileName="south-";
			break;
		}
		//find out which one exact- number for now only 1
		soundFileName+="1";
		AudioClip ac = Resources.Load(AmbientLocation+soundFileName) as AudioClip;
		ac.LoadAudioData ();
		ambientSource.clip = ac;
		ambientSource.Play ();
		//TODO make this like it should be :D, better make it sound nice 

	}
		
	//TODO implement a way of playing these sounds
	public void OnEventStart(GameEvent ge){
		Debug.LogWarning ("Not implemented yet!");
	}
	public void OnEventEnd(GameEvent ge){
		//Maybe never used?
	}

}