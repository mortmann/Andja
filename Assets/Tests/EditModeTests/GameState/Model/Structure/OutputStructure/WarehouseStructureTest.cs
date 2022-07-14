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

public class WarehouseStructureTest {
    string ID = "MarketStructure";
    WarehouseStructure Warehouse;
    WarehousePrototypData PrototypeData;
    private MockUtil mockutil;

    [SetUp]
    public void SetUp() {
        Warehouse = new WarehouseStructure(ID, PrototypeData);
        PrototypeData = new WarehousePrototypData() {
            ID = ID,
            structureRange = 20
        };
        mockutil = new MockUtil();
        PrototypeData.output = new Item[] { ItemProvider.Stone_1 };
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        mockutil.CityMock.Setup(x => x.RemoveTiles(It.IsAny<IEnumerable<Tile>>()));
        var items = new Dictionary<string, Item>() {
            { ItemProvider.Brick.ID, ItemProvider.Brick.Clone() },
            { ItemProvider.Tool.ID, ItemProvider.Tool.Clone()   },
            { ItemProvider.Wood.ID, ItemProvider.Wood.Clone()   },
            { ItemProvider.Fish.ID, ItemProvider.Fish.Clone()   },
            { ItemProvider.Stone.ID, ItemProvider.Stone.Clone()   },
        };
        prototypeControllerMock.Setup(m => m.GetCopieOfAllItems()).Returns(
            () => items.ToDictionary(x => x.Key, y => y.Value.Clone()));
        prototypeControllerMock.Setup(m => m.GetPopulationLevels(It.IsAny<City>())).Returns(
            () => new List<PopulationLevel>());

        CreateFourByFour();
    }

    private void CreateFourByFour() {
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;

        Warehouse.Tiles = Warehouse.GetBuildingTiles(World.Current.GetTileAt(Warehouse.StructureRange, Warehouse.StructureRange));
        Warehouse.RangeTiles = new HashSet<Tile>();
        Warehouse.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void SpecialCheckForBuild_True() {
        Assert.IsTrue(Warehouse.SpecialCheckForBuild(Warehouse.Tiles));
    }
    [Test]
    public void SpecialCheckForBuild_False_WarehouseExists() {
        Warehouse.Tiles.First().City = mockutil.City;
        mockutil.CityMock.Setup(c => c.Warehouse).Returns(Warehouse);
        Assert.IsFalse(Warehouse.SpecialCheckForBuild(Warehouse.Tiles));
    }
}
