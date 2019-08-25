using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGameSettings : MonoBehaviour {

    public void SetSeed(string value) {
        GameDataHolder.MapSeed = Mathf.Abs(value.GetHashCode());
    }
    public void SetHeight(string text) {
        int value = 0;
        if (int.TryParse(text, out value) == false && text.Length > 0) {
            Debug.LogError(transform.parent?.name + " Inputfield is not number only ");
        }
        GameDataHolder.Height = Mathf.Abs(value);
    }
    public void SetWidth(string text) {
        int value = 0;
        if (int.TryParse(text, out value) == false && text.Length > 0) {
            Debug.LogError(transform.parent?.name + " Inputfield is not number only ");
        }
        GameDataHolder.Width = Mathf.Abs(value);
    }
    public void SetPirate(bool value) {
        GameDataHolder.pirates = value;
    }
    public void SetFire(bool value) {
        GameDataHolder.fire = value;
    }
}
