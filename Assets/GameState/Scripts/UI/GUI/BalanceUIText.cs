using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BalanceUIText : MonoBehaviour {
	public Player player;
	public Text balanceText;
	public Text changeText;

	// Use this for initialization
	void Start () {
		player = GameObject.FindObjectOfType<PlayerController>().currPlayer;
	}
	
	// Update is called once per frame
	void Update () {
		if(player.Balance < 0){
			balanceText.color = Color.red;
		}
		if(player.Balance >= 0){
			balanceText.color = Color.black;
		}
		if(player.Change < 0){
			changeText.color = Color.red;
			changeText.text =""+ player.Change + " ";
		}
		if(player.Change >= 0){
			changeText.color = Color.green;
			changeText.text ="+ "+ player.Change + " ";
		}
		balanceText.text = player.Balance + " ";

	}
}
