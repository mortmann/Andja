using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class BalanceUIText : MonoBehaviour {
        public Player player => PlayerController.CurrentPlayer;
        public ImageText balanceText;
        public ImageText changeText;
        private void Start() {
            balanceText.Set(UISpriteController.GetIcon(CommonIcon.Money), StaticLanguageVariables.Balance, "");
            changeText.Set(UISpriteController.GetIcon(CommonIcon.Upkeep), StaticLanguageVariables.BalanceChange, "");
        }
        private void Update() {
            if (player.TreasuryBalance < 0) {
                balanceText.SetColorText(Color.red);
            }
            if (player.TreasuryBalance >= 0) {
                balanceText.SetColorText(Color.black);
            }
            if (player.LastTreasuryChange < 0) {
                changeText.SetColorText(Color.red);
            }
            if (player.LastTreasuryChange >= 0) {
                changeText.SetColorText(Color.green);
            }
            balanceText.SetText(player.TreasuryBalance + " ");
            changeText.SetText((player.LastTreasuryChange > 0 ? "+" : "") + player.LastTreasuryChange + " ");
            if(MouseController.Instance.NeededBuildCost > 0) {
                balanceText.ShowAddon(MouseController.Instance.NeededBuildCost + "", TextColor.Negative);
            } else {
                balanceText.RemoveAddon();
            }
        }
    }
}