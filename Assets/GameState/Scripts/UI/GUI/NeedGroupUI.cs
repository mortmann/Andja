using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NeedGroupUI : MonoBehaviour {
    public Text nameText;

    public void Show(NeedGroup group) {
        nameText.text = group.Name;
    }
}
