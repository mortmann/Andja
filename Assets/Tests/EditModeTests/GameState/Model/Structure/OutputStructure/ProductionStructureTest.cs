using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using Andja.Utility;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;
using Andja.Pathfinding;

public class ProductionStructureTest {
    string ID = "Production";
    private MockUtil mockutil;
    TestProductionStructure Production;
    ProductionPrototypeData PrototypeData;

    string ProducerID = "Production";
    ProductionStructure Producer;
    private string WorkerID = "Worker";

    [SetUp]
    public void SetUp() {
        PrototypeData = new ProductionPrototypeData() {
            ID = ID,
            produceTime = 2f,
            maxOutputStorage = 2,
            structureRange = 10,
            tileWidth = 2,
            tileHeight = 2,
            intake = new Item[] {ItemProvider.Wood_1, ItemProvider.Fish_2},
            output = new Item[] {ItemProvider.Stone_1}
        };
        Producer = new ProductionStructure(ProducerID, null);
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        CreateTwoByThree();
    }
    private void CreateTwoByThree() {
        Production = new TestProductionStructure(ID, PrototypeData);
        Production.City = mockutil.City;
        Production.Tiles = Production.GetBuildingTiles(World.Current.GetTileAt(Production.StructureRange, Production.StructureRange));
        PrototypeData.structureRange = 10;
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void HasRequiredIntake() {
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 };
        Assert.IsTrue(Production.HasRequiredInput());
    }

