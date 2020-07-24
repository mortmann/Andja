using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class HoverOverScript : MonoBehaviour {
    float lifetime = 1;
    public float hovertime = HoverDuration;
    public const float HoverDuration = 1.5f;
    bool isDebug = false;
    public RectTransform rect;
    bool show;
    public Text Header;
    public Text Description;
    public RectTransform fitForm;
    Vector2 Position = Vector2.negativeInfinity;
    bool staticPosition = false;
    private bool truePosition = true;
    private bool instantShow = false;

    private void Start() {
        rect = GetComponent<RectTransform>();
    }
    public void Show(string header) {
        Show(header, null);
    }
    public void Show(string header, params string[] descriptions) {
        transform.GetChild(0).gameObject.SetActive(true);
        Header.text = header;
        if (descriptions != null) {
            Description.gameObject.SetActive(true);
            string description = "";
            foreach(string s in descriptions) {
                if (s == null)
                    continue;
                description += "\n" + s;
            }
            Description.text = description.Trim();
        }
        else {
            Description.gameObject.SetActive(false);
        }
        transform.GetChild(0).gameObject.SetActive(isDebug);
        show = true;
    }
    public void Show(string header, Vector3 position, bool truePosition, bool instantShow, params string[] descriptions) {
        Show(header, descriptions);
        Position = position;
        staticPosition = true;
        this.truePosition = truePosition;
        this.instantShow = instantShow;
        if(instantShow)
            transform.GetChild(0).gameObject.SetActive(true);
        fitForm.ForceUpdateRectTransforms();
    }
    public void Unshow() {
        transform.GetChild(0).gameObject.SetActive(false);
        show = false;
        isDebug = false;
        staticPosition = false;
        this.instantShow = false;
        this.truePosition = true;
    }
    void Update() {
        if (show == false && hovertime == HoverDuration)
            return;
        if (show) {
            hovertime -= Time.deltaTime;
        }
        else {
            hovertime += Time.deltaTime;
        }
        //if (EventSystem.current.IsPointerOverGameObject() == isDebug &&  .Contains(Input.mousePosition) == false) {
        //    Unshow();
        //    hovertime = HoverDuration;
        //}
        hovertime = Mathf.Clamp(hovertime, 0, HoverDuration);
        if (hovertime > 0 && instantShow == false) {
            return;
        }
        transform.GetChild(0).gameObject.SetActive(true);
        Vector3 offset = Vector3.zero;
        if (truePosition == false) {
            offset = -fitForm.sizeDelta / 2;
            offset *= UIController.Instance.CanvasScale; //Fix for the scaling ...
        }
        Vector3 position = Input.mousePosition;
        if (staticPosition)
            position = Position;
        if (fitForm.sizeDelta.x + position.x > Screen.width) {
            offset.x = Screen.width - (fitForm.sizeDelta.x + position.x);
        }
        if (fitForm.sizeDelta.y + position.y > Screen.height) {
            offset.y = Screen.height - (fitForm.sizeDelta.y + position.y);
        }
        if (position.x < 0 ) {
            position.x = 0;
        }
        if (position.y < 0 ) {
            position.y = 0;
        }
        fitForm.transform.position = position + offset;
        lifetime -= Time.deltaTime;
    }

    internal void DebugTileInfo(Tile tile) {
        isDebug = true;
        hovertime = 0;
        //show tile info and when structure not null that as well
        Show(tile.ToBaseString(), tile.Structure?.ToString(), AIController.Instance?.GetTileValue(tile));
    }

}
