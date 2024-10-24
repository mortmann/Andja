﻿using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ValueNameSetter : MonoBehaviour {
        public Text NameText;
        public Text ValueText;

        public void Show(string name, object value, Transform parent = null) {
            NameText.text = name;
            ValueText.text = value?.ToString();
            if (parent != null)
                transform.SetParent(parent, false);
        }

        public void Show(object value) {
            ValueText.text = value?.ToString();
        }

        // Update is called once per frame
    }
}