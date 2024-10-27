using System;
using System.Linq;
using System.Collections.Generic;
using Andja.Model;
using Moq;
using NUnit.Framework;
using Andja;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class MarketStructureTest {
    string ID = "MarketStructure";
    MarketStructure Market;
    MarketPrototypeData PrototypeData;
    private MockUtil mockutil;
    private ICity City;
    [SetUp]
    public void SetUp() {
        Market = new MarketStructure(ID, null);
        PrototypeData = new MarketPrototypeData() {
            ID = ID,
            structureRange = 20
        };
        mockutil = new MockUtil();
        City = mockutil.City;
        PrototypeData.output = new Item[] { ItemProvider.Stone_1 };
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        mockutil.CityMock.Setup(x => x.RemoveTiles(It.IsAny<IEnumerable<Tile>>()));
        var Items = new Dictionary<string, Item>() {
            { ItemProvider.Brick.ID, ItemProvider.Brick.Clone() },
            { ItemProvider.Tool.ID, ItemProvider.Tool.Clone()   },
            { ItemProvider.Wood.ID, ItemProvider.Wood.Clone()   },
            { ItemProvider.Fish.ID, ItemProvider.Fish.Clone()   },
            { ItemProvider.Stone.ID, ItemProvider.Stone.Clone()   },
        };
        prototypeControllerMock.Setup(m => m.GetCopieOfAllItems()).Returns(() => {
            return Items.ToDictionary(x => x.Key, y => y.Value.Clone());
        });
        prototypeControllerMock.Setup(m => m.GetPopulationLevels(It.IsAny<City>()))
            .Returns(() => new List<PopulationLevel>());
        
        CreateFourByFour();
    }
    
    private void CreateFourByFour() {
        Market.City = mockutil.City;
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;
        Market.OutputMarkedStructures = new List<OutputStructure>();
        Market.Tiles = Market.GetBuildingTiles(World.Current.GetTileAt(Market.StructureRange, Market.StructureRange));
        Market.RangeTiles = new HashSet<Tile>();
        Market.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void GetOutput_WithItemAndMax() {
        Item[] items = new Item[] { ItemProvider.Stone_N(5), ItemProvider.Wood_N(5) };
        City.Inventory.Items[ItemProvider.Stone.ID].count = 5;
        City.Inventory.Items[ItemProvider.Wood.ID].count = 5;

        Item[] output = Market.GetOutput(items, new int[] { 2, 3 });

        Assert.AreEqual(items.Length, output.Length);
        Assert.AreEqual(items[0].ID, output[0].ID);
        Assert.AreEqual(items[1].ID, output[1].ID);
        Assert.AreEqual(2, output[0].count);
        Assert.AreEqual(3, output[1].count);
        Assert.AreEqual(3, City.Inventory.Items[output[0].ID].count);
        Assert.AreEqual(2, City.Inventory.Items[output[1].ID].count);
    }

    [Test]
    public void GetOutputWithItemCountAsMax() {
        Item[] items = new Item[] { ItemProvider.Stone_N(2), ItemProvider.Wood_N(3) };
        City.Inventory.Items[ItemProvider.Stone.ID].count = 5;
        City.Inventory.Items[ItemProvider.Wood.ID].count = 5;

        Item[] output = Market.GetOutputWithItemCountAsMax(items);

        Assert.AreEqual(items.Length, output.Length);
        Assert.AreEqual(items[0].ID, output[0].ID);
        Assert.AreEqual(items[1].ID, output[1].ID);
        Assert.AreEqual(2, output[0].count);
        Assert.AreEqual(3, output[1].count);
        Assert.AreEqual(3, City.Inventory.Items[output[0].ID].count);
        Assert.AreEqual(2, City.Inventory.Items[output[1].ID].count);
    }

    [Test]
    public void Capture() {
        Mock<IWarfare> warfare = new Mock<IWarfare>();
        warfare.Setup(w => w.PlayerNumber).Returns(1);

        for (int i = 0; i < 20; i++) {
            Market.Capture(warfare.Object, 0.1f);
            Market.UpdateCaptureProgress(1f);
            Assert.AreEqual(Market.MaximumCaptureSpeed * 1f * (i+1), Market.capturedProgress, 0.0001);
        }
        Assert.IsTrue(Market.Captured);
    }
    [Test]
    public void Capture_Stops_ReturnsToFull() {
        Mock<IWarfare> warfare = new Mock<IWarfare>();
        warfare.Setup(w => w.PlayerNumber).Returns(1);
        PrototypeData.decreaseCaptureSpeed = 0.01f;

        for (int i = 0; i < 20; i++) {
            Market.Capture(warfare.Object, 0.01f);
            Market.UpdateCaptureProgress(1f);
        }
        for (int i = 0; i < 10; i++) {
            Market.Capture(warfare.Object, 0);
            Market.UpdateCaptureProgress(1f);
        }
        AssertThat(Market.capturedProgress).IsGreaterThan(0);
        for (int i = 0; i < 10; i++) {
            Market.Capture(warfare.Object, 0);
            Market.UpdateCaptureProgress(1f);
        }
        AssertThat(Market.capturedProgress).IsEqualTo(0,0.0001f);
        AssertThat(Market.Captured).IsFalse();
    }
    [Test]
    public void DoneCapturing_WithCity() {
        Market.Tiles.ForEach(t => t.City = mockutil.City);
        Market.RangeTiles.ToList().ForEach(t => t.City = mockutil.City);

        Mock<IWarfare> warfare = new Mock<IWarfare>();
        warfare.Setup(w => w.PlayerNumber).Returns(1);
        mockutil.WorldIsland.Cities.Add(new City(1, mockutil.WorldIsland));

        Market.City = City;
        Market.capturedProgress = 1;
        Market.Capture(warfare.Object, 10010101);

        Assert.AreEqual(1, Market.PlayerNumber);
        Assert.IsFalse(Market.Captured);
    }

    [Test]
    public void DoneCapturing_WithoutCity() {
        Market.Tiles.ForEach(t => t.City = mockutil.City);
        Market.RangeTiles.ToList().ForEach(t => t.City = mockutil.City);

        Mock<IWarfare> warfare = new Mock<IWarfare>();
        warfare.Setup(w => w.PlayerNumber).Returns(1);

        Market.City = City;
        Market.capturedProgress = 1;
        Market.Capture(warfare.Object, 10010101);

        Assert.IsTrue(Market.IsDestroyed);
    }

    [Theory]
    [TestCase(0, false)]
    [TestCase(7, false)]
    [TestCase(8, true)]
    [TestCase(16, true)]
    public void InCityCheck_BuildTiles(int tilesInCity, bool expected) {
        Market.Tiles.Take(tilesInCity).ToList()
                    .ForEach(x => x.City = City);
        Assert.AreEqual(expected, Market.InCityCheck(Market.Tiles, City.PlayerNumber));
    }

    [Theory]
    [TestCase(0, false)]
    [TestCase(3, false)]
    [TestCase(4, true)]
    [TestCase(42, true)]
    public void InCityCheck_RangeTiles(int tilesInCity, bool expected) {
        Market.RangeTiles.Take(tilesInCity).ToList()
                    .ForEach(x => x.City = City);
        Assert.AreEqual(expected, Market.InCityCheck(Market.Tiles, City.PlayerNumber));
    }

    [Test]
    public void OnBuild() {
        Market.OnBuild();
        HashSet<Tile> tiles = new HashSet<Tile>(Market.RangeTiles);
        tiles.UnionWith(Market.Tiles);
        AssertThat(mockutil.CityMock)
            .HasInvoked(c => c.AddTiles(It.Is<IEnumerable<Tile>>(x => tiles.SetEquals(x)))).Once();
    }

    [Test]
    public void AddRoadStructure() {
        var road = new RoadStructure();
        road.Route = new Route();
        Market.AddRoadStructure(road);
        Assert.IsTrue(road.Route.MarketStructures.Contains(Market));
        Assert.IsTrue(Market.GetRoutes().Contains(road.Route));
        Assert.IsTrue(Market.RoadsAroundStructure().Contains(road));
    }

    [Test]
    public void GetRequiredItems() {
        var items = new[] { ItemProvider.Stone_1, ItemProvider.Fish_2 };
        mockutil.CityMock.SetupGet(c => c.MarketStructures).Returns(new List<MarketStructure>());
        AssertThat(Market.GetRequiredItems(new OutputStructureTest.TestOutputStructure(), items))
            .AllSatisfy(newItem => items.ToList().Exists(x => x.ID == newItem.ID && newItem.count == 50));

    }

    [Test]
    public void GetRequiredItems_FullStoneCity() {
        var items = new[] { ItemProvider.Stone_1, ItemProvider.Fish_2 };
        mockutil.CityMock.SetupGet(c => c.Inventory).Returns(() => {
            var ci = new CityInventory(1);
            ci.Items[ItemProvider.Stone.ID].count = 50;
            ci.Items[ItemProvider.Fish.ID].count = 25;
            return ci;
        });
        mockutil.CityMock.SetupGet(c => c.MarketStructures).Returns(new List<MarketStructure>());
        var news = Market.GetRequiredItems(new OutputStructureTest.TestOutputStructure(), items);
        AssertThat(news).AllSatisfy(newItem => ItemProvider.Fish.ID == newItem.ID && newItem.count == 25);
    }

    [Test]
    public void OnRouteChange() {
        var road = new RoadStructure();
        var routeOne = new Route();
        road.Route = routeOne;
        Market.AddRoadStructure(road);
        var routeTwo = new Route();
        road.Route = routeTwo;
        Assert.IsFalse(routeOne.MarketStructures.Contains(Market));
        Assert.IsTrue(routeTwo.MarketStructures.Contains(Market));
    }
    [Test]
    public void RemoveRoute() {
        var road = new RoadStructure();
        road.Route = new Route();
        road.Route.MarketStructures = new HashSet<MarketStructure>();
        road.Route.MarketStructures.Add(Market);
        Market.AddRoadStructure(road);
        Market.RemoveRoute(road.Route);
        Assert.IsFalse(road.Route.MarketStructures.Contains(Market));
        Assert.IsFalse(Market.GetRoutes().Contains(road.Route));

    }
    [Test]
    public void OnDestroy() {
        Market.Tiles.ForEach(t => t.City = mockutil.City);
        Market.RangeTiles.ToList().ForEach(t => t.City = mockutil.City);

        Market.OnDestroy();
        List<Tile> h = new List<Tile>(Market.Tiles);
        h.AddRange(Market.RangeTiles);
        Assert.IsTrue(h.All(x => x.City != City));
    }

    [Test]
    public void OnOutputChangedStructure_HasOutput_NoRoute() {
        OutputStructureTest.TestOutputStructure outputStructureTest = new OutputStructureTest.TestOutputStructure("URG", new OutputPrototypData());
        Market.OutputMarkedStructures = new List<OutputStructure>();
        outputStructureTest.Output = new Item[] { ItemProvider.Tool_5 };
        Market.OnOutputChangedStructure(outputStructureTest);
        Assert.True(Market.OutputMarkedStructures.Contains(outputStructureTest));
    }
    [Test]
    public void OnOutputChangedStructure_NoOutput_NoRoute() {
        OutputStructureTest.TestOutputStructure outputStructureTest = new OutputStructureTest.TestOutputStructure("URG", new OutputPrototypData());
        outputStructureTest.Output = new Item[] { ItemProvider.Wood };

        Market.OutputMarkedStructures = new List<OutputStructure>();
        Market.OnOutputChangedStructure(outputStructureTest);
        Assert.False(Market.OutputMarkedStructures.Contains(outputStructureTest));
    }
    [Test]
    public void OnOutputChangedStructure_HasOutput_Route() {
        OutputStructureTest.TestOutputStructure outputStructureTest = new OutputStructureTest.TestOutputStructure("URG", new OutputPrototypData());
        Market.OutputMarkedStructures = new List<OutputStructure>();

        RoadStructure road = new RoadStructure();
        road.Route = new Route();
        outputStructureTest.AddRoadStructure(road);
        Market.AddRoadStructure(road);
        Market.OutputMarkedStructures = new List<OutputStructure>();
        outputStructureTest.Output = new Item[] { ItemProvider.Tool_5 };
        Market.OnOutputChangedStructure(outputStructureTest);
        
        Assert.IsTrue(Market.WorkerJobsToDo.ContainsKey(outputStructureTest));
        Assert.IsFalse(Market.OutputMarkedStructures.Contains(outputStructureTest));
    }
    [Test]
    public void OnOutputChangedStructure_NoOutput_Route() {
        OutputStructureTest.TestOutputStructure outputStructureTest = new OutputStructureTest.TestOutputStructure("URG", new OutputPrototypData());
        Market.OutputMarkedStructures = new List<OutputStructure>();
        outputStructureTest.Output = new Item[] { ItemProvider.Wood };

        RoadStructure road = new RoadStructure();
        road.Route = new Route();
        outputStructureTest.AddRoadStructure(road);
        Market.AddRoadStructure(road);
        Market.OutputMarkedStructures = new List<OutputStructure>();
        Market.OnOutputChangedStructure(outputStructureTest);

        Assert.IsFalse(Market.WorkerJobsToDo.ContainsKey(outputStructureTest));
    }
    [Test]
    public void OnStructureAdded_OutputStructure() {
        OutputStructureTest.TestOutputStructure outputStructureTest = new OutputStructureTest.TestOutputStructure("URG", new OutputPrototypData());
        Market.OutputMarkedStructures = new List<OutputStructure>();

        outputStructureTest.Output = Array.Empty<Item>();
        outputStructureTest.City = City;
        outputStructureTest.Tiles = Market.RangeTiles.Take(4).ToList();
        Market.OnStructureAdded(outputStructureTest);
        Assert.IsTrue(outputStructureTest.IsOutputChangedCallbackRegistered());
    }
}
