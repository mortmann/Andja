using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class HoverOverScript : MonoBehaviour {
	float lifetime=1;
	float hovertime=3f;
	bool show;

	public void Show(string text){
//		if(GetComponentInParent<Mask>()!=null){
//			while(GetComponentInParent<Mask>()!=null){
//				transform.SetParent (transform.parent.parent);
//			}
//		}
		transform.GetChild (0).gameObject.SetActive (true);
		GetComponentInChildren<Text> ().text = text;
		transform.GetChild (0).gameObject.SetActive (false);
		show = true;
	}
	public void Unshow(){
		transform.GetChild (0).gameObject.SetActive (false);
		show = false;
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
