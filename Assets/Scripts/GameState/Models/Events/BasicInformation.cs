using Andja.UI;
using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public enum InformationType { UnitUnderAttack, StructureUnderAttack, DiplomacyChange, }

    public class BasicInformation {
        public InformationType Type;
        public Func<Vector2> GetPosition;
        public string[] DescriptionValues;
        public string[] TitleValues;
        TranslationData translation;
        Action LanguageChanged;
        public string SpriteName { get; internal set; }

        public BasicInformation(InformationType type, Func<Vector2> getPosition, 
                                    string[] titleValues, string[] descriptionValues,
                                    string spriteName, Action languageChanged) {
            Type = type;
            GetPosition = getPosition;
            DescriptionValues = descriptionValues;
            TitleValues = titleValues;
            LanguageChanged = languageChanged;
            SpriteName = spriteName;
        }
        public string GetTitle() {
            string text = translation.translation;
            for (int i = 0; i < TitleValues.Length; i++) {
                text = text.Replace("$" + i, TitleValues[i]);
            }
            return text;
        }
        public string GetDescription() {
            string text = translation.toolTipTranslation;
            for (int i = 0; i < DescriptionValues.Length; i++) {
                text = text.Replace("$" + i, DescriptionValues[i]);
            }
            return text;
        }
        public void OnLanguageChange() {
            translation = UILanguageController.Instance.GetTranslationData(Type);
            LanguageChanged?.Invoke();
        }

        public static BasicInformation CreateUnitDamage(Unit unit, IWarfare warfare) {
            if(warfare == null) {
                return new BasicInformation(InformationType.UnitUnderAttack,
                () => unit.CurrentPosition, 
                new string[] { unit.PlayerSetName }, 
                new string[] { unit.PlayerSetName },
                "Attacked",
                null
                );
            }
            return new BasicInformation(InformationType.UnitUnderAttack,
                () => unit.CurrentPosition,
                new string[] { unit.PlayerSetName },
                new string[] { unit.PlayerSetName, PlayerController.GetPlayerName(warfare.PlayerNumber) },
                "Attacked",
                null
                );
        }
        public static BasicInformation CreateStructureDamage(Structure Structure, IWarfare warfare) {
            if (warfare == null) {
                return new BasicInformation(InformationType.UnitUnderAttack,
                () => Structure.Center, 
                new string[] { Structure.Name }, 
                new string[] { Structure.Name },
                "Attacked",
                null
                );
            }
            return new BasicInformation(InformationType.StructureUnderAttack,
                () => Structure.Center,
                new string[] { Structure.Name },
                new string[] { Structure.Name, PlayerController.GetPlayer(warfare.PlayerNumber).Name },
                "Attacked",
                null
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
                status.currentStatus.ToString(),
                () => {
                    TranslationData td = UILanguageController.Instance.GetTranslationData(status.currentStatus);
                    first[0] = td.translation;
                    second[2] = td.translation;
                    }
                );
        }
    }
}

