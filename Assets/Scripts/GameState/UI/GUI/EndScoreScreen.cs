using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScoreScreen : MonoBehaviour {
    public Button closeButton;

    void Start() {
        closeButton.onClick.AddListener(OnCloseButton);
    }

    private void OnCloseButton() {
        SceneManager.LoadScene("MainMenu");
    }

    void Update() {
        
    }
}
