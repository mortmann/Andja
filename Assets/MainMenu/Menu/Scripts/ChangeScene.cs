using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class ChangeScene : MonoBehaviour {
	
	public void ChangeSceneClick(){
		SceneManager.LoadScene ("GameState"); 
	}
	public void OpenMenuState(){
		SceneManager.LoadScene ("MainMenu"); 
	}
}