    [Test]
    public void HasRequiredIntake_False() {
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish };
        Assert.IsFalse(Production.HasRequiredInput());
    }

    [Test]
    public void OnUpdate_CanProduce_HasEnoughForOne() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 };
        for (int i = 0; i < 4; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(1, Production.Output[0].count);
    }

    [Test]
    public void OnUpdate_CanProduce_StopsAtMaxOut() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood_50, ItemProvider.Fish_25 };
        for (int i = 0; i < 12; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(2, Production.Output[0].count);
    }
    
    [Test]
    public void OnUpdate_CannotProduce() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        for (int i = 0; i < 4; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(0, Production.Output[0].count);
    }

    [Test]
    public void AddToIntake() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Production.AddToIntake(new UnitInventory() {
            Items = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 }
        });
        Assert.AreEqual(1, Production.Intake[0].count);
        Assert.AreEqual(2, Production.Intake[1].count);
    }

    [Test]
    public void GetRequiredItems_Item() {
        PrototypeData.structureRange = 10;
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Assert.AreEqual(ItemProvider.Wood.ID, Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Wood })[0].ID);
    }

    [Test]
    public void GetRequiredItems_NoItem() {
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Assert.AreEqual(0, Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Brick }).Length);
    }

    [Test]
    public void OnOutputChangedStructure() {
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Production.WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
        Producer.Output = new Item[] { ItemProvider.Wood_5 };
        Production.RegisteredStructures[Producer] = Producer.Output;

        Production.OnOutputChangedStructure(Producer);

        Assert.AreEqual(Producer, Production.WorkerJobsToDo.First().Key);
        Assert.AreEqual(1, Production.WorkerJobsToDo.First().Value.Length);
        Assert.AreEqual(ItemProvider.Wood.ID, Production.WorkerJobsToDo.First().Value[0].ID);
        Assert.AreEqual(PrototypeData.intake[0].count * ProductionStructure.INTAKE_MULTIPLIER,
                        Production.WorkerJobsToDo.First().Value[0].count);
    }

    [Test]
    public void GetMaxIntakeForIndex() {
        Production.OnBuild();
        Assert.AreEqual(PrototypeData.intake[0].count * ProductionStructure.INTAKE_MULTIPLIER,
                Production.GetMaxIntakeForIndex(0));
        Assert.AreEqual(PrototypeData.intake[1].count * ProductionStructure.INTAKE_MULTIPLIER,
                Production.GetMaxIntakeForIndex(1));
    }

    [Test]
    public void Load() {
        Production.OnBuild();
        Production.Load();
        Assert.AreEqual(ItemProvider.Wood.ID, Production.Intake[0].ID);
        Assert.AreEqual(ItemProvider.Fish.ID, Production.Intake[1].ID);
    }

    [Test]
    public void Load_ItemsChanged() {
        Production.OnBuild();
        PrototypeData.intake = new Item[] { ItemProvider.Brick };
        Production.Load();
        Assert.AreEqual(ItemProvider.Brick.ID, Production.Intake[0].ID);
        Assert.AreEqual(1, Production.Intake.Length);
    }

    [Test]
    public void SendOutWorkerIfCan_NearestMarket() {
        Route route = new Route();
        CreateTwoByThree();
        PrototypeData.workerID = "worker";
        Production.Intake = new[] { ItemProvider.Stone_1 };
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker")).Returns(new WorkerPrototypeData());
        Production.NearestMarketStructure = new TestMarketStructure { 
            City = mockutil.City,
            Tiles = new List<Tile>{mockutil.GetInCityTile(15,15)}
        };
        mockutil.CityMock.Setup(c => c.HasAnythingOfItem(It.IsAny<Item>())).Returns(true);
        mockutil.CityMock.SetupGet(c=>c.Inventory).Returns(() => {
            var inv = new CityInventory();
            inv.Items[ItemProvider.Stone.ID].count = 50;
            return inv;
        });

        Production.TestTrySendOutWorker();

        AssertThat(Production.Workers.Count).IsEqualTo(1);
        AssertThat(Production.Workers[0].ToGetItems.Length).IsEqualTo(1);
        AssertThat(Production.Workers[0].ToGetItems[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(Production.Workers[0].ToGetItems[0].count).IsEqualTo(4);
    }

    [Test]
    public void OnStructureBuild() {
        CreateTwoByThree();
        TestProductionStructure test = new TestProductionStructure(ID, new ProductionPrototypeData()) {
            Output = new[] { ItemProvider.Wood_1 },
            Tiles = new List<Tile> { Production.RangeTiles.First() }
        };
        Production.Workers = new List<Worker>();
        Production.RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
        Production.OnStructureBuild(test);
        Production.OnStructureBuild(test);

        AssertThat(Production.RegisteredStructures.Keys).ContainsExactly(test);
    }
    [Test]
    public void OnStructureBuild_NotNeeded() {
        CreateTwoByThree();
        TestProductionStructure test = new TestProductionStructure(ID, new ProductionPrototypeData()) {
            Output = new[] { ItemProvider.Brick },
            Tiles = new List<Tile> { Production.RangeTiles.First() }
        };
        Production.RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
        Production.OnStructureBuild(test);
        AssertThat(Production.RegisteredStructures.Keys).DoesNotContain(test);
        AssertThat(Production.WorkerJobsToDo.Count).IsEqualTo(0);
    }

    [Test]
    public void FindNearestMarketStructure() {
        CreateTwoByThree();
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(new WorkerPrototypeData());
        PrototypeData.workerID = WorkerID;
        Tile tile = mockutil.GetInCityTile(5, 5);
        MarketStructure marketStructure = new MarketStructure() {
            Tiles = new List<Tile> { tile }
        };
        tile.Structure = marketStructure;
        Production.FindNearestMarketStructure(tile);

        AssertThat(Production.NearestMarketStructure).IsEqualTo(marketStructure);
    }
    [Test]
    public void FindNearestMarketStructure_CloserOne() {
        CreateTwoByThree();
        PrototypeData.workerID = WorkerID;
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(new WorkerPrototypeData());
        TestMarketStructure marketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) }
        };
        Tile tile = mockutil.GetInCityTile(5, 6);
        Production.NearestMarketStructure = marketStructure;
        TestMarketStructure closerMarketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { tile }
        };
        tile.Structure = closerMarketStructure;
        Production.FindNearestMarketStructure(tile);

        AssertThat(Production.NearestMarketStructure).IsEqualTo(closerMarketStructure);
    }
    [Test]
    public void FindNearestMarketStructure_MustFollowRoad_NoConnection_IsOld() {
        CreateTwoByThree();
        PrototypeData.workerID = WorkerID;
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(()=>new WorkerPrototypeData() { hasToFollowRoads = true });

        TestMarketStructure marketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) }
        };
        Tile tile = mockutil.GetInCityTile(5, 6);
        Production.NearestMarketStructure = marketStructure;
        TestMarketStructure closerMarketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { tile }
        };
        tile.Structure = closerMarketStructure;
        Production.FindNearestMarketStructure(tile);

        AssertThat(Production.NearestMarketStructure).IsEqualTo(marketStructure);
    }

    [Test]
    public void FindNearestMarketStructure_MustFollowRoad_ClosestPerRoadWins() {
        CreateTwoByThree();
        PrototypeData.workerID = WorkerID;
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(() => new WorkerPrototypeData() { hasToFollowRoads = true });
        mockutil.pathfindingMock.Setup(p => p.EnqueueJob(It.IsAny<PathJob>())).Returns((PathJob job) => {
            job.End = new Vector2(6, 6);
            job.PathUsedGrid = new PathGrid();
            job.OnFinished?.Invoke();
            return job;
        });
        Route route = new Route();
        TestMarketStructure marketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) },
            TestRoutes = new HashSet<Route> { route }
        };
        Tile tile = mockutil.GetInCityTile(6, 6);
        Production.TestRoutes = new HashSet<Route> { route };
        Production.NearestMarketStructure = marketStructure;
        TestMarketStructure closerMarketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { tile },
            TestRoutes = new HashSet<Route> { route }
        };
        tile.Structure = closerMarketStructure;
        route.MarketStructures = new HashSet<MarketStructure>();
        route.MarketStructures.Add(marketStructure);
        route.MarketStructures.Add(closerMarketStructure);

        Production.FindNearestMarketStructure(tile);
        PathfindingThreadHandler.Instance = null;
        AssertThat(Production.NearestMarketStructure).IsEqualTo(closerMarketStructure);
    }
    
    [Test]
    public void CheckMarketStructureForRoutes_OldNoRoute_IsNull() {
        CreateTwoByThree();
        PrototypeData.workerID = WorkerID;
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(new WorkerPrototypeData());
        TestMarketStructure marketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) }
        };
        Tile tile = mockutil.GetInCityTile(5, 6);
        Production.NearestMarketStructure = marketStructure;
        Route route = new Route();
        route.MarketStructures = new HashSet<MarketStructure>();

        Production.TestRoutes = new HashSet<Route> { route };
        Production.TestCheckMarketStructureForRoutes(marketStructure);

        AssertThat(Production.NearestMarketStructure).IsNull();
    }
    [Test]
    public void CheckMarketStructureForRoutes_OldHasStillRoute() {
        CreateTwoByThree();
        PrototypeData.workerID = WorkerID;
        mockutil.PrototypControllerMock
            .Setup(p => p.GetWorkerPrototypDataForID(WorkerID))
            .Returns(new WorkerPrototypeData());
        TestMarketStructure marketStructure = new TestMarketStructure() {
            Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) }
        };
        Tile tile = mockutil.GetInCityTile(5, 6);
        Production.TestRoutes = new HashSet<Route>();
        Production.NearestMarketStructure = marketStructure;
        Route route = new Route();
        route.MarketStructures = new HashSet<MarketStructure>();
        route.MarketStructures.Add(marketStructure);
        Production.TestRoutes = new HashSet<Route> { route };
        marketStructure.TestRoutes = new HashSet<Route> { route };

        Production.TestCheckMarketStructureForRoutes(marketStructure);

        AssertThat(Production.NearestMarketStructure).IsEqualTo(marketStructure);
    }

    class TestProductionStructure : ProductionStructure {
        public TestProductionStructure(string id, ProductionPrototypeData productionPrototypeData) : base(id, productionPrototypeData) {
        }
        public MarketStructure NearestMarketStructure {
            get => nearestMarketStructure;
            set => nearestMarketStructure = value;
        }
        public HashSet<Route> TestRoutes {
            get => Routes;
            set => Routes = value;
        }
        public List<Worker> Workers {
            get => workers;
            set => workers = value;
        }
        public void TestTrySendOutWorker() {
            Workers ??= new List<Worker>();
            SendOutWorkerIfCan();
        }

        public void TestAddJobStructure(OutputStructureTest.TestOutputStructure testOutputStructure, Item[] items) {
            WorkerJobsToDo ??= new Dictionary<OutputStructure, Item[]>();
            WorkerJobsToDo[testOutputStructure] = items;
        }

        public void TestCheckMarketStructureForRoutes(MarketStructure structure) {
            CheckMarketStructureForRoutes(structure);
        }
    }

    class TestMarketStructure : MarketStructure {

        public TestMarketStructure() {
            prototypeData = new StructurePrototypeData() {
                tileWidth = 1,
                tileHeight = 1,
            };
        }

        public HashSet<Route> TestRoutes {
            get => Routes;
            set => Routes = value;
        }
    }
}
