using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
namespace Andja.Controller {

    public class UnitConverter {
        private readonly Dictionary<string, Unit> idToUnit;
        private readonly Dictionary<string, UnitPrototypeData> idToUnitPrototypData;
        private readonly Dictionary<string, WorkerPrototypeData> idToWorkerPrototypData;
        BaseConverter<UnitPrototypeData> unitConverter;
        BaseConverter<WorkerPrototypeData> workerConverter;
        BaseConverter<ShipPrototypeData> shipConverter;

        public UnitConverter(
            Dictionary<string, Unit> idToUnit,
            Dictionary<string, UnitPrototypeData> idToUnitPrototypData,
            Dictionary<string, WorkerPrototypeData> idToWorkerPrototypData) {
            unitConverter = new BaseConverter<UnitPrototypeData>(
                (_) => new UnitPrototypeData(),
                "units/unit",
                (id, data) => {
                    idToUnit[id] = new Unit(id, data);
                    idToUnitPrototypData[id] = data;
                });
            shipConverter = new BaseConverter<ShipPrototypeData>(
                (_) => new ShipPrototypeData(),
                "units/ship",
                (id, data) => {
                    idToUnit[id] = new Ship(id, data);
                    idToUnitPrototypData[id] = data;
                });
            workerConverter = new BaseConverter<WorkerPrototypeData>(
                (_) => new WorkerPrototypeData(),
                "units/worker",
                (id, data) => {
                    idToWorkerPrototypData[id] = data;
                });
            this.idToUnit = idToUnit;
            this.idToUnitPrototypData = idToUnitPrototypData;
            this.idToWorkerPrototypData = idToWorkerPrototypData;
        }

        public void ReadFile(string fileContent) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(fileContent);
            unitConverter.ReadFile(xmlDoc);
            shipConverter.ReadFile(xmlDoc);
            workerConverter.ReadFile(xmlDoc);
        }

    }
}