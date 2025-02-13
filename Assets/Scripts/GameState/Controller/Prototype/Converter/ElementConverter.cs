using System;
using System.Reflection;
using System.Xml;
using Andja.Model;
using GameState.Models.Elements;
using UnityEngine;

namespace GameState.Controller.Prototype.Converter {
    public class ElementConverter {
        public static ElementData ConvertToData(XmlNode child) {
            switch (child.Attributes["type"].Value) {
                case "Capturable":
                    return SetFields(new CapturablePrototypeData(), child);
                default:
                    Debug.LogError("Unknown element type");
                    return null;
            }
        }

        private static ElementData SetFields(ElementData data, XmlNode node) {
            FieldInfo[] fieldInfos = data.GetType().GetFields();
            foreach (FieldInfo fieldInfo in fieldInfos) {
                try {
                    XmlNode currentNode = node.SelectSingleNode(fieldInfo.Name);
                    fieldInfo.SetValue(data, Convert.ChangeType(currentNode.InnerXml, fieldInfo.FieldType, System.Globalization.CultureInfo.InvariantCulture));
                }
                catch {
                    Debug.Log(data + " -> " + fieldInfo.Name + " is faulty!");
                }
            }

            return data;
        }
    }
}
