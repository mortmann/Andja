using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BalanceUIText : MonoBehaviour {
	public PlayerController playerController;
	public Text balanceText;
	public Text changeText;
	// Use this for initialization
	void Start () {
		playerController = GameObject.FindObjectOfType<PlayerController>();
	}
	
	// Update is called once per frame
	void Update () {
		if(playerController.balance < 0){
			balanceText.color = Color.red;
		}
		if(playerController.balance >= 0){
			balanceText.color = Color.black;
		}
		if(playerController.change < 0){
			changeText.color = Color.red;
			changeText.text =""+ playerController.change + " ";
		}
		if(playerController.change >= 0){
			changeText.color = Color.green;
			changeText.text ="+ "+ playerController.change + " ";
		}
		balanceText.text = playerController.balance + " ";
	}
}
