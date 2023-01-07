using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Controller {

    public class NeedsConverter  {
        private readonly BaseConverter<NeedGroupPrototypeData> needGroupConverter;
        private readonly BaseConverter<NeedPrototypeData> needConverter;

        public NeedsConverter(Dictionary<string, NeedGroup> idToNeedGroup, Dictionary<string, NeedGroupPrototypeData> idToNeedGroupData,
            List<Need> idToNeed, Dictionary<string, NeedPrototypeData> idToNeedData) {
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
                    idToNeedData[id] = data;
                    idToNeed.Add(new Need(id, data));
                });
            //if (npd.item == null && npd.structures == null)
            //    continue;
            //if (npd.structures != null) {
            //    foreach (NeedStructure str in npd.structures) {
            //        if (npd.startLevel > str.PopulationLevel) {
            //            npd.startLevel = str.PopulationLevel;
            //        }
            //        if (npd.startLevel != str.PopulationLevel) continue;
            //        if (npd.startPopulationCount > str.PopulationCount) {
            //            npd.startPopulationCount = str.PopulationCount;
            //        }
            //    }
            //}
            //Need n = new Need(ID, npd);
            //if (_idToNeedGroup.ContainsKey(n.Group.ID))
            //    _idToNeedGroup[n.Group.ID].AddNeed(n.Clone());
            //_allNeeds.Add(n);
            //if (levelToNeedList.ContainsKey(npd.startLevel) == false) {
            //    levelToNeedList[npd.startLevel] = new List<Need>();
            //}

            //levelToNeedList[npd.startLevel].Add(n.Clone());
        }

        public void ReadFromFile(string fileContent) {
            needGroupConverter.ReadFile(fileContent);
            needConverter.ReadFile(fileContent);
        }
    }
}