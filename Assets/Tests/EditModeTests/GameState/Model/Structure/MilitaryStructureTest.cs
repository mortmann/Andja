using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Andja.Utility;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using Andja;


public class MilitaryStructureTest {
    string ID = "MilitaryStructure";
    TestMilitary Military;
    MilitaryPrototypeData PrototypeData;
    private MockUtil mockutil;
    string UnitID = "UnitID";
    Unit Unit;
    UnitPrototypeData UnitPrototypeData;

    [SetUp]
    public void SetUp() {
        Military = new TestMilitary(ID, PrototypeData);
        PrototypeData = new MilitaryPrototypeData() {
            ID = ID,
            structureRange = 4,
            buildTimeModifier = 1,
            buildQueueLength = 2,
        };
        Unit = new Unit(UnitID, UnitPrototypeData);
        UnitPrototypeData = new UnitPrototypeData() {
            ID = ID,
            buildingItems = new Item[] { ItemProvider.Wood_5, ItemProvider.Tool_5 },
            buildTime = 2f,
        };

        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        prototypeControllerMock.Setup(m => m.GetUnitPrototypDataForID(UnitID)).Returns(UnitPrototypeData);

        CreateFourByFour();
    }
    private void CreateFourByFour() {
        Military.City = mockutil.City;
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;
        Military.Tiles = Military.GetBuildingTiles(World.Current.GetTileAt(Military.StructureRange, Military.StructureRange));
        Military.RangeTiles = new HashSet<Tile>();
        Military.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }
    [Test]
    public void HasEnoughResources() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(),It.IsAny<int>())).Returns(true);
        Assert.IsTrue(Military.HasEnoughResources(Unit));
    }

    [Test]
    public void UpdateBuildUnit() {
        Unit secondInQueue = new Unit("Second", UnitPrototypeData);
        mockutil.PrototypControllerMock.Setup(m => m.GetUnitPrototypDataForID("Second")).Returns(UnitPrototypeData);
        Military.ToBuildUnits = new Queue<Unit>();
        Military.ToBuildUnits.Enqueue(Unit);
        Military.ToBuildUnits.Enqueue(secondInQueue);
        Military.ToPlaceUnitTiles = new List<Tile>() { World.Current.GetTileAt(1,1) };

        for (int i = 0; i < 10; i++) {
            Military.UpdateBuildUnit(0.2f);
        }
        Assert.AreEqual(Military.CurrentlyBuildingUnit.ID, secondInQueue.ID);
        mockutil.WorldMock.Verify(x => x.CreateUnit(Unit, It.IsAny<Player>(), World.Current.GetTileAt(1, 1), 0), Times.Once());
    }

    [Test]
    public void AddUnitToBuildQueue() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.RemoveItems(It.IsAny<Item[]>()));
        Military.ToBuildUnits = new Queue<Unit>();
        Military.AddUnitToBuildQueue(Unit);
        Assert.AreEqual(Military.CurrentlyBuildingUnit, Unit);
    }

    class TestMilitary : MilitaryStructure {
        public TestMilitary(string iD, MilitaryPrototypeData mpd) : base(iD, mpd) {
        }
        public Queue<Unit> ToBuildUnits {
            get => toBuildUnits;
            set => toBuildUnits = value;
        }
        public List<Tile> ToPlaceUnitTiles {
            get => toPlaceUnitTiles;
            set => toPlaceUnitTiles = value;
        }

    }

}
