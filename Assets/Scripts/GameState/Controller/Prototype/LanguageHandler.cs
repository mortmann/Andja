using Andja.Model;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using static Andja.Controller.PrototypController;

namespace Andja.Controller {
    public class LanguageHandler {
        private static readonly BindingFlags _flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static XmlFilesTypes _current;

        public static void ReloadLanguage() {
            foreach (XmlFilesTypes xml in Enum.GetValues(typeof(XmlFilesTypes))) {
                _current = xml;
                ReloadLanguageVariables(PrototypController.Instance.LoadXml(xml));
                ModLoader.LoadXMLs(xml, ReloadLanguageVariables);
            }
        }
        public static string ReplacePlaceHolders<T>(T data, string text) {
            if (text.Contains("$") == false)
                return text;
            string[] splits = text.Split('$');
            for (int i = 0; i < splits.Length - 1; i += 2) {
                string[] replaceSplit = splits[i + 1].Split(' ');
                string replace = replaceSplit[0];
                string replaceWith;
                if (replace.Contains('.')) {
                    string[] subgetsplits = replace.Split('.');
                    replaceWith = GetFieldString(data, 0, subgetsplits);
                }
                else {
                    replaceWith = GetFieldString(data, 0, replace);
                }
                replaceSplit[0] = replaceWith;
                splits[i + 1] = string.Join(" ", replaceSplit);
            }
            return string.Join("", splits);
        }


        private static string GetFieldString(object data, int index, params string[] fields) {
            Type dataType = data.GetType();
            if (typeof(IEnumerable).IsAssignableFrom(dataType)) {
                List<string> strings = (from object o in (IEnumerable)data select GetFieldString(o, index, fields)).ToList();
                if (strings.Count == 1)
                    return strings[0];
                string last = strings[strings.Count - 1];
                strings.RemoveAt(strings.Count - 1);
                return string.Join(", ", strings) + " " + GetLocalisedAnd() + " " + last;
            }
            if (fields.Length - 1 == index) {
                var field = dataType.GetField(fields[index], _flags)?.GetValue(data);
                if (field == null)
                    field = dataType.GetProperty(fields[index], _flags)?.GetValue(data);
                return field.ToString();
            }
            return GetFieldString(dataType.GetField(fields[index], _flags)?.GetValue(data), ++index, fields);
        }

        private static string GetLocalisedAnd() {
            return UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.And);
        }
        public static void ReloadLanguageVariables(string xml) {
            FieldInfo[] fields = typeof(LanguageVariables).GetFields();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNodeList nodeList = xmlDoc.ChildNodes;
            //have todo it for all three deep xml files
            if (_current == XmlFilesTypes.Structures) {
                nodeList = xmlDoc.FirstChild.ChildNodes;
            }
            foreach (XmlNode parent in nodeList) {
                foreach (XmlNode node in parent.ChildNodes) {
                    object data = null;
                    if (node.Attributes.Count == 0)
                        continue;
                    string id = node.Attributes?[0]?.InnerXml;
                    //if(id == null) {
                    //    id = node.Attributes.GetNamedItem("LEVEL")?.InnerXml;
                    //}
                    if (id == null)
                        continue;
                    switch (_current) {
                        case XmlFilesTypes.Other:
                            if (node.LocalName == "PopulationLevel")
                                data = Instance.PopulationLevelDatas[int.Parse(id)];
                            else
                                Debug.LogWarning("Read Language again one missing this type" + _current);
                            break;

                        case XmlFilesTypes.Events:
                            if (node.LocalName == "GameEvent")
                                data = Instance.GameEventPrototypeDatas[id];
                            if (node.LocalName == "Effect")
                                data = Instance.EffectPrototypeDatas[id];
                            break;

                        case XmlFilesTypes.Fertilities:
                            data = Instance.FertilityPrototypeDatas[id];
                            break;

                        case XmlFilesTypes.Items:
                            data = Instance.ItemPrototypeDatas[id];
                            break;

                        case XmlFilesTypes.Combat:
                            if (node.LocalName == "damageType")
                                data = Instance.DamageTypeDatas[id];
                            if (node.LocalName == "armorType")
                                data = Instance.ArmorTypeDatas[id];
                            break;

                        case XmlFilesTypes.Units:
                            if (node.LocalName == "worker")
                                continue;
                            data = Instance.UnitPrototypeDatas[id];
                            break;

                        case XmlFilesTypes.Structures:
                            data = Instance.StructurePrototypeDatas[id];
                            break;

                        case XmlFilesTypes.Needs:
                            if (node.LocalName == "need")
                                data = Instance.NeedPrototypeDatas[id];
                            if (node.LocalName == "needGroup")
                                data = Instance.NeedGroupDatas[id];
                            break;

                        case XmlFilesTypes.Startingloadouts:
                            break;

                        case XmlFilesTypes.Mapgeneration:
                            break;

                        default:
                            Debug.LogWarning("Read Language again missing this type" + _current);
                            return;
                    }
                    if (data == null)
                        continue;
                    foreach (FieldInfo fi in fields) {
                        XmlNode currentNode = node.SelectSingleNode(fi.Name);
                        XmlNode textNode = currentNode?.SelectSingleNode("entry[@lang='" + UILanguageController.selectedLanguage + "']");
                        if (textNode != null) {
                            string text = ReplacePlaceHolders(data, textNode.InnerXml);
                            fi.SetValue(data, Convert.ChangeType(text, fi.FieldType));
                        }
                    }
                }
            }
        }
    }
}