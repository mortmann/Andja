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
	float ambientSourceMaxVolume;
	public AudioSource uiSource;
	public AudioSource soundEffectSource;

	public AudioClip placeBuildingSound;
	public AudioClip cityCreateSound;

	CameraController cameraController;
	StructureSpriteController ssc;
	WorkerSpriteController wsc;
	UnitSpriteController usc;

	public static string MusicLocation = "Audio/Music/";
	public static string SoundEffectLocation = "Audio/Game/SoundEffects/";
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
		ambientSourceMaxVolume = ambientSource.volume;
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

		UpdateAmbient ();
		UpdateSoundEffects ();
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
//			audio.volume = soundEffectSource.volume;
//			audio.outputAudioMixerGroup = soundEffectSource.outputAudioMixerGroup;
//			audio.spread = soundEffectSource.spread;
//			audio.maxDistance = soundEffectSource.maxDistance;
//			audio.minDistance = soundEffectSource.minDistance;
//			audio.dopplerLevel = soundEffectSource.dopplerLevel;
//			audio.rolloffMode= soundEffectSource.rolloffMode;
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
		//Maybe make diffrent sound when diffrent buildingtyps are placed
		uiSource.clip = placeBuildingSound;
		uiSource.Play ();
	}
	public void OnCityCreate(City c){
		//diffrent sounds for diffrent locations of City? North,middle,South?
		uiSource.clip = cityCreateSound;
		uiSource.Play ();
	}
	public void UpdateAmbient(){
		if(cameraController.nearestIsland!=null){
			switch (cameraController.nearestIsland.myClimate){
			case Climate.Cold:
				currentAmbient = AmbientSound.North;
				break;
			case Climate.Middle:
				currentAmbient = AmbientSound.Middle;
				break;
			case Climate.Warm:
				currentAmbient = AmbientSound.South;
				break;
			}
		} else {
			currentAmbient = AmbientSound.Water;
		}
		switch (currentAmbient) {
		case AmbientSound.Water:
			break;
		case AmbientSound.North:
			break;
		case AmbientSound.Middle:
			break;
		case AmbientSound.South:
			break;
		}
		//TODO make this like it should be :D, better make it sound nice 
		//TODO make if you get further away from the ground wind is gonna get louder
		ambientSource.volume = ambientSourceMaxVolume - cameraController.zoomLevel / ambientSourceMaxVolume;
	}
}