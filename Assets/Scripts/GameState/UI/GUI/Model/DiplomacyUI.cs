using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Andja.UI {

    public class DiplomacyUI : MonoBehaviour {
        public Transform playerContent;
        public Transform decisionContent;

        public GameObject playerObjectPrefab;
        public Dictionary<Player, GameObject> playerToGO;

        private Dictionary<Player, UILineRenderer> playerToLine;
        public GameObject prefabPlayerLine;
        public Button increaseDiplomaticStatusButton;
        public Button decreaseDiplomaticStatusButton;
        public Button sendMoneyButton;
        public Button demandMoneyButton;
        public Button denouncePlayerButton;
        public Button praisePlayerButton;
        private Player selectedPlayer;

        // Use this for initialization
        private void Start() {
            ShowFor(PlayerController.CurrentPlayer);
            increaseDiplomaticStatusButton.onClick.AddListener(TryToIncreaseDiplomaticStatus);
            decreaseDiplomaticStatusButton.onClick.AddListener(DecreaseDiplomaticStatus);
            sendMoneyButton.onClick.AddListener(SendMoneyToPlayer);
            demandMoneyButton.onClick.AddListener(TryToDemandMoney);
            denouncePlayerButton.onClick.AddListener(DenouncePlayer);
            praisePlayerButton.onClick.AddListener(PraisePlayer);
        }

        private void ShowFor(Player showPlayer) {
            selectedPlayer = showPlayer;
            foreach (Transform t in playerContent) {
                Destroy(t.gameObject);
            }
            GameObject center = Instantiate(playerObjectPrefab);
            center.transform.SetParent(playerContent, false);
            center.transform.localPosition = Vector3.zero;
            string name = showPlayer.Number == PlayerController.currentPlayerNumber ? "You" : showPlayer.Name;
            center.GetComponentInChildren<Text>().text = name;
            int number = 0;
            int playerAmount = PlayerController.PlayerCount;
            float degreeBetweenPlayer = Mathf.Min(90, 360f / playerAmount);
            float startDegree = 270;
            float y = (playerContent.GetComponent<RectTransform>().sizeDelta.y - 1.7f * center.GetComponent<RectTransform>().sizeDelta.y) / 2;
            Vector2 distance = new Vector2(0, -y);
            playerToLine = new Dictionary<Player, UILineRenderer>();
            foreach (Player other in PlayerController.Players) {
                if (other == showPlayer)
                    continue;
                float degree = Mathf.Deg2Rad * (startDegree + number * degreeBetweenPlayer) % 360;
                Vector2 pos = new Vector2(distance.x * Mathf.Cos(degree) - distance.y * Mathf.Sin(degree),
                                          distance.x * Mathf.Sin(degree) + distance.y * Mathf.Cos(degree));
                GameObject otherPlayerGo = Instantiate(playerObjectPrefab);
                otherPlayerGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                otherPlayerGo.GetComponentInChildren<Text>().text = other.Number == PlayerController.currentPlayerNumber ? "You" : other.Name;
                otherPlayerGo.transform.SetParent(playerContent, false);
                otherPlayerGo.transform.localPosition = pos;
                EventTrigger trigger = otherPlayerGo.GetComponentInChildren<EventTrigger>();
                EventTrigger.Entry click = new EventTrigger.Entry();
                click.eventID = EventTriggerType.PointerClick;
                Player temp = other;
                click.callback.AddListener((eventData) => { ShowFor(temp); });
                trigger.triggers.Add(click);

                GameObject line = Instantiate(prefabPlayerLine); //needed because iniated uilinerenderer bugged points == null
                //new GameObject {
                //    name = "LineRenderer-" + showPlayer.Name + "-" + other.Name
                //};
                line.transform.SetParent(playerContent, false);
                line.transform.localPosition = Vector2.zero;// center.transform.position;
                UILineRenderer uILineRenderer = line.GetComponent<UILineRenderer>();
                uILineRenderer.LineThickness = 20f;
                uILineRenderer.color = GetColorForDiplomaticStatus(PlayerController.Instance.GetDiplomaticStatusType(showPlayer, other));
                line.transform.SetAsFirstSibling();
                playerToLine[other] = uILineRenderer;
                uILineRenderer.Points = new Vector2[] { /*(Vector2)center.transform.position +*/ Vector2.zero,/* (Vector2)center.transform.position +*/ pos };
                number++;
            }

            if (showPlayer.IsCurrent()) {
                foreach (Button b in decisionContent.GetComponentsInChildren<Button>()) {
                    b.interactable = false;
                }
            }
            else {
                foreach (Button b in decisionContent.GetComponentsInChildren<Button>()) {
                    b.interactable = true;
                }
            }
            if (showPlayer.IsCurrent() == false) {
                PlayerController.Instance.RegisterPlayersDiplomacyStatusChange(OnDiplomacyChange);
                DiplomacyType dt = PlayerController.Instance.GetDiplomaticStatusType(showPlayer, PlayerController.CurrentPlayer);
                OnDiplomacyChange(PlayerController.CurrentPlayer, selectedPlayer, dt, dt);
            }
            if (showPlayer.IsHuman) {
                denouncePlayerButton.interactable = false;
                praisePlayerButton.interactable = false;
            }
            else {
                denouncePlayerButton.interactable = true;
                praisePlayerButton.interactable = true;
            }
        }

        private void OnDiplomacyChange(Player playerOne, Player playerTwo, DiplomacyType oldType, DiplomacyType newType) {
            if (selectedPlayer != playerOne && selectedPlayer != playerTwo) {
                return;
            }
            if (playerOne != selectedPlayer) {
                playerToLine[playerOne].color = GetColorForDiplomaticStatus(newType);
            }
            else {
                playerToLine[playerTwo].color = GetColorForDiplomaticStatus(newType);
            }
            if (selectedPlayer.IsCurrent())
                return;
            //TODO: update buttons better
            if (newType != DiplomacyType.Alliance) {
                increaseDiplomaticStatusButton.GetComponentInChildren<Text>().text = "" + (DiplomacyType)((int)newType + 1);
                increaseDiplomaticStatusButton.interactable = true;
            }
            else {
                increaseDiplomaticStatusButton.interactable = false;
            }
            if (newType != DiplomacyType.War) {
                decreaseDiplomaticStatusButton.GetComponentInChildren<Text>().text = "" + (DiplomacyType)((int)newType - 1);
                decreaseDiplomaticStatusButton.interactable = true;
            }
            else {
                decreaseDiplomaticStatusButton.interactable = false;
            }
        }

        private Color GetColorForDiplomaticStatus(DiplomacyType type) {
            switch (type) {
                case DiplomacyType.War:
                    return Color.red;

                case DiplomacyType.Neutral:
                    return new Color(0.7f, 0.7f, 0.7f, 1);

                case DiplomacyType.TradeAgreement:
                    return Color.blue;

                case DiplomacyType.Alliance:
                    return Color.green;

                default:
                    return Color.magenta;
            }
        }

        private void OnEnable() {
            ShowFor(selectedPlayer ?? PlayerController.CurrentPlayer);
        }

        private void PraisePlayer() {
            PlayerController.Instance.PraisePlayer(PlayerController.CurrentPlayer, selectedPlayer);
        }

        private void DenouncePlayer() {
            PlayerController.Instance.DenouncePlayer(PlayerController.CurrentPlayer, selectedPlayer);
        }

        private void TryToDemandMoney() {
            PlayerController.Instance.TryToDemandMoney(PlayerController.CurrentPlayer, selectedPlayer, 1000);
        }

        private void SendMoneyToPlayer() {
            PlayerController.Instance.SendMoneyFromTo(PlayerController.CurrentPlayer, selectedPlayer, 1000);
        }

        private void DecreaseDiplomaticStatus() {
            PlayerController.Instance.DecreaseDiplomaticStanding(PlayerController.CurrentPlayer, selectedPlayer);
        }

        private void TryToIncreaseDiplomaticStatus() {
            PlayerController.Instance.IncreaseDiplomaticStanding(PlayerController.CurrentPlayer, selectedPlayer);
        }
    }
}