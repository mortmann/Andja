using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class ChangeScene : MonoBehaviour {
	
	public void ChangeToGameStateLoadScreen(){
		SceneManager.LoadScene ("GameStateLoadingScreen"); 
	}
	public void ChangeToEditorLoadScreen(){
		SceneManager.LoadScene ("EditorLoadingScreen"); 
	}
	public void OpenMenuState(){
		SceneManager.LoadScene ("MainMenu"); 
	}
}
