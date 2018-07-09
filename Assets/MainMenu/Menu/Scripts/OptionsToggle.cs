using UnityEngine;
using System.Collections;

public class OptionsToggle : MonoBehaviour {

    public GameObject[] childrens;
    int lastOpen = 0;
    void OnEnable(){
		Show (lastOpen);
	}
    public void Show(int numberOfChildToShow) {
        if (childrens == null)
            return;
        for (int i = 0; i < childrens.Length; i++) {
            childrens[i].SetActive(false);
        }
        childrens[numberOfChildToShow].SetActive(true);
        lastOpen = numberOfChildToShow;
    }
    private void OnDisable() {
        for (int i = 0; i < childrens.Length; i++) {
            childrens[i].SetActive(false);
        }
    }
}
