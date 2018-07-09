using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class HoverOverScript : MonoBehaviour {
	float lifetime=1;
	public float hovertime=3f;
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
		transform.GetChild (0).gameObject.SetActive (false);
		show = true;
	}
	public void Unshow(){
		transform.GetChild (0).gameObject.SetActive (false);
		show = false;
	}
	public void DebugInfo(string info){
		transform.GetChild (0).gameObject.SetActive (true);
		GetComponentInChildren<Text> ().text = info;
		this.transform.position = Input.mousePosition;

	}
	void FixedUpdate(){
		if(show){
			hovertime -= Time.deltaTime;

		} else {
			hovertime += Time.deltaTime;
		}
		if(EventSystem.current.IsPointerOverGameObject ()==false){
			Unshow ();
			hovertime = float.MaxValue;
		}
		hovertime = Mathf.Clamp (hovertime,0,3);
		if(hovertime>0){
			return;
		}

		transform.GetChild (0).gameObject.SetActive (true);

		this.transform.position = Input.mousePosition;

		lifetime -= Time.deltaTime;
	
	}
}
