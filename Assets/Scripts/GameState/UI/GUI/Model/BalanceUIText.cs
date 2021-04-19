using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class BalanceUIText : MonoBehaviour {
        public Player player => PlayerController.CurrentPlayer;
        public Text balanceText;
        public Text changeText;

        private void Update() {
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
            changeText.text = (player.LastTreasuryChange > 0 ? "+" : "") + player.LastTreasuryChange + " ";
        }
    }
}