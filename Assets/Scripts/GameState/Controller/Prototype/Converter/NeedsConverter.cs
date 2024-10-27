using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Controller {

    public class NeedsConverter  {
        private readonly BaseConverter<NeedGroupPrototypeData> needGroupConverter;
        private readonly BaseConverter<NeedPrototypeData> needConverter;

        public NeedsConverter(Dictionary<string, NeedGroup> idToNeedGroup, Dictionary<string, NeedGroupPrototypeData> idToNeedGroupData,
            List<Need> allNeeds, Dictionary<string, NeedPrototypeData> idToNeedData) {
            needGroupConverter = new BaseConverter<NeedGroupPrototypeData>(
                (id) => new NeedGroupPrototypeData() { ID = id },
                "needs/NeedGroup",
                (id, data) => {
                    idToNeedGroupData[id] = data;
                    idToNeedGroup[id] = new NeedGroup(id);
                });
            needConverter = new BaseConverter<NeedPrototypeData>(
                (id) => new NeedPrototypeData(),
                "needs/Need",
                (id, data) => {
                    if (data.item == null && data.structures == null)
                        return;
                    if (data.structures != null) {
                        foreach (NeedStructure str in data.structures) {
                            if (data.startLevel > str.PopulationLevel) {
                                data.startLevel = str.PopulationLevel;
                            }
                            if (data.startLevel != str.PopulationLevel) continue;
                            if (data.startPopulationCount > str.PopulationCount) {
                                data.startPopulationCount = str.PopulationCount;
                            }
                        }
                    }
                    idToNeedData[id] = data;
                    allNeeds.Add(new Need(id, data));
                    if (idToNeedGroupData.ContainsKey(data.group.ID))
                        idToNeedGroup[data.group.ID].AddNeed(new Need(id, data));
                });
        }

        public void ReadFromFile(string fileContent) {
            needGroupConverter.ReadFile(fileContent);
            needConverter.ReadFile(fileContent);
        }
    }
}