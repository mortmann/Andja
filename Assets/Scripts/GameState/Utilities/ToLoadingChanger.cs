﻿using Andja.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Utility {

    //just to change it to loading cause nothing works when you start here!
    public class ToLoadingChanger : MonoBehaviour {

        private void OnEnable() {
            if (World.Current != null) {
                Destroy(this);
                return;
            }
            else if (SceneManager.GetActiveScene().name == "GameState")
                GameState();
        }

        public void GameState() {
            SceneManager.LoadScene("GameStateLoadingScreen");
        }

        public void Editor() {
            SceneManager.LoadScene("EditorLoadingScreen");
        }
    }
}