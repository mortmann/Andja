using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BalanceUIText : MonoBehaviour {
    public Player player;
    public Text balanceText;
    public Text changeText;

    // Use this for initialization
    void Start() {
        player = PlayerController.Instance.CurrPlayer;
    }

    // Update is called once per frame
    void Update() {
        if (player.Balance < 0) {
            balanceText.color = Color.red;
        }
        if (player.Balance >= 0) {
            balanceText.color = Color.black;
        }
        if (player.LastChange < 0) {
            changeText.color = Color.red;
        }
        if (player.LastChange >= 0) {
            changeText.color = Color.green;
        }
        balanceText.text = player.Balance + " ";
        changeText.text = "" + player.LastChange + " ";

    }
}
