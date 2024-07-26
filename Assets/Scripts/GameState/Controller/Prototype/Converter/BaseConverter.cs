using Andja.Model;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEngine;
using System.Linq;
using Range = Andja.Utility.Range;

namespace Andja.Controller { 

    public class BaseConverter<T> {
        public string AttributeKey = "ID";
        public string Selector { get; private set; }
        public Action<string, T> AddPrototype { get; }
        public Action<string, T> AddToDictionaries { get; }

        readonly Func<string, T> CreatePrototypeDataInstance;
        private readonly Action<T, XmlNode> AdditionalRead;

        public BaseConverter(Func<string, T> createPrototypDataInstance, string selector, Action<string, T> addToDictionaries, 
            Action<T, XmlNode> additionalRead = null) {
            CreatePrototypeDataInstance = createPrototypDataInstance;
            Selector = selector;
            AddToDictionaries = addToDictionaries;
            AdditionalRead = additionalRead;
        }

        public virtual void ReadFile(XmlDocument xmlDoc) {
            foreach (XmlElement node in xmlDoc.SelectNodes(Selector)) {
                string ID = node.GetAttribute(AttributeKey);
                T prototypeData = CreatePrototypeDataInstance(ID);
                SetData(node, ID, ref prototypeData);
                AddToDictionaries(ID, prototypeData);
                AdditionalRead?.Invoke(prototypeData, node);
            }
        }

        public void ReadFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent); // load the file.
            ReadFile(xmlDoc);
        }

        private void SetData(XmlElement node, string T_ID, ref T data) {
            FieldInfo[] fields = typeof(T).GetFields();
            HashSet<string> langs = new HashSet<string>();
            if (typeof(LanguageVariables).IsAssignableFrom(typeof(T))) {
                foreach (FieldInfo f in typeof(LanguageVariables).GetFields()) {
                    langs.Add(f.Name);
                }
            }
            foreach (FieldInfo fi in fields) {
                if (fi.Name == "ID") {
                    fi.SetValue(data, T_ID);
                    continue;
                }
                if (Attribute.IsDefined(fi, typeof(IgnoreAttribute)))
                    continue;
                XmlNode currentNode = node.SelectSingleNode(fi.Name);
                if (langs.Contains(fi.Name)) {
                    if (currentNode == null) {
                        //TODO activate this warning when all data is correctly created
                        //				Debug.LogWarning (fi.Name + " selected language not avaible!");
                        continue;
                    }
                    XmlNode textNode = currentNode.SelectSingleNode("entry[@lang='" + UILanguageController.selectedLanguage.ToString() + "']");
                    if (textNode != null) {
                        string text = LanguageHandler.ReplacePlaceHolders(data, textNode.InnerXml);
                        fi.SetValue(data, Convert.ChangeType(text, fi.FieldType));
                    }
                    continue;
                }
                if (currentNode == null) continue;
                if (fi.FieldType == typeof(Item)) {
                    fi.SetValue(data, NodeToItem(currentNode));
                    continue;
                }
                if (fi.FieldType == typeof(Item[])) {
                    fi.SetValue(data, (from XmlNode item in currentNode.ChildNodes select NodeToItem(item)).ToArray());
                    continue;
                }
                if (fi.FieldType.IsSubclassOf(typeof(Structure))) {
                    fi.SetValue(data, NodeToStructure(currentNode));
                    continue;
                }
                if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsSubclassOf(typeof(Structure))
                    || fi.FieldType == (typeof(Structure[]))) {
                    Array items = (Array)Activator.CreateInstance(fi.FieldType, currentNode.ChildNodes.Count);
                    for (int i = 0; i < currentNode.ChildNodes.Count; i++) {
                        items.SetValue(NodeToStructure(currentNode.ChildNodes[i]), i);
                    }
                    fi.SetValue(data, items);
                    continue;
                }
                if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsSubclassOf(typeof(string))
                    || fi.FieldType == (typeof(string[]))) {
                    Array items = (Array)Activator.CreateInstance(fi.FieldType, currentNode.ChildNodes.Count);
                    for (int i = 0; i < currentNode.ChildNodes.Count; i++) {
                        items.SetValue(currentNode.ChildNodes[i].InnerText, i);
                    }
                    fi.SetValue(data, items);
                    continue;
                }
                if (fi.FieldType == typeof(NeedGroupPrototypeData)) {
                    fi.SetValue(data, NodeToNeedGroupPrototypData(currentNode));
                    continue;
                }
                if (fi.FieldType == typeof(ArmorType)) {
                    fi.SetValue(data, NodeToArmorType(currentNode));
                    continue;
                }
                if (fi.FieldType == typeof(DamageType)) {
                    fi.SetValue(data, NodeToDamageType(currentNode));
                    continue;
                }
                if (fi.FieldType == typeof(Fertility)) {
                    fi.SetValue(data, NodeToFertility(currentNode));
                    continue;
                }
                if (fi.FieldType.IsSubclassOf(typeof(Unit))) {
                    fi.SetValue(data, NodeToUnit(currentNode));
                    continue;
                }
                if (fi.FieldType.IsSubclassOf(typeof(Unit[])) || fi.FieldType == (typeof(Unit[]))) {
                    List<Unit> items = new List<Unit>();
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        items.Add(NodeToUnit(item));
                    }
                    fi.SetValue(data, items.ToArray());
                    continue;
                }
                if (fi.FieldType == (typeof(Effect[])) || fi.FieldType == (typeof(IEffect[]))) {
                    fi.SetValue(data, (from XmlNode item in currentNode.ChildNodes select NodeToEffect(item)).ToArray());
                    continue;
                }
                if (fi.FieldType == (typeof(float[]))) {
                    List<float> items = new List<float>(currentNode.ChildNodes.Count);
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        int id = int.Parse(item.Attributes[0].InnerXml);
                        items.Insert(id, float.Parse(item.InnerXml));
                    }
                    fi.SetValue(data, items.ToArray());
                    continue;
                }
                if (fi.FieldType.IsEnum) {
                    fi.SetValue(data, Enum.Parse(fi.FieldType, currentNode.InnerXml, true));
                    continue;
                }
                if (fi.FieldType.IsArray && fi.FieldType.GetArrayRank() == 1 && fi.FieldType.GetElementType().IsEnum) {
                    var listType = typeof(List<>);
                    var constructedListType = listType.MakeGenericType(fi.FieldType.GetElementType());
                    var list = (IList)Activator.CreateInstance(constructedListType);
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        if (item.Name != fi.FieldType.GetElementType().Name) {
                            continue;
                        }
                        if (fi.DeclaringType == typeof(TileType)) {
                            if (item.Name == "BuildLand") { // shortcut to make it easy to include all buildable land
                                foreach (TileType tt in Tile.BuildLand)
                                    list.Add(tt);
                                continue;
                            }
                        }
                        list.Add(Enum.Parse(fi.FieldType.GetElementType(), item.InnerXml, true));
                    }
                    Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), list.Count);
                    list.CopyTo(enumArray, 0);
                    fi.SetValue(data, Convert.ChangeType(enumArray, fi.FieldType));
                    continue;
                }
                if (fi.FieldType.IsArray && fi.FieldType.GetArrayRank() == 2 && fi.FieldType.GetElementType() == typeof(TileType?)) {
                    if (int.TryParse(currentNode.Attributes.GetNamedItem("length").Value, out var firstLength) == false) {
                        continue;
                    }
                    int secondLength = 0;
                    var listType = typeof(List<>);
                    var constructedfirstListType = listType.MakeGenericType(fi.FieldType.GetElementType().MakeArrayType());
                    var firstlist = (IList)Activator.CreateInstance(constructedfirstListType);
                    foreach (XmlNode item in currentNode.ChildNodes) {
                        XmlNode len2 = item.Attributes.GetNamedItem("length");
                        if (len2 == null || int.TryParse(len2.Value, out secondLength) == false) {
                            continue;
                        }
                        var constructedSecondListType = listType.MakeGenericType(fi.FieldType.GetElementType());
                        var secondlist = (IList)Activator.CreateInstance(constructedSecondListType);
                        string[] singleValues = item.InnerXml.Split(',');
                        if (singleValues.Length < secondLength) {
                            continue;
                        }
                        foreach (string single in singleValues) {
                            //own try parse because in needs non nullable
                            try {
                                secondlist.Add(Enum.Parse(typeof(TileType), single.Trim(), true));
                            }
                            catch {
                                secondlist.Add(null);
                            }
                        }
                        Array enumArray = Array.CreateInstance(fi.FieldType.GetElementType(), secondLength);
                        secondlist.CopyTo(enumArray, 0);
                        firstlist.Add(enumArray);
                    }
                    Array twoDimEnumArray = Array.CreateInstance(fi.FieldType.GetElementType(), firstLength, secondLength);
                    for (int i = 0; i < firstlist.Count; i++) {
                        for (int j = 0; j < ((TileType?[])firstlist[i]).Length; j++) {
                            twoDimEnumArray.SetValue(((TileType?[])firstlist[i])[j], i, j);
                        }
                    }
                    fi.SetValue(data, Convert.ChangeType(twoDimEnumArray, fi.FieldType));
                    continue;
                }
                if (fi.FieldType == typeof(Dictionary<ArmorType, float>)) {
                    // this will get set in load xml directly and not here!
                    continue;
                }
                if (fi.FieldType == typeof(Range)) {
                    fi.SetValue(data, new Range(currentNode["lower"].GetIntValue(), currentNode["upper"].GetIntValue()));
                    continue;
                }
                if (fi.FieldType == typeof(Dictionary<Target, List<int>>)) {
                    Dictionary<Target, List<int>> range = new Dictionary<Target, List<int>>();
                    foreach (XmlNode child in currentNode.ChildNodes) {
                        Target target = Target.World;
                        if (child.Attributes[0] == null)
                            continue;
                        if (Enum.TryParse<Target>(child.Attributes[0].InnerXml, true, out target) == false)
                            continue;
                        string[] ids = child.InnerXml.Split(',');
                        if (ids.Length == 0) {
                            continue;
                        }
                        range.Add(target, new List<int>());
                        foreach (string stringid in ids) {
                            int.TryParse(stringid, out int id);
                            if (id == -1)
                                continue;
                            range[target].Add(id);
                        }
                    }
                    //clean up empty target groups
                    List<Target> targets = new List<Target>(range.Keys);
                    targets.RemoveAll(t => range[t].Count == 0);
                    //only if it has stuff we need to set it
                    if (range.Count > 0)
                        fi.SetValue(data, range);
                    continue;
                }
                if (fi.FieldType == typeof(TargetGroup)) {
                    List<Target> targets = new List<Target>();
                    foreach (XmlNode child in currentNode.ChildNodes) {
                        if (Enum.TryParse(child.InnerXml, true, out Target target) == false)
                            continue;
                        targets.Add(target);
                    }
                    fi.SetValue(data, new TargetGroup(targets));
                    continue;
                }
                if (fi.FieldType == typeof(Dictionary<Climate, string[]>)) {
                    Dictionary<Climate, string[]> climToString = new Dictionary<Climate, string[]>();
                    foreach (XmlNode child in currentNode.ChildNodes) {
                        if (Enum.TryParse(child.Attributes[0].InnerXml, true, out Climate climate) == false)
                            continue;
                        climToString[climate] = child.InnerXml.Split(';');
                    }
                    fi.SetValue(data, climToString);
                    continue;
                }
                try {
                    fi.SetValue(data, Convert.ChangeType(currentNode.InnerXml, fi.FieldType, System.Globalization.CultureInfo.InvariantCulture));
                }
                catch {
                    Debug.Log(data + " -> " + fi.Name + " is faulty!");
                }
            }
        }

        private Effect NodeToEffect(XmlNode item) {
            string id = item.InnerXml;
            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.EffectPrototypeDatas.ContainsKey(id)) return new Effect(id);
            Debug.LogError("ID was not created before the depending DamageType! " + id);
            return null;
        }

        private object NodeToDamageType(XmlNode n) {
            string id = n.InnerXml;

            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.DamageTypeDatas.ContainsKey(id)) return PrototypController.Instance.DamageTypeDatas[id];
            Debug.LogError("ID was not created before the depending DamageType! " + id);
            return null;
        }

        private object NodeToNeedGroupPrototypData(XmlNode n) {
            string id = n.InnerXml;

            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.NeedGroupDatas.ContainsKey(id)) 
                return PrototypController.Instance.NeedGroupDatas[id];
            Debug.LogError("ID was not created before the depending NeedGroup! " + id);
            return null;
        }

        private object NodeToArmorType(XmlNode n) {
            string id = n.InnerXml;

            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.ArmorTypeDatas.ContainsKey(id)) return PrototypController.Instance.ArmorTypeDatas[id];
            Debug.LogError("ID was not created before the depending ArmorType! " + id);
            return null;
        }

        private Item NodeToItem(XmlNode n) {
            string id = n.Attributes["ID"].Value;
            if (PrototypController.Instance.AllItems.ContainsKey(id) == false) {
                Debug.LogError("ITEM ID was not created! " + id + " (" + n.ParentNode.Name + ")");
                return null;
            }
            Item clone = PrototypController.Instance.AllItems[id].Clone();
            if (n.SelectSingleNode("count") == null) return clone;
            if (int.TryParse(n.SelectSingleNode("count").InnerXml, out int count) == false) {
                Debug.LogError("Count is not an int");
                return null;
            }
            clone.count = Mathf.Abs(count);
            return clone;
        }

        private Unit NodeToUnit(XmlNode n) {
            string id = n.InnerXml;
            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }
            if (PrototypController.Instance.UnitPrototypes.ContainsKey(id)) return PrototypController.Instance.UnitPrototypes[id];
            Debug.LogError("ID was not created before the depending Unit! " + id);
            return null;
        }

        private Structure NodeToStructure(XmlNode n) {
            string id = n.InnerText;
            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.StructurePrototypes.ContainsKey(id)) return PrototypController.Instance.StructurePrototypes[id];
            Debug.LogError("ID was not created before the depending Structure! " + id);
            return null;
        }

        private Fertility NodeToFertility(XmlNode n) {
            string id = n.InnerXml;
            if (string.IsNullOrEmpty(id)) {
                return null;//not needed
            }

            if (PrototypController.Instance.IdToFertilities.ContainsKey(id)) return PrototypController.Instance.IdToFertilities[id];
            Debug.LogError("ID was not created before the depending Fertility! " + id);
            return null;
        }

    }
}