using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class FarmStructureTest {

    private const string ID = "farm";
    private const string GrowableID = "growable";
    private const string RoadID = "FAKEROAD";
    private const string NaturalSpawnID = "tree";
    private FarmPrototypeData farmPrototypeData;
    private GrowablePrototypeData growablePrototypeData;
    private TestFarmStructure farm;

    [SetUp]
    public void SetUp() {
        farmPrototypeData = new FarmPrototypeData {
            produceTime = 3,
            neededHarvestToProduce = 2,
            structureRange = 5,
            output = new[] { ItemProvider.Wood },
            maxOutputStorage = 5
        };
        farm = new TestFarmStructure(ID, null);
        growablePrototypeData = new GrowablePrototypeData {
            ID = GrowableID,
            output = new[] {ItemProvider.Wood} 
        };
        MockUtil mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => farmPrototypeData);
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(GrowableID)).Returns(() => growablePrototypeData);
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(RoadID)).Returns(() => new RoadStructurePrototypeData());
        prototypeControllerMock.Setup(m => m.GetWorkerPrototypDataForID(It.IsAny<string>())).Returns(() => new WorkerPrototypeData());
        prototypeControllerMock.Setup(m => m.AllNaturalSpawningStructureIDs).Returns(new List<string> { NaturalSpawnID });
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID("not" + GrowableID)).Returns(() => growablePrototypeData);

    }

    private void CreateTwoByTwo() {
        farmPrototypeData.tileWidth = 2;
        farmPrototypeData.tileHeight = 2;
        farm.RangeTiles = new HashSet<Tile>();
        farm.Tiles = farm.GetBuildingTiles(World.Current.GetTileAt(farm.StructureRange, farm.StructureRange));
        farm.RangeTiles.UnionWith(farmPrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void OnBuild_NoGrowable_EmptyTiles() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
    }
    [Test]
    public void OnBuild_NoGrowable_FullTilesNaturalSpawns() {
        CreateTwoByTwo();
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new GrowableStructure(NaturalSpawnID, null));
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
    }
    [Test]
    public void OnBuild_NoGrowable_FullTiles() {
        CreateTwoByTwo();
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new RoadStructure(RoadID, null));
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(0, farm.WorkingTilesCount);
    }

    [Test]
    public void OnBuild_WithGrowable_EmptyTiles() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(0, farm.WorkingTilesCount);
    }

    [Test]
    public void OnBuild_WithGrowable_FullTiles() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        farm.RangeTiles.AsParallel().ForAll(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
    }

    [Test]
    public void OnStructureChange_NoGrowable_NewEmptyTile() {
        CreateTwoByTwo();
        farm.OnBuild();
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new RoadStructure("FAKEROAD", null));
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(0, farm.WorkingTilesCount);
        farm.RangeTiles.First().Structure = null;
        Assert.AreEqual(1, farm.WorkingTilesCount);
    }
    [Test]
    public void OnStructureChange_Growable_NewGrowable() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(0, farm.WorkingTilesCount);
        farm.RangeTiles.First().Structure = new GrowableStructure(GrowableID, growablePrototypeData);
        Assert.AreEqual(1, farm.WorkingTilesCount);
    }
    [Test]
    public void OnStructureChange_NoGrowable_NewFullTile() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
        farm.RangeTiles.First().Structure = new RoadStructure("FAKEROAD", null);
        Assert.AreEqual(75, farm.WorkingTilesCount);
    }
    [Test]
    public void OnStructureChange_Growable_NewEmptyTile() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
        farm.RangeTiles.First().Structure = null;
        Assert.AreEqual(75, farm.WorkingTilesCount);
    }
    [Test]
    public void OnStructureChange_Growable_NewFullTile() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        farm.OnBuild();
        Assert.AreEqual(76, farm.RangeTiles.Count);
        Assert.AreEqual(76, farm.WorkingTilesCount);
        farm.RangeTiles.First().Structure = new RoadStructure("FAKEROAD", null);
        Assert.AreEqual(75, farm.WorkingTilesCount);
    }

    [Test]
    public void SendWorkerOutIfCan_NoTarget() {
        CreateTwoByTwo();
        farm.OnBuild();
        farm.TestTrySendOutWorker();
        Assert.AreEqual(0, farm.Workers.Count);
    }
    [Test]
    public void SendWorkerOutIfCan_Target() {
        CreateTwoByTwo();
        farm.OnBuild();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        var tile = farm.RangeTiles.First();
        tile.Structure = new GrowableStructure(GrowableID, growablePrototypeData) {
            hasProduced = true,
            Tiles = new List<Tile> { tile },
        };
        farm.TestTrySendOutWorker();
        Assert.AreEqual(1, farm.Workers.Count);
        Assert.AreEqual(ItemProvider.Wood.ID, farm.Workers[0].ToGetItems[0].ID);
        Assert.AreEqual(tile.Structure, farm.Workers[0].WorkStructure);
    }
    [Test]
    public void ProduceNoGrowable() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(0, farm.Output[0].count);
        for (int i = 0; i < 2; i++) {
            Assert.AreEqual(i, farm.currentlyHarvested);
            farm.DoWorkNoGrowable(3);
        }
        farm.CheckForOutputProduced();
        Assert.AreEqual(1, farm.Output[0].count);
    }
    [Test]
    public void OnUpdate_CapAtMax() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(0, farm.Output[0].count);
        for (int i = 0; i < 20; i++) {
            farm.OnUpdate(3);
        }
        farm.CheckForOutputProduced();
        Assert.AreEqual(5, farm.Output[0].count);
    }
    [Test]
    public void ProduceNoGrowable_NoFreeTiles() {
        CreateTwoByTwo();
        farm.OnBuild();
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        Assert.AreEqual(0, farm.Output[0].count);
        for (int i = 0; i < 2; i++) {
            Assert.AreEqual(0, farm.currentlyHarvested);
            farm.DoWorkNoGrowable(3);
        }
        farm.CheckForOutputProduced();
        Assert.AreEqual(0, farm.Output[0].count);
    }
    [Test]
    public void ProduceNoGrowable_PartialFreeTiles_SlowerProduce() {
        CreateTwoByTwo();
        farm.OnBuild();
        new List<Tile>(farm.RangeTiles.Skip(farm.RangeTiles.Count/2)).ForEach(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        Assert.AreEqual(0, farm.Output[0].count);
        for (int i = 0; i < 2; i++) {
            farm.DoWorkNoGrowable(3);
        }
        Assert.AreEqual(1, farm.currentlyHarvested);
        for (int i = 0; i < 2; i++) {
            farm.DoWorkNoGrowable(3);
        }
        Assert.AreEqual(2, farm.currentlyHarvested);
        farm.CheckForOutputProduced();
        Assert.AreEqual(1, farm.Output[0].count);
    }
    [Test]
    public void ProduceGrowableWithWorker() {
        CreateTwoByTwo();
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        new List<Tile>(farm.RangeTiles).ForEach(x => x.Structure = new GrowableStructure(GrowableID, growablePrototypeData));
        farm.OnBuild();
        Assert.AreEqual(0, farm.Output[0].count);
        for (int i = 0; i < 2; i++) {
            farm.AddHarvastable();
            Assert.AreEqual(i+1, farm.currentlyHarvested);
        }
        farm.CheckForOutputProduced();
        Assert.AreEqual(1, farm.Output[0].count);
    }

    [Test]
    public void CalculateProgress_NoGrowableOrNoWorker() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(farm.ProduceTime * farm.NeededHarvestForProduce, farm.TotalProgress);
        Assert.AreEqual(0, farm.Progress);
        for (int i = 0; i < 2; i++) {
            farm.DoWorkNoGrowable(3);
            Assert.AreEqual(farm.currentlyHarvested * farm.ProduceTime, farm.Progress);
        }
        Assert.AreEqual(farm.TotalProgress, farm.Progress);
    }
    [Test]
    public void CalculateProgress_LessWorkerThanNeededHarvestForProduce() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(farm.ProduceTime * farm.NeededHarvestForProduce, farm.TotalProgress);
        Assert.AreEqual(0, farm.Progress);
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        farmPrototypeData.maxNumberOfWorker = 2;
        farm.Workers = new List<Worker>();
        farm.Workers.Add(new Worker(farm, GetGrowable(farm.RangeTiles.First()), 1.5f, null));
        farm.Workers.Add(new Worker(farm, GetGrowable(farm.RangeTiles.First()), 0.5f, null));
        Assert.AreEqual(1.5f + 2.5f, farm.Progress);
        farm.AddHarvastable();
        Assert.AreEqual(1.5f + 2.5f + farm.ProduceTime, farm.Progress);

    }
    [Test]
    public void CalculateProgress_MoreWorkerThanNeededHarvestForProduce() {
        CreateTwoByTwo();
        farm.OnBuild();
        Assert.AreEqual(farm.ProduceTime * farm.NeededHarvestForProduce, farm.TotalProgress);
        Assert.AreEqual(0, farm.Progress);
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        farmPrototypeData.maxNumberOfWorker = 3;
        farm.Workers = new List<Worker>();
        farm.Workers.Add(new Worker(farm, GetGrowable(farm.RangeTiles.ToList()[0]), 1.5f, null));
        farm.Workers.Add(new Worker(farm, GetGrowable(farm.RangeTiles.ToList()[1]), 0.5f, null));
        farm.Workers.Add(new Worker(farm, GetGrowable(farm.RangeTiles.ToList()[2]), 0.5f, null));
        Assert.AreEqual(2.5f + 2.5f, farm.Progress);
        farm.AddHarvastable();
        Assert.AreEqual(2.5f + 2.5f + farm.ProduceTime, farm.Progress);

    }

    [Test]
    public void DoWorkWithGrowableNoWorker() {
        CreateTwoByTwo();
        farmPrototypeData.maxNumberOfWorker = 0;
        GrowableStructure gs = GetGrowable(farm.RangeTiles.First());
        farm.WorkingGrowables = new List<GrowableStructure> { gs };
        gs.hasProduced = true;

        for (int i = 0; i < 4; i++) {
            farm.DoWorkWithGrowableNoWorker(1);
        }

        AssertThat(gs.hasProduced).IsFalse();
        AssertThat(farm.Output[0].ID).IsEqualTo(ItemProvider.Wood.ID);
        AssertThat(farm.Output[0].count).IsEqualTo(0);
        AssertThat(farm.currentlyHarvested).IsEqualTo(1);
    }
    [Test]
    public void DoWorkWithGrowableNoWorker_HarvestedTwice() {
        CreateTwoByTwo();
        farmPrototypeData.maxNumberOfWorker = 0;
        farm.RangeTiles.ToList().ForEach( x=> GetGrowable(x));
        farm.WorkingGrowables = farm.RangeTiles.ToList().Select(x=>x.Structure as GrowableStructure).ToList();
        for (int i = 0; i < 7; i++) {
            farm.DoWorkWithGrowableNoWorker(1);
        }
        AssertThat(farm.currentlyHarvested).IsEqualTo(2);
    }
    [Test]
    public void CalculateEfficiencyPercent() {
        CreateTwoByTwo();
        farm.WorkingTilesCount = 38;
        AssertThat(farm.EfficiencyPercent).IsEqualTo(50);
    }
    [Test]
    public void OnDestroy() {
        CreateTwoByTwo();
        farm.Workers ??= new List<Worker>();
        farm.Workers.Add(new Worker());
        farm.Workers.Add(new Worker());
        farmPrototypeData.growable = new GrowableStructure(GrowableID, growablePrototypeData);
        foreach (var farmRangeTile in farm.RangeTiles) {
            GrowableStructure gs = new GrowableStructure(GrowableID, growablePrototypeData);
            gs.SetBeingWorked(true);
            farmRangeTile.Structure = gs;
        }
        GrowableStructure notgs = new GrowableStructure("not" + GrowableID, growablePrototypeData);
        notgs.SetBeingWorked(true);
        farm.RangeTiles.First().Structure = notgs;

        farm.OnDestroy();
        AssertThat(farm.Workers).AllSatisfy(w => w.IsAlive == false);
        AssertThat(farm.RangeTiles.Select(t => t.Structure as GrowableStructure))
            .AllSatisfy(g => g.ID == GrowableID && false == g.IsBeingWorked 
                                         || g.ID != GrowableID && g.IsBeingWorked);
    }


    class TestFarmStructure : FarmStructure {
        public TestFarmStructure(string id, FarmPrototypeData fpd) : base(id, fpd) {
        }
        public List<Worker> Workers {
            get => workers;
            set => workers = value;
        }

        public List<GrowableStructure> WorkingGrowables {
            get => readyToHarvestGrowable;
            set => readyToHarvestGrowable = value;
        }
        public void TestTrySendOutWorker() {
            Workers ??= new List<Worker>();
            SendOutWorkerIfCan();
        }
    }

    private GrowableStructure GetGrowable(Tile t) {
        var gs = new GrowableStructure(GrowableID, growablePrototypeData) {
            Tiles = new List<Tile> { t },
        };
        t.Structure = gs;
        return gs;
    }
}
