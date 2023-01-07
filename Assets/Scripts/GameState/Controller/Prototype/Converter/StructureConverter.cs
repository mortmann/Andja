using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Andja.Model;
using UnityEngine;
using System.Linq;

namespace Andja.Controller {
    public class StructureConverter {
        private readonly BaseConverter<RoadStructurePrototypeData> roadConverter;
        private readonly BaseConverter<GrowablePrototypeData> growablesConverter;
        private readonly BaseConverter<FarmPrototypeData> farmsConverter;
        private readonly BaseConverter<ProductionPrototypeData> productionConverter;
        private readonly BaseConverter<NeedStructurePrototypeData> needStructureConverter;
        private readonly BaseConverter<MinePrototypeData> mineConverter;
        private readonly BaseConverter<HomePrototypeData> homeConverter;
        private readonly BaseConverter<MarketPrototypeData> marketConverter;
        private readonly BaseConverter<WarehousePrototypData> warehouseConverter;
        private readonly BaseConverter<MilitaryPrototypeData> militaryConverter;
        private readonly BaseConverter<ServiceStructurePrototypeData> serviceConverter;

        public StructureConverter(
            Dictionary<string, Structure> idToStructure,
            Dictionary<string, StructurePrototypeData> idToPrototypData) {

            roadConverter = new BaseConverter<RoadStructurePrototypeData>(
                (_) => new RoadStructurePrototypeData(),
                "structures/roads/road",
                (id, data) => {
                    idToStructure[id] = new RoadStructure(id, data);
                    idToPrototypData[id] = data;
                });
            growablesConverter = new BaseConverter<GrowablePrototypeData>(
                (_) => new GrowablePrototypeData(),
                "structures/growables/growable",
                (id, data) => {
                    idToStructure[id] = new GrowableStructure(id, data);
                    idToPrototypData[id] = data;
                });
            farmsConverter = new BaseConverter<FarmPrototypeData>(
                (_) => new FarmPrototypeData(),
                "structures/farms/farm",
                (id, data) => {
                    idToStructure[id] = new FarmStructure(id, data);
                    idToPrototypData[id] = data;
                    if (data.growable.ID == "farmland") {
                        //for now hardcoded. maybe gonna change this
                        //but this is just the "empty" setting for growable
                        data.growable = null;
                    }
                    if (data.output != null && data.output.Length > 0 && data.output[0].count == 0) {
                        data.output[0].count = 1;
                    }
                });
            productionConverter = new BaseConverter<ProductionPrototypeData>(
                (_) => new ProductionPrototypeData(),
                "structures/productions/production",
                (id, data) => {
                    idToStructure[id] = new ProductionStructure(id, data);
                    idToPrototypData[id] = data;
                });
            needStructureConverter = new BaseConverter<NeedStructurePrototypeData>(
                (_) => new NeedStructurePrototypeData(),
                "structures/needstructures/needstructure",
                (id, data) => {
                    idToStructure[id] = new NeedStructure(id, data);
                    idToPrototypData[id] = data;
                });
            mineConverter = new BaseConverter<MinePrototypeData>(
                (_) => new MinePrototypeData(),
                "structures/mines/mine",
                (id, data) => {
                    idToStructure[id] = new MineStructure(id, data);
                    idToPrototypData[id] = data;
                });
            homeConverter = new BaseConverter<HomePrototypeData>(
                (_) => new HomePrototypeData(),
                "structures/homes/home",
                (id, data) => {
                    HomeStructure home = new HomeStructure(id, data);
                    idToStructure[id] = home;
                    idToPrototypData[id] = data;
                    PrototypController.Instance.PopulationLevelDatas[data.populationLevel].HomeStructure = home;
                });
            marketConverter = new BaseConverter<MarketPrototypeData>(
                (_) => new MarketPrototypeData(),
                "structures/markets/market",
                (id, data) => {
                    idToStructure[id] = new MarketStructure(id, data);
                    idToPrototypData[id] = data;
                });
            warehouseConverter = new BaseConverter<WarehousePrototypData>(
                (_) => new WarehousePrototypData(),
                "structures/warehouses/warehouse",
                (id, data) => {
                    idToStructure[id] = new WarehouseStructure(id, data);
                    idToPrototypData[id] = data;
                });
            militaryConverter = new BaseConverter<MilitaryPrototypeData>(
                (_) => new MilitaryPrototypeData(),
                "structures/militarystructures/militarystructure",
                (id, data) => {
                    idToStructure[id] = new MilitaryStructure(id, data);
                    idToPrototypData[id] = data;
                    foreach (Unit u in data.canBeBuildUnits) {
                        if (u.IsShip) {
                            data.canBuildShips = true;
                        }
                    }
                });
            serviceConverter = new BaseConverter<ServiceStructurePrototypeData>(
                (_) => new ServiceStructurePrototypeData(),
                "structures/servicestructures/servicestructure",
                (id, data) => {
                    idToStructure[id] = new ServiceStructure(id, data);
                    idToPrototypData[id] = data;
                }, AdditionalServiceStructureRead);
        }


        public void ReadFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent); // load the file.
            roadConverter.ReadFile(xmlDoc);
            growablesConverter.ReadFile(xmlDoc);
            farmsConverter.ReadFile(xmlDoc);
            productionConverter.ReadFile(xmlDoc);
            needStructureConverter.ReadFile(xmlDoc);
            mineConverter.ReadFile(xmlDoc);
            homeConverter.ReadFile(xmlDoc);
            marketConverter.ReadFile(xmlDoc);
            warehouseConverter.ReadFile(xmlDoc);
            militaryConverter.ReadFile(xmlDoc);
            serviceConverter.ReadFile(xmlDoc);
        }

        protected void AdditionalServiceStructureRead(ServiceStructurePrototypeData sspd, XmlNode node) {
            if (sspd.effectsOnTargets != null)
                foreach (Effect effect in sspd.effectsOnTargets) {
                    effect.Serialize = false;
                }
            //Important is that we set the usageItem count to more than the usage amount
            //(for the case it is more than one ton
            if (node.SelectSingleNode("Usages") != null) {
                var nodes = node.SelectSingleNode("Usages").SelectNodes("entry");
                List<float> usages = new List<float>();
                List<Item> items = new List<Item>();
                for (int i = 0; i < nodes.Count; i++) {
                    XmlNode child = nodes.Item(i);
                    var attribute = child.Attributes["Item"];
                    if (attribute == null || attribute.Value == null)
                        continue;
                    if (PrototypController.Instance.AllItems.ContainsKey(attribute.Value) == false)
                        continue;
                    if (float.TryParse(child.InnerText, out float usage) == false)
                        continue;
                    if (usage <= 0)
                        continue;
                    usages.Add(usage);
                    Item item = new Item(attribute.Value) {
                        count = Mathf.Clamp(Mathf.CeilToInt(usage), 1, 100)
                    };
                    items.Add(item);
                }
                sspd.usagePerTick = usages.ToArray();
                sspd.usageItems = items.ToArray();
            }
        }
    }
}