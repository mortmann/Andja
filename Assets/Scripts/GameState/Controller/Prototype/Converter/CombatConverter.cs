using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
namespace Andja.Controller {

    public class CombatConverter {
        private readonly Dictionary<string, ArmorType> idToArmorType;
        private readonly Dictionary<string, DamageType> idToDamageType;
        private readonly BaseConverter<ArmorType> armorConverter;
        private readonly BaseConverter<DamageType> damageConverter;

        public CombatConverter(Dictionary<string, ArmorType> idToArmorType, Dictionary<string, DamageType> idToDamageType) {
            this.idToArmorType = idToArmorType;
            this.idToDamageType = idToDamageType;
            armorConverter = new BaseConverter<ArmorType>(
                (id) => new ArmorType() { ID = id },
                "combatTypes/armorType",
                (id, data) => idToArmorType[id] = data
                );
            damageConverter = new BaseConverter<DamageType>(
                (id) => new DamageType() { ID = id },
                "combatTypes/damageType",
                (id, data) => idToDamageType[id] = data
                );
        }

        public void ReadFromFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent); // load the file.
            armorConverter.ReadFile(xmlDoc);
            damageConverter.ReadFile(xmlDoc);

            //XmlNodeList listDamageType = xmlDoc.SelectNodes("combatTypes/damageType");
            //if (listDamageType != null) {
            //    foreach (XmlElement node in listDamageType) {
            //        DamageType at = new DamageType();
            //        string id = node.GetAttribute("ID");
            //        at.ID = id;
            //        SetData<DamageType>(node, ref at);
            //        XmlNode dict = node.SelectSingleNode("damageMultiplier");
            //        at.damageMultiplier = new Dictionary<ArmorType, float>();
            //        foreach (XmlElement child in dict.ChildNodes) {
            //            string armorID = child.GetAttribute("ArmorTyp");
            //            if (string.IsNullOrEmpty(armorID))
            //                continue;
            //            if (float.TryParse(child.InnerText, out float multiplier) == false) {
            //                Debug.LogError("ID is not an float for ArmorType ");
            //            }
            //            at.damageMultiplier[_armorTypeDatas[armorID]] = multiplier;
            //        }
            //        _damageTypeDatas[id] = at;
            //    }
            //}
        }

    }
}