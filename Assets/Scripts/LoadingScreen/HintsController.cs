using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Andja.Controller;
using Random = UnityEngine.Random;

namespace Andja.LoadScreen {

    public class HintsController : MonoBehaviour {
        public TMP_Text hintText;
        const float timePerHint = 5f;
        List<string> hintList = new List<string>();
        int currentIndex = 0;
        float timeToNextHint;
        void Start() {
            timeToNextHint = timePerHint;
            hintList = UILanguageController.Instance.LoadHints();
            hintList.Add("Really important Hint.");
            hintList.Add("Really unimportant Hint.");
            hintList.Add("Really sarcastic Hint.");
            hintList.Add("Not important at all Hint.");
            hintList.Add("Dumb Hint.");
            hintList.Add("Joke Hint.");
            ShowNextHint();
        }
        void Update() {
            if(Input.GetKeyDown(KeyCode.Space)) {
                timeToNextHint = 0;
            }
            timeToNextHint -= Time.deltaTime;
            if(timeToNextHint <= 0) {
                timeToNextHint = timePerHint;
                ShowNextHint();
            }
        }

        private void ShowNextHint() {
            int newIndex = Random.Range(0, hintList.Count);
            if (newIndex == currentIndex) //for the chance it is the same just increase it 
                currentIndex = ++currentIndex % hintList.Count;
            hintText.text = hintList[currentIndex];
        }

    }
}
