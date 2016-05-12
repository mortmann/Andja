using UnityEngine;
using System.Collections;

public class NeedsUIController : MonoBehaviour {

	public GameObject needPrefab;
	public GameObject contentCanvas;

	// Use this for initialization
	void Start () {
		for (int i = 0; i < 20; i++) {
			GameObject b = Instantiate(needPrefab);
			b.transform.SetParent(contentCanvas.transform);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
