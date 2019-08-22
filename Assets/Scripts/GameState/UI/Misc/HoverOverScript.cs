using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class HoverOverScript : MonoBehaviour {
    float lifetime = 1;
    public float hovertime = HoverDuration;
    public const float HoverDuration = 3f;
    bool isDebug = false;
    public RectTransform rect;
    bool show;
    public Text Header;
    public Text Description;
    public RectTransform fitForm;
    private void Start() {
        rect = GetComponent<RectTransform>();
    }
    public void Show(string header, string description = null) {
        transform.GetChild(0).gameObject.SetActive(true);
        Header.text = header;
        if (description != null) {
            Description.gameObject.SetActive(true);
            Description.text = description;
        }
        else {
            Description.gameObject.SetActive(false);
        }
        
        transform.GetChild(0).gameObject.SetActive(isDebug);
        show = true;
    }
    public void Unshow() {
        transform.GetChild(0).gameObject.SetActive(false);
        show = false;
        isDebug = false;
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
        if (EventSystem.current.IsPointerOverGameObject() == isDebug) {
            Unshow();
            hovertime = HoverDuration;
        }
        hovertime = Mathf.Clamp(hovertime, 0, HoverDuration);
        if (hovertime > 0) {
            return;
        }
        transform.GetChild(0).gameObject.SetActive(true);
        //rect.sizeDelta = fitForm.sizeDelta;
        Vector3 offset = Vector3.zero;
        if (fitForm.sizeDelta.x + Input.mousePosition.x > Screen.width) {
            offset.x = Screen.width - (fitForm.sizeDelta.x + Input.mousePosition.x);
        }
        if (fitForm.sizeDelta.y + Input.mousePosition.y > Screen.height) {
            offset.y = Screen.height - (fitForm.sizeDelta.y + Input.mousePosition.y);
        }
        Vector3 pos = Input.mousePosition;
        if ( Input.mousePosition.x < 0 ) {
            pos.x = 0;
        }
        if ( Input.mousePosition.y < 0 ) {
            pos.y = 0;
        }
        this.transform.position = pos + offset;
        lifetime -= Time.deltaTime;
    }

    internal void DebugTileInfo(Tile tile) {
        isDebug = true;
        hovertime = 0;
        //show tile info and when structure not null that as well
        Show(tile.ToString(), tile.Structure?.ToString());
    }

}
