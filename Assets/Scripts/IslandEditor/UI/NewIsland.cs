using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
public class NewIsland : MonoBehaviour {
    public InputField height;
    public InputField width;
    public Dropdown zone;
    public Button create;
    // Use this for initialization
    void Start() {
        create.onClick.AddListener(OnCreateClick);
        height.text = EditorController.Height+"";
        width.text = EditorController.Width+"";
        zone.ClearOptions();
        zone.AddOptions(Enum.GetNames(typeof(Climate)).ToList());
        zone.value = (int)EditorController.climate;
    }

    public void OnCreateClick() {
        int h = int.Parse(height.text);
        int w = int.Parse(width.text);
        Climate cli = (Climate)zone.value;
        EditorController.Instance.NewIsland(w, h, cli);
    }
}
