using Andja.Model;
using Andja.Model.Generator;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
namespace Andja.Controller {

    public class MapGenerationConverter {
        private readonly BaseConverter<IslandSizeGenerationInfo> IslandSizeConverter;
        private readonly BaseConverter<IslandFeaturePrototypeData> IslandFeatureConverter;
        private readonly BaseConverter<SpawnStructureGenerationInfo> spawnStructureConverter;
        private readonly BaseConverter<ResourceGenerationInfo> resourceConverter;
        private readonly Dictionary<Size, IslandSizeGenerationInfo> IslandSizeToGenerationInfo;

        public MapGenerationConverter(Dictionary<Climate, List<SpawnStructureGenerationInfo>> spawnStructureGeneration,
            Dictionary<Size, IslandSizeGenerationInfo> islandSizeToGenerationInfo,
            Dictionary<string, IslandFeaturePrototypeData> islandFeaturePrototypeDatas,
            List<string> allNaturalSpawningStructureIDs,
            Dictionary<Climate, List<ResourceGenerationInfo>> climateToResourceGeneration,
            List<ResourceGenerationInfo> resourceGenerations
            ) {
            IslandSizeConverter = new BaseConverter<IslandSizeGenerationInfo>(
                (_) => new IslandSizeGenerationInfo(),
                "generationInfos/islandSizes/islandSize",
                (sizeString, data) => { Enum.TryParse(sizeString, true, out Size size);
                    islandSizeToGenerationInfo[size] = data;
                }) { AttributeKey = "size" };

            IslandFeatureConverter = new BaseConverter<IslandFeaturePrototypeData>(
                (id) => new IslandFeaturePrototypeData() { ID = id},
                "generationInfos/islandFeatures/islandFeature",
                (id, data) => islandFeaturePrototypeDatas[id] = data);

            spawnStructureConverter = new BaseConverter<SpawnStructureGenerationInfo>(
                (id) => new SpawnStructureGenerationInfo() { ID = id },
                "generationInfos/structures/structure",
                (id, data) => {
                    if (data.climate != null) {
                        foreach (Climate c in data.climate) {
                            spawnStructureGeneration[c].Add(data);
                        }
                    }
                    else {
                        foreach (Climate c in Enum.GetValues(typeof(Climate))) {
                            spawnStructureGeneration[c].Add(data);
                        }
                    }
                    if (data.structureType == StructureType.Natural) {
                        allNaturalSpawningStructureIDs.Add(data.ID);
                    }
                });
            resourceConverter = new BaseConverter<ResourceGenerationInfo>(
                (id) => new ResourceGenerationInfo() { ID = id },
                "generationInfos/resources/resource",
                (id, data) => {
                    resourceGenerations.Add(data);
                    foreach (Climate c in data.climate) {
                        climateToResourceGeneration[c].Add(data);
                    }
                }, AdditionalResourceRead);
            IslandSizeToGenerationInfo = islandSizeToGenerationInfo;
        }
        public void ReadFromFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent); // load the file.
            IslandSizeConverter.ReadFile(xmlDoc);
            IslandFeatureConverter.ReadFile(xmlDoc);
            spawnStructureConverter.ReadFile(xmlDoc);
            
        }
        private void AdditionalResourceRead(ResourceGenerationInfo generationInfo, XmlNode node) {
            generationInfo.resourceRange = new Dictionary<Size, Range>();
            foreach (XmlElement child in node["distributionMap"].ChildNodes) {
                string sizeS = child.GetAttribute("islandSize");
                Enum.TryParse(sizeS, true, out Size size);
                Range range = new Range(child["range"]["lower"].GetIntValue(), child["range"]["upper"].GetIntValue());
                generationInfo.resourceRange[size] = range;
                if (range.upper > 0) {
                    IslandSizeToGenerationInfo[size].resourceGenerationsInfo.Add(generationInfo);
                }
                generationInfo.climate ??= (Climate[])Enum.GetValues(typeof(Climate));
            }
            
        }
    } 
}