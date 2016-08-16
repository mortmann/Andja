using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BalanceUIText : MonoBehaviour {
	public Player player;
	public Text balanceText;
	public Text changeText;
	public Text fpsText;
	// Use this for initialization
	void Start () {
		player = GameObject.FindObjectOfType<PlayerController>().currPlayer;
	}
	
	// Update is called once per frame
	void Update () {
		if(player.balance < 0){
			balanceText.color = Color.red;
		}
		if(player.balance >= 0){
			balanceText.color = Color.black;
		}
		if(player.change < 0){
			changeText.color = Color.red;
			changeText.text =""+ player.change + " ";
		}
		if(player.change >= 0){
			changeText.color = Color.green;
			changeText.text ="+ "+ player.change + " ";
		}
		balanceText.text = player.balance + " ";

		fpsText.text = Mathf.Round (1.0f / Time.deltaTime) + " fps ";
	}
}
