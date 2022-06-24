using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class FarmStructureTest {

    private const string ID = "farm";
    private const string GrowableID = "growable";
    private const string RoadID = "FAKEROAD";
    private const string NaturalSpawnID = "tree";
    private FarmPrototypeData farmPrototypeData;
    private GrowablePrototypeData growablePrototypeData;

    private FarmStructure farm;
    private Mock<IGrowableStructure> growable;
    private Mock<IPrototypController> prototypeControllerMock;
    private Mock<ICity> CityMock;
    private Mock<IWorld> WorldMock;
    private Island Island;
    [SetUp]
    public void SetUp() {
        prototypeControllerMock = new Mock<IPrototypController>();
        PrototypController.Instance = prototypeControllerMock.Object;
        growable = new Mock<IGrowableStructure>();
        CityMock = new Mock<ICity>();
        WorldMock = new Mock<IWorld>();
        World.Current = WorldMock.Object;
        Island = new Island();
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<float>(), It.IsAny<float>())).Returns((float x, float y) => {
            LandTile t = new LandTile((int)x,(int)y);
            t.Type = TileType.Dirt;
            t.Island = Island;
            return t;
        });
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => {
            LandTile t = new LandTile(x, y);
            t.Type = TileType.Dirt;
            t.Island = Island;
            return t;
        });
        farmPrototypeData = new FarmPrototypeData {
            produceTime = 3,
            neededHarvestToProduce = 2,
            structureRange = 5,
            output = new[] { ItemProvider.Wood }
        };
        farm = new FarmStructure(ID, farmPrototypeData);
        growablePrototypeData = new GrowablePrototypeData {
            ID = GrowableID,
            output = new[] {ItemProvider.Wood} 
        };
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(farmPrototypeData);
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(GrowableID)).Returns(growablePrototypeData);
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(RoadID)).Returns(new RoadStructurePrototypeData());
        prototypeControllerMock.Setup(m => m.GetWorkerPrototypDataForID(It.IsAny<string>())).Returns(new WorkerPrototypeData());
        prototypeControllerMock.Setup(m => m.AllNaturalSpawningStructureIDs).Returns(new List<string> { NaturalSpawnID });

    }
    private void CreateTwoByTwo() {
        farmPrototypeData.tileWidth = 2;
        farmPrototypeData.tileHeight = 2;
        farm.RangeTiles = new HashSet<Tile>();
        farm.Tiles = farm.GetBuildingTiles(World.Current.GetTileAt(farm.StructureRange + 1, farm.StructureRange + 1));
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
        farm.TrySendWorker();
        Assert.AreEqual(0, farm.ReadWorkers.Count);
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
        farm.TrySendWorker();
        Assert.AreEqual(1, farm.ReadWorkers.Count);
        Assert.AreEqual(ItemProvider.Wood.ID, farm.ReadWorkers[0].toGetItems[0].ID);
        Assert.AreEqual(tile.Structure, farm.ReadWorkers[0].WorkStructure);

    }
}
