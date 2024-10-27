namespace Andja.Controller {

    public class PlayerCommands : ConsoleCommand {
        
        public PlayerCommands() : base("player", null){
            NextLevelCommands = new ConsoleCommand[]{
                new ConsoleCommand("change", ChangePlayer),
                new ConsoleCommand("money", ChangePlayerMoney),
                new ConsoleCommand("diplomatic", ChangeWar),
                //new ConsoleCommand("effect", ChangeEffect),
            };
        }

        private bool ChangeWar(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            int playerOne = PlayerController.currentPlayerNumber;
            int pos = 0;
            // anything can thats not a number can be the current player
            if (parameters.Length > 2) {
                if (int.TryParse(parameters[pos], out playerOne) == false) {
                    return false;
                }
                pos++;
            }
            if (int.TryParse(parameters[pos], out int playerTwo) == false) {
                return false;
            }
            if (playerOne < 0 || playerOne >= PlayerController.Instance.PlayerCount)
                return false;
            if (playerTwo < 0 || playerTwo >= PlayerController.Instance.PlayerCount)
                return false;

            if (PlayerController.Instance.ArePlayersAtWar(playerOne, playerTwo) == false)
                PlayerController.Instance.ChangeDiplomaticStanding(playerOne, playerTwo, DiplomacyType.War, true);
            else
                PlayerController.Instance.ChangeDiplomaticStanding(playerOne, playerTwo, DiplomacyType.Neutral, true);
            return true;
        }

        private bool ChangePlayerMoney(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            int player = PlayerController.currentPlayerNumber;
            int pos = 0;
            // anything can thats not a number can be the current player
            if (parameters.Length > 1) {
                if (int.TryParse(parameters[pos], out player) == false) {
                    return false;
                }
                else {
                    pos++;
                }
            }
            if (int.TryParse(parameters[pos], out int money) == false) {
                return false;
            }
            PlayerController.Instance.AddMoney(money, player);
            return true;
        }

        private bool ChangePlayer(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            int pos = 0;
            // anything can thats not a number can be the current player
            if (int.TryParse(parameters[pos], out var player) == false) {
                return false;
            }
            return PlayerController.Instance.ChangeCurrentPlayer(player);
        }
    }
}