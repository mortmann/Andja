using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
namespace Andja.Controller {

    public class EventConverter {
        private BaseConverter<EffectPrototypeData> effectConverter;
        private BaseConverter<GameEventPrototypData> gameEventConverter;

        public EventConverter(Dictionary<string, EffectPrototypeData> effectPrototypeDatas,
            Dictionary<string, GameEventPrototypData> gameEventPrototypeDatas) {
            effectConverter = new BaseConverter<EffectPrototypeData>(
                (id) => new EffectPrototypeData(),
                "events/Effect",
                (id, data) => {
                    effectPrototypeDatas[id] = data;
                });
            gameEventConverter = new BaseConverter<GameEventPrototypData>(
                (id) => new GameEventPrototypData() { ID = id },
                "events/GameEvent",
                (id, data) => {
                    gameEventPrototypeDatas[id] = data;
                });
        }

        public void ReadFromFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent);
            effectConverter.ReadFile(fileContent);
            gameEventConverter.ReadFile(fileContent);
        }
    }
}