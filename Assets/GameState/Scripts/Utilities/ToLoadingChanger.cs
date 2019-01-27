using UnityEngine;
using UnityEngine.SceneManagement;

//just to change it to loading cause nothing works when you start here!
public class ToLoadingChanger : MonoBehaviour {


    void OnEnable() {
        if (World.Current != null) {
            Destroy(this);
            return;
        }
        SceneManager.LoadScene("GameStateLoadingScreen");
    }


}
