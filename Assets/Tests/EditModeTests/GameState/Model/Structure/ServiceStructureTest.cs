using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static Andja.Model.HomeStructure;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class ServiceStructureTest {
    private const string ID = "road";
    TestServiceStructure Service;
    ServiceStructurePrototypeData PrototypeData;
    private MockUtil mockutil;
    [SetUp]
    public void SetUp() {
        Service = new TestServiceStructure(ID, PrototypeData) {
        };
        PrototypeData = new ServiceStructurePrototypeData() {
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        Service.City = mockutil.City;
        CreateTwoByTwo();
    }
    private void CreateTwoByTwo() {
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 2;
        Service.Tiles = Service.GetBuildingTiles(World.Current.GetTileAt(1, 1));
    }


    class TestServiceStructure : ServiceStructure {

        public TestServiceStructure(string iD, ServiceStructurePrototypeData prototypeData) : base(iD, prototypeData) {
        }

    }
}
