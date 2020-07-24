using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BalanceUIText : MonoBehaviour {
    public Player player => PlayerController.CurrentPlayer;
    public Text balanceText;
    public Text changeText;

    // Update is called once per frame
    void Update() {
        if (player.TreasuryBalance < 0) {
            balanceText.color = Color.red;
        }
        if (player.TreasuryBalance >= 0) {
            balanceText.color = Color.black;
        }
        if (player.LastTreasuryChange < 0) {
            changeText.color = Color.red;
        }
        if (player.LastTreasuryChange >= 0) {
            changeText.color = Color.green;
        }
        balanceText.text = player.TreasuryBalance + " ";
        changeText.text = (player.LastTreasuryChange>0? "+" : "") + player.LastTreasuryChange + " ";
    }
}
