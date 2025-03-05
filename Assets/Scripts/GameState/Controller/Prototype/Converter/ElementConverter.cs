using System;
using System.Reflection;
using System.Xml;
using Andja.Controller;
using Andja.Model;
using UnityEngine;

namespace GameState.Controller.Prototype.Converter {
    public class ElementConverter {
        public static ElementData ConvertToData(XmlNode child) {
            switch (child.Attributes["type"].Value) {
                case "Capturable":
                    return SetFields(new CapturablePrototypeData(), child);
                case "Capturer":
                    return SetFields(new CapturerPrototypeData(), child);
                case "Attack":
                    return SetFields(new AttackPrototypeData(), child);
                case "ShipAttack":
                    return SetFields(new ShipAttackPrototypeData(), child);
                case "Target":
                    return SetFields(new TargetPrototypeData(), child);
                default:
                    Debug.LogError("Unknown element type");
                    return null;
            }
        }

        private static T SetFields<T>(T data, XmlNode node) {
            BaseConverter<T>.SetData(node, null, ref data);
            return data;
        }
    }
}
