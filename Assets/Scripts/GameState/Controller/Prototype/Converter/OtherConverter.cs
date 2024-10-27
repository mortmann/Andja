using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Controller {

    public class OtherConverter {
        private BaseConverter<PopulationLevelPrototypData> PopulationLevelConverter;

        public OtherConverter(Dictionary<int, PopulationLevelPrototypData> populationLevelDatas) {
            PopulationLevelConverter = new BaseConverter<PopulationLevelPrototypData>(
                (_) => new PopulationLevelPrototypData(),
                "Other/PopulationLevels/PopulationLevel",
                (levelString, data) => {
                    data.LEVEL = int.Parse(levelString);
                    populationLevelDatas[data.LEVEL] = data;
                }) { AttributeKey = "LEVEL" };
        }

        public void ReadFromFile(string fileContent) {
            PopulationLevelConverter.ReadFile(fileContent);
        }
    }
}
