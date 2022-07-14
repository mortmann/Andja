using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class MineStructureTest {
    string MineID = "Mine";
    MineStructure Mine;
    MinePrototypeData MinePrototypeData;
    private MockUtil mockutil;
    IIsland Island;

    [SetUp]
    public void SetUp() {
        Mine = new MineStructure(MineID, MinePrototypeData);
        MinePrototypeData = new MinePrototypeData() {
            ID = MineID,
            produceTime = 2f,
            maxOutputStorage = 2,
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(MineID)).Returns(MinePrototypeData);
        Island = mockutil.WorldIsland;
    }
    private void CreateTwoByThree() {
        MineStructure.CurrentResourceMode = ResourceMode.PerMine;
        Mine.City = mockutil.City;
        MinePrototypeData.tileWidth = 2;
        MinePrototypeData.tileHeight = 3;
        Island.Resources = new Dictionary<string, int>();
        MinePrototypeData.output = new Item[] { ItemProvider.Stone_1 };
        Mine.Tiles = Mine.GetBuildingTiles(World.Current.GetTileAt(Mine.StructureRange + 1, Mine.StructureRange + 1));
    }

    [Test] 
    public void OnBuild_PerMine_ShouldRemoveResourceFromIsland() {
        CreateTwoByThree();
        Island.Resources[ItemProvider.Stone.ID] = 1;
        Mine.OnBuild();
        Assert.AreEqual(0, Island.Resources[ItemProvider.Stone.ID]);
    }

    [Test]
    public void OnDestroy_PerMine_ShouldAddResourceFromIsland() {
        CreateTwoByThree();
        Island.Resources[ItemProvider.Stone.ID] = 0;
        Mine.OnDestroy();
        Assert.AreEqual(1, Island.Resources[ItemProvider.Stone.ID]);
    }

    [Test]
    public void SpecialCheck_PerMine() {
        CreateTwoByThree();
        Assert.IsFalse(Mine.SpecialCheckForBuild(Mine.Tiles));
        Island.Resources[ItemProvider.Stone.ID] = 1;
        Assert.IsTrue(Mine.SpecialCheckForBuild(Mine.Tiles));
    }

    [Test]
    public void OnUpdate_PerMine() {
        CreateTwoByThree();
        for (int i = 0; i < 2; i++) {
            Mine.OnUpdate(1f);
        }
        Assert.AreEqual(1, Mine.Output[0].count);
    }
    [Test]
    public void OnUpdate_PerMine_CapAtMaxOutput() {
        CreateTwoByThree();
        for (int i = 0; i < 10; i++) {
            Mine.OnUpdate(1f);
        }
        Assert.AreEqual(2, Mine.Output[0].count);
    }
    [Test]
    public void OnUpdate_PerProduce() {
        CreateTwoByThree();
        Island.Resources[ItemProvider.Stone.ID] = 1;
        MineStructure.CurrentResourceMode = ResourceMode.PerProduce;
        for (int i = 0; i < 2; i++) {
            Mine.OnUpdate(1f);
        }
        Assert.AreEqual(1, Mine.Output[0].count);
        Assert.AreEqual(0, Island.Resources[Mine.Resource]);
    }
    [Test]
    public void OnUpdate_PerProduce_NoResource_NotWorking() {
        CreateTwoByThree();
        MineStructure.CurrentResourceMode = ResourceMode.PerProduce;
        for (int i = 0; i < 2; i++) {
            Mine.OnUpdate(1f);
        }
        Assert.AreEqual(0, Mine.Output[0].count);
    }
    [Test]
    public void OnUpdate_PerProduce_CapAtMaxOutput() {
        CreateTwoByThree();
        Island.Resources[ItemProvider.Stone.ID] = 5;
        MineStructure.CurrentResourceMode = ResourceMode.PerProduce;
        for (int i = 0; i < 12; i++) {
            Mine.OnUpdate(1f);
        }
        Assert.AreEqual(2, Mine.Output[0].count);
    }
}
