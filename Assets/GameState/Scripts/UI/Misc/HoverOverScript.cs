using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class HoverOverScript : MonoBehaviour {
	float lifetime=1;
	public float hovertime = HoverDuration;
    public const float HoverDuration = 3f;
    bool isDebug = false;

    bool show;
    public Text Header;
    public Text Description;
    public void Show(string header, string description = null){
//		if(GetComponentInParent<Mask>()!=null){
//			while(GetComponentInParent<Mask>()!=null){
//				transform.SetParent (transform.parent.parent);
//			}
//		}
		transform.GetChild (0).gameObject.SetActive (true);
		GetComponentInChildren<Text> ().text = header;
        if(description != null) {
            Description.gameObject.SetActive(true);
            Description.text = description;
        } else {
            Description.gameObject.SetActive(false);
        }
		transform.GetChild (0).gameObject.SetActive (isDebug);
		show = true;
	}
	public void Unshow(){
		transform.GetChild (0).gameObject.SetActive (false);
		show = false;
        isDebug = false;
    }
	void FixedUpdate(){
        if (show == false && hovertime == HoverDuration)
            return;
		if(show){
			hovertime -= Time.deltaTime;
		} else {
			hovertime += Time.deltaTime;
		}
		if(EventSystem.current.IsPointerOverGameObject () == isDebug) {
            Unshow();
            hovertime = HoverDuration;
		}
		hovertime = Mathf.Clamp (hovertime,0, HoverDuration);
		if(hovertime>0){
			return;
		}

		transform.GetChild (0).gameObject.SetActive (true);
		this.transform.position = Input.mousePosition;
		lifetime -= Time.deltaTime;
	}

    internal void DebugTileInfo(Tile tile) {
        isDebug = true;
        hovertime = 0;
        //show tile info and when structure not null that as well
        Show(tile.ToString(), tile.Structure?.ToString());
    }

}
