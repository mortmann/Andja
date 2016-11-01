using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;
public class MenuAudioManager : MonoBehaviour {
	static string fileName="volume.ini";
    public AudioMixer mixer;	
    public static MenuAudioManager instance;

    public AudioClip hoverSound;
    public AudioClip sliderSound;
    public AudioClip clickSound;
	private int masterVolume;
	private int musicVolume;
	private int ambientVolume;
	private int soundVolume;
	private int uiVolume;

    void Awake() {
        instance = this;
		ReadSoundVolumes ();
    }

    public void PlayClickSound() {
        GetComponent<AudioSource>().clip = clickSound;
        GetComponent<AudioSource>().Play();
    }
    public void PlayHoverSound() {
        GetComponent<AudioSource>().clip = hoverSound;
        GetComponent<AudioSource>().Play();
    }
    public void PlaySliderSound() {
        GetComponent<AudioSource>().clip = sliderSound;
        GetComponent<AudioSource>().Play();
    }

    public void SetMusicVolume(float value) {
        mixer.SetFloat("MusicVolume", ConvertToDecibel(value));
		musicVolume = Mathf.RoundToInt (value);
		SaveVolumetSchema ();
    }
    public void SetSoundEffectsVolume(float value) {
        mixer.SetFloat("SoundEffectsVolume", ConvertToDecibel(value));
		soundVolume = Mathf.RoundToInt (value);
    }
    public void SetMasterVolume(float value) {
        mixer.SetFloat("MasterVolume", ConvertToDecibel(value));
		masterVolume = Mathf.RoundToInt (value);
    }
    public void SetAmbientVolume(float value) {
        mixer.SetFloat("AmbientVolume", ConvertToDecibel(value));
		ambientVolume = Mathf.RoundToInt (value);
    }
    public void SetUIVolume(float value) {
		mixer.SetFloat ("UIVolume", ConvertToDecibel (value));
		uiVolume = Mathf.RoundToInt (value);
    }

	public float getVolumeFor(VolumeType volType){
		switch (volType) {
		case VolumeType.Master:
			return masterVolume;
		case VolumeType.SoundEffect:
			return soundVolume;
		case VolumeType.Ambient:
			return ambientVolume;
		case VolumeType.Music:
			return musicVolume;
		case VolumeType.UI:
			return uiVolume;
		default:
			Debug.LogError ("Unknown VolumeType");
			return 0;
		}
	}

    /**
     * Convert the value coming from our sliders to a decibel value we can
     * feed into the audio mixer.
     */
    public float ConvertToDecibel(float value) {
        // Log(0) is undefined so we just set it by default to -80 decibels
        // which is 0 volume in the audio mixer.
        float decibel = -80f;

        // I think the correct formula is Mathf.Log10(value / 100f) * 20f.
        // Using that yields -6dB at 50% on the slider which is I think is half
        // volume, but I don't feel like it sounds like half volume. :p And I also
        // felt this homemade formula sounds more natural/linear when you go towards 0.
        // Note: We use 0-100 for our volume sliders in the menu, hence the
        // divide by 100 in the equation. If you use 0-1 instead you would remove that.
        if (value > 0) {
            decibel = Mathf.Log(value / 100f) * 17f;
        }

        return decibel;
    }
	public void SaveVolumetSchema(){
		string path = Application.dataPath.Replace ("/Assets", "");
		if( Directory.Exists(path ) == false ) {
			// NOTE: This can throw an exception if we can't create the folder,
			// but why would this ever happen? We should, by definition, have the ability
			// to write to our persistent data folder unless something is REALLY broken
			// with the computer/device we're running on.
			Directory.CreateDirectory( path  );
		}
		string filePath = System.IO.Path.Combine(path,fileName) ;
		StringWriter writer = new StringWriter ();
		writer.WriteLine ("masterVolume  = "+masterVolume);
		writer.WriteLine ("soundVolume   = "+soundVolume);
		writer.WriteLine ("ambientVolume = "+ambientVolume);
		writer.WriteLine ("uiVolume      = "+uiVolume);
		writer.WriteLine ("musicVolume   = "+musicVolume);
		File.WriteAllText( filePath, writer.ToString() );
	}
	public void ReadSoundVolumes(){
		string filePath = System.IO.Path.Combine(Application.dataPath.Replace ("/Assets",""),fileName) ;
		if(File.Exists (filePath)==false){
			SetMasterVolume (100);
			SetMusicVolume (100);
			SetUIVolume (100);
			SetSoundEffectsVolume (100);
			SetAmbientVolume (100);
			Debug.Log ("load"); 
			return;
		}

		string[] lines = File.ReadAllLines (filePath);
		foreach(string line in lines){
			string[] split = line.Split ('=');
			if(split.Length!=2){
				continue;
			}
			split[0]=split[0].Trim();
			split[1]=split[1].Trim();
			int value = 0;
			if(int.TryParse (split[1],out value)==false){
				Debug.LogError ("Error in reading in integer from "+fileName);
				value = 50;
			}
			switch(split[0]){
			case "masterVolume":
				masterVolume = value;
				break;
			case "musicVolume":
				musicVolume = value;
				break;
			case "ambientVolume":
				ambientVolume = value;
				break;
			case "soundVolume":
				soundVolume = value;
				break;
			case "uiVolume":
				uiVolume = value;
				break;
			default: 
				Debug.LogError ("This name is not a volume type");
				break;
			}
		}
	}

	void OnDisable(){
		SaveVolumetSchema ();
	}


}
