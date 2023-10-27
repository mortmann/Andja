using Andja.UI;
using Andja.Controller;
using System;
using UnityEngine;
using Andja.Model;
using Newtonsoft.Json;
using Andja.UI.Model;

namespace Andja.UI {
    public enum InformationType { UnitUnderAttack, StructureUnderAttack, DiplomacyChange, Praise, Denounce, ReceivedGift, 
                                  DemandMoney }
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BasicInformation {
        public InformationType Type;
        public Func<Vector2> GetPosition;
        public object[] DescriptionValues;
        public object[] TitleValues;
        TranslationData translation;
        public string SpriteName { get; internal set; }

        public BasicInformation(InformationType type, Func<Vector2> getPosition, 
                                    object[] titleValues, object[] descriptionValues,
                                    string spriteName) {
            Type = type;
            GetPosition = getPosition;
            DescriptionValues = descriptionValues;
            TitleValues = titleValues;
            SpriteName = spriteName;
        }
        public BasicInformation() { }

        public abstract BasicInformation Load();
        public string GetTitle() {
            if (translation == null)
                return "MISSING Title";
            string text = translation.translation;
            for (int i = 0; i < TitleValues.Length; i++) {
                if(TitleValues[i] is string value)
                    text = text.Replace("$" + i, value);
                else
                    text = text.Replace("$" + i, UILanguageController.Instance.GetTranslation(TitleValues[i]));
            }
            return text;
        }
        public string GetDescription() {
            if (translation == null)
                return "MISSING Description";
            string text = translation.toolTipTranslation;
            for (int i = 0; i < DescriptionValues.Length; i++) {
                if (DescriptionValues[i] is string)
                    text = text.Replace("$" + i, DescriptionValues[i].ToString());
                else
                    text = text.Replace("$" + i, UILanguageController.Instance.GetTranslation(DescriptionValues[i]));
            }
            return text;
        }
        public void OnLanguageChange() {
            translation = UILanguageController.Instance.GetTranslationData(Type);
        }

        public static BasicInformation CreateUnitDamage(Unit unit, IWarfare warfare) {
            return new UnitUnderAttack(unit, warfare);
        }
        public static BasicInformation CreateStructureDamage(Structure Structure, IWarfare warfare) {
            return new StructureUnderAttack(Structure, warfare); 
        }
        public static BasicInformation DiplomacyChanged(DiplomaticStatus status) {
            return new DiplomaticStatusChange(status);
        }
        internal static BasicInformation CreatePraiseReceived(Player from) {
            return new PraiseInteraction(from);
        }
        internal static BasicInformation CreateDenounceReceived(Player from) {
            return new DenounceInteraction(from);
        }
        internal static BasicInformation CreateMoneyReceived(Player from, int amount) {
            return new MoneyReceivedInteraction(from, amount);
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AttackInformation : BasicInformation {
        [JsonProperty] protected uint buildID;
        [JsonProperty] protected IWarfare Warfare;

        public AttackInformation() { }

        public AttackInformation(InformationType type, Func<Vector2> getPosition, object[] titleValues,
            object[] descriptionValues, string spriteName, uint buildID, IWarfare warfare) : 
            base(type, getPosition, titleValues, descriptionValues, spriteName) {
            this.buildID = buildID;
            Warfare = warfare;
        }
        public bool IsSame(uint hit, IWarfare warfare) {
            return buildID == hit && Warfare == warfare;
        }
    }
    public class UnitUnderAttack : AttackInformation {
        public UnitUnderAttack() { }
        public UnitUnderAttack(Unit Unit, IWarfare warfare) :
            base(InformationType.UnitUnderAttack,
                () => Unit.PositionVector,
                new string[] { Unit.Name },
                warfare != null ? new string[] { Unit.Name, GetAttackerName(warfare) } : new string[] { Unit.Name },
                "Attacked",
                Unit.BuildID,
                warfare
                ) {
        }

        private static string GetAttackerName(IWarfare warfare) {
            return warfare.PlayerNumber == GameData.PirateNumber? 
                UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.Pirate) :
                PlayerController.Instance.GetPlayer(warfare.PlayerNumber).Name;
        }

        public override BasicInformation Load() {
            return new UnitUnderAttack(World.Current.GetUnitFromBuildID(buildID), Warfare);
        }
    }
    public class StructureUnderAttack : AttackInformation {
        public StructureUnderAttack() { }

        public StructureUnderAttack(Structure Structure, IWarfare warfare) :
            base(InformationType.StructureUnderAttack,
                () => Structure.Center,
                new string[] { Structure.Name },
                warfare != null ? new string[] { Structure.Name, PlayerController.Instance.GetPlayer(warfare.PlayerNumber).Name } : new string[] { Structure.Name },
                "Attacked",
                Structure.BuildID,
                warfare
                ) {
        }

        public override BasicInformation Load() {
            return new StructureUnderAttack(BuildController.Instance.BuildIdToStructure[buildID], Warfare);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class DiplomaticStatusChange : BasicInformation {
        [JsonProperty] int PlayerNumberOne;
        [JsonProperty] int PlayerNumberTwo;
        public DiplomaticStatusChange(DiplomaticStatus status) : base(
            InformationType.DiplomacyChange, 
            () => status.PlayerTwo.GetMainCityPosition(),
            new object[] { status.CurrentStatus }, 
            new object[] { status.PlayerOne.Name, status.PlayerTwo.Name, status.CurrentStatus },
            status.CurrentStatus.ToString()
            ) {
            PlayerNumberOne = status.PlayerNumberOne;
            PlayerNumberTwo = status.PlayerNumberTwo;
        }
        public DiplomaticStatusChange() { }

        public override BasicInformation Load() {
            return new DiplomaticStatusChange(PlayerController.Instance.GetDiplomaticStatus(PlayerNumberOne, PlayerNumberTwo));
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class PlayerInteraction : BasicInformation {
        [JsonProperty] protected int fromPlayer;
        [JsonProperty] protected int moneyAmount;
        public PlayerInteraction() { }

        public PlayerInteraction(InformationType type, object[] titleValues, object[] descriptionValues, string spriteName, Player from, int amount = 0) 
            : base(type, () => from.GetMainCityPosition(), titleValues, descriptionValues, spriteName) {
            this.fromPlayer = from.GetPlayerNumber();
            moneyAmount = amount;
        }
    }
    class PraiseInteraction : PlayerInteraction {
        public PraiseInteraction(Player from) :
            base(InformationType.Praise, new string[] { from.Name }, new string[] { from.Name }, InformationType.Praise.ToString(), from, 0) {
        }
        public PraiseInteraction() { }

        public override BasicInformation Load() {
            return new PraiseInteraction(PlayerController.Instance.GetPlayer(fromPlayer));
        }
    }
    class DenounceInteraction : PlayerInteraction {
        public DenounceInteraction(Player from) : 
            base(InformationType.Denounce, new string[] { from.Name }, new string[] { from.Name }, InformationType.Denounce.ToString(), from, 0) {
        }
        public DenounceInteraction() { }

        public override BasicInformation Load() {
            return new DenounceInteraction(PlayerController.Instance.GetPlayer(fromPlayer));
        }
    }
    class MoneyReceivedInteraction : PlayerInteraction {
        public MoneyReceivedInteraction(Player from, int amount) 
            : base(InformationType.ReceivedGift, 
                  new string[] { from.Name, amount.ToString() }, 
                  new string[] { from.Name, amount.ToString() }, 
                  InformationType.ReceivedGift.ToString(), 
                  from,
                  amount) {
        }
        public MoneyReceivedInteraction() { }

        public override BasicInformation Load() {
            return new MoneyReceivedInteraction(PlayerController.Instance.GetPlayer(fromPlayer), moneyAmount);
        }
    }
    public abstract class ChoiceInformation : PlayerInteraction {
        public Choice[] Choices;
        public Action OnClose;
        public ChoiceInformation(InformationType type,
                                    object[] titleValues, object[] descriptionValues,
                                    string spriteName, Choice[] choices, Player from, int amount = 0)
                                : base(type, titleValues, descriptionValues, spriteName, from, amount) {
            Choices = choices;
        }
        public ChoiceInformation() { }

        class ChoiceDemandMoney : ChoiceInformation {
            public ChoiceDemandMoney(Player from, int amount) 
                : base(InformationType.DemandMoney,
                      new string[] { from.Name, amount.ToString() },
                      new string[] { from.Name, amount.ToString() },
                      InformationType.DemandMoney.ToString(),
                      new Choice[] {
                        new Choice(StaticLanguageVariables.Yes, () => PlayerController.Instance.SendMoneyFromTo(PlayerController.CurrentPlayer, from, amount)),
                        new Choice(StaticLanguageVariables.No, null)
                      },
                      from, 
                      amount
                      ) {
            }
            public ChoiceDemandMoney() { }
            public override BasicInformation Load() {
                return new ChoiceDemandMoney(PlayerController.Instance.GetPlayer(fromPlayer), moneyAmount);
            }
        }
        [JsonObject(MemberSerialization.OptIn)]
        class ChoiceDiplomacy : ChoiceInformation {
            public ChoiceDiplomacy(Player from, DiplomacyType askForStatus, DiplomaticStatus status)
                : base(InformationType.DemandMoney,
                      new object[] { from.Name, askForStatus },
                      new string[] { from.Name },
                      askForStatus.ToString(),
                      new Choice[] {
                        new Choice(StaticLanguageVariables.Yes, () => status.Increase()),
                        new Choice(StaticLanguageVariables.No, null)
                      },
                      from
                      ) {
            }
            public ChoiceDiplomacy() { }

            public override BasicInformation Load() {
                DiplomaticStatus status = PlayerController.Instance.GetDiplomaticStatus(PlayerController.currentPlayerNumber, fromPlayer);
                return new ChoiceDiplomacy(PlayerController.Instance.GetPlayer(fromPlayer), status.NextHigherStatus, status);
            }
        }
        internal static ChoiceInformation CreateMoneyDemand(Player from, int amount) {
            return new ChoiceDemandMoney(from, amount);
        }
        internal static ChoiceInformation CreateAskDiplomaticIncrease(Player from, DiplomacyType askForStatus, DiplomaticStatus status) {
            return new ChoiceDiplomacy(from, askForStatus, status);
        }
    }
}

