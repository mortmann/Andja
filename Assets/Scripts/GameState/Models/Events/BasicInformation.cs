using Andja.UI;
using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public enum InformationType { UnitUnderAttack, StructureUnderAttack, DiplomacyChange, Praise, Denounce, ReceivedGift, 
                                  DemandMoney }

    public class BasicInformation {
        public InformationType Type;
        public Func<Vector2> GetPosition;
        public string[] DescriptionValues;
        public string[] TitleValues;
        TranslationData translation;
        public string SpriteName { get; internal set; }

        public BasicInformation(InformationType type, Func<Vector2> getPosition, 
                                    string[] titleValues, string[] descriptionValues,
                                    string spriteName) {
            Type = type;
            GetPosition = getPosition;
            DescriptionValues = descriptionValues;
            TitleValues = titleValues;
            SpriteName = spriteName;
        }
        public string GetTitle() {
            if (translation == null)
                return "MISSING Title";
            string text = translation.translation;
            for (int i = 0; i < TitleValues.Length; i++) {
                text = text.Replace("$" + i, TitleValues[i]);
            }
            return text;
        }
        public string GetDescription() {
            if (translation == null)
                return "MISSING Description";
            string text = translation.toolTipTranslation;
            for (int i = 0; i < DescriptionValues.Length; i++) {
                text = text.Replace("$" + i, DescriptionValues[i]);
            }
            return text;
        }
        public void OnLanguageChange() {
            translation = UILanguageController.Instance.GetTranslationData(Type);
        }

        public static BasicInformation CreateUnitDamage(Unit unit, IWarfare warfare) {
            if(warfare == null) {
                return new BasicInformation(InformationType.UnitUnderAttack,
                () => unit.CurrentPosition, 
                new string[] { unit.PlayerSetName }, 
                new string[] { unit.PlayerSetName },
                "Attacked"
                );
            }
            return new BasicInformation(InformationType.UnitUnderAttack,
                () => unit.CurrentPosition,
                new string[] { unit.PlayerSetName },
                new string[] { unit.PlayerSetName, PlayerController.GetPlayerName(warfare.PlayerNumber) },
                "Attacked"
                );
        }
        public static BasicInformation CreateStructureDamage(Structure Structure, IWarfare warfare) {
            if (warfare == null) {
                return new BasicInformation(InformationType.UnitUnderAttack,
                () => Structure.Center, 
                new string[] { Structure.Name }, 
                new string[] { Structure.Name },
                "Attacked"
                );
            }
            return new BasicInformation(InformationType.StructureUnderAttack,
                () => Structure.Center,
                new string[] { Structure.Name },
                new string[] { Structure.Name, PlayerController.GetPlayer(warfare.PlayerNumber).Name },
                "Attacked"
                );
        }
        public static BasicInformation DiplomacyChanged(DiplomaticStatus status) {
            Player one = PlayerController.GetPlayer(status.PlayerOne);
            Player two = PlayerController.GetPlayer(status.PlayerTwo);
            if (PlayerController.currentPlayerNumber == status.PlayerTwo) {
                one = two;
                two = PlayerController.GetPlayer(status.PlayerOne);
            }
            TranslationData td = UILanguageController.Instance.GetTranslationData(status.currentStatus);
            string[] first = new string[] { td.translation };
            string[] second = new string[] { one.Name, two.Name, td.translation };
            return new BasicInformation(InformationType.DiplomacyChange,
                () => two.GetMainCityPosition(),
                first,
                second,
                status.currentStatus.ToString()
                );
        }

        internal static BasicInformation CreatePraiseReceived(Player from) {
            string[] first = new string[] { };
            string[] second = new string[] { from.Name };
            return new BasicInformation(InformationType.Praise,
                () => from.GetMainCityPosition(),
                first,
                second,
                InformationType.Praise.ToString()
                );
        }

        internal static BasicInformation CreateDenounceReceived(Player from) {
            string[] first = new string[] { };
            string[] second = new string[] { from.Name };
            return new BasicInformation(InformationType.Denounce,
                () => from.GetMainCityPosition(),
                first,
                second,
                InformationType.Praise.ToString()
                );
        }
        internal static BasicInformation CreateMoneyReceived(Player from, int amount) {
            string[] first = new string[] { amount.ToString() };
            string[] second = new string[] { from.Name };
            return new BasicInformation(InformationType.Denounce,
                () => from.GetMainCityPosition(),
                first,
                second,
                InformationType.Praise.ToString()
                );
        }
    }

    public class ChoiceInformation : BasicInformation {
        public Choice[] Choices;
        public Action OnClose;
        public ChoiceInformation(InformationType type, Func<Vector2> getPosition,
                                    string[] titleValues, string[] descriptionValues,
                                    string spriteName, Choice[] choices) 
                                : base(type, getPosition,titleValues, descriptionValues,spriteName) {
            Choices = choices;
        }
        internal static ChoiceInformation CreateMoneyDemand(Player from, int amount, Action yes, Action no) {
            return new ChoiceInformation (
                InformationType.DemandMoney, 
                () => from.GetMainCityPosition(),
                new string[] { amount.ToString() },
                new string[] { from.Name },
                InformationType.DemandMoney.ToString(),
                new Choice[] {
                    new Choice(StaticLanguageVariables.Yes, yes),
                    new Choice(StaticLanguageVariables.No, no)
                }
            );
        }
        internal static ChoiceInformation CreateAskDiplomaticIncrease(Player from, DiplomacyType status, Action yes, Action no) {
            return new ChoiceInformation(
                InformationType.DiplomacyChange,
                () => from.GetMainCityPosition(),
                new string[] { },
                new string[] { from.Name },
                status.ToString(),
                new Choice[] {
                    new Choice(StaticLanguageVariables.Yes, yes),
                    new Choice(StaticLanguageVariables.No, no)
                }
            );
        }
    }
}

