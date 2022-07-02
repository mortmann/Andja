using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;

public class NeedStructureTest {

    private const string ID = "farm";
    NeedStructure NeedStructure;
    NeedStructurePrototypeData PrototypeData;

    [SetUp]
    public void SetUp() {
        NeedStructure = new NeedStructure(ID, PrototypeData) {
        };
        PrototypeData = new NeedStructurePrototypeData() {
            structureRange = 5
        };
        MockUtil mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        NeedStructure.City = mockutil.WorldCity;
        CreateTwoByTwo();
    }
    private void CreateTwoByTwo() {
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 2;
        NeedStructure.RangeTiles = new HashSet<Tile>();
        NeedStructure.Tiles = NeedStructure.GetBuildingTiles(World.Current.GetTileAt(NeedStructure.StructureRange, NeedStructure.StructureRange));
        NeedStructure.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }
    [Test]
    public void OnBuild() {
        NeedStructure.OnBuild();
        Assert.IsTrue(NeedStructure.RangeTiles
                            .All(x => x.GetListOfInRangeNeedStructures(NeedStructure.City.PlayerNumber)
                            .Contains(NeedStructure)));
    }
}
