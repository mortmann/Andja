using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static Andja.Model.HomeStructure;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class RoadStructureTest {
    private const string ID = "road";
    RoadStructure Road;
    RoadStructurePrototypeData PrototypeData;
    private MockUtil mockutil;
    [SetUp]
    public void SetUp() {
        Road = new RoadStructure(ID, PrototypeData) {
        };
        PrototypeData = new RoadStructurePrototypeData() {
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        Road.City = mockutil.City;
        CreateOneByOne();
    }

    private void CreateOneByOne() {
        PrototypeData.tileWidth = 1;
        PrototypeData.tileHeight = 1;
        Road.Tiles = Road.GetBuildingTiles(World.Current.GetTileAt(1, 1));
        HashSet<Tile> tiles = new HashSet<Tile>();
        tiles.Add(World.Current.GetTileAt(1, 0));
        tiles.Add(World.Current.GetTileAt(1, 2));
        tiles.Add(World.Current.GetTileAt(0, 1));
        tiles.Add(World.Current.GetTileAt(2, 1));
        Road.NeighbourTiles = tiles;
    }
    [Theory]
    [TestCase("")]
    [TestCase("S")]
    [TestCase("NS")]
    [TestCase("NESW")]
    [TestCase("EW")]
    public void UpdateOrientation(string neighbourString) {
        List<Tile> tiles = new List<Tile>();
        for (int i = 0; i < neighbourString.Length; i++) {
            switch (neighbourString.ToCharArray()[i]) {
                case 'S':
                    tiles.Add(World.Current.GetTileAt(1, 0));
                    break;
                case 'N':
                    tiles.Add(World.Current.GetTileAt(1, 2));
                    break;
                case 'W':
                    tiles.Add(World.Current.GetTileAt(0, 1));
                    break;
                case 'E':
                    tiles.Add(World.Current.GetTileAt(2, 1));
                    break;
            }
        }
        tiles.ForEach(t => t.Structure = new RoadStructure(ID, PrototypeData));
        AssertThat(RoadStructure.UpdateOrientation(Road.BuildTile, tiles)).IsEqualTo("_" + neighbourString);
    }

    [Test]
    public void UpdateOrientation_NonStaticCallsChangedCB() {
        Road.RegisterOnRoadCallback(mockutil.Callbacks.Object.Structure);
        List<Tile> tiles = new List<Tile>();
        tiles.ForEach(t => t.Structure = new RoadStructure(ID, PrototypeData));
        Road.UpdateOrientation();
        AssertThat(Road.connectOrientation).IsEqualTo("_");
        AssertThat(mockutil.Callbacks).HasInvoked(c => c.Structure((Road))).Once();
    }

    [Test]
    public void OnBuild_NoNeighbours() {
        Road.OnBuild();
        AssertThat(Road.Route).IsNotNull();
        AssertThat(Road.Route.Tiles).ContainsExactly(Road.BuildTile);
        AssertThat(mockutil.CityMock).HasInvoked(c=>c.AddRoute(Road.Route));
    }

    [Test]
    public void OnBuild_OneRouteNeighbours() {
        Tile t = World.Current.GetTileAt(1, 0);
        RoadStructure road = new RoadStructure(ID, PrototypeData);
        road.Route = new Route();
        road.Route.Tiles = new List<Tile>();
        road.Tiles = new List<Tile>() { t };
        t.Structure = road;
        mockutil.CityMock.SetupGet(c => c.Routes).Returns(new List<Route> { road.Route });
        Road.OnBuild();
        AssertThat(Road.Route).IsEqualTo(road.Route);
        AssertThat(road.RoadsAroundStructure()).Contains(Road);
        AssertThat(road.Route.Tiles).Contains(Road.BuildTile);
    }
    [Test]
    public void OnBuild_FourSingleRouteNeighbours() {
        List<Tile> tiles = new List<Tile>();
        tiles.Add(World.Current.GetTileAt(1, 0));
        tiles.Add(World.Current.GetTileAt(1, 2));
        tiles.Add(World.Current.GetTileAt(0, 1));
        tiles.Add(World.Current.GetTileAt(2, 1));
        tiles.Add(Road.BuildTile);
        tiles.ForEach(t => {
            RoadStructure road = new RoadStructure(ID, PrototypeData);
            road.City = mockutil.City;
            road.Route = new Route();
            road.Route.Tiles = new List<Tile>();
            t.Structure = road;
        });
        Road.OnBuild();

        AssertThat(Road.Route).IsNotNull();
        AssertThat(Road.Route.Tiles).ContainsExactlyInAnyOrder(Road.BuildTile);
    }

    [Test]
    public void OnDestroy() {
        List<Tile> tiles = new List<Tile>();
        tiles.Add(World.Current.GetTileAt(1, 0));
        tiles.Add(World.Current.GetTileAt(1, 2));
        tiles.Add(World.Current.GetTileAt(0, 1));
        tiles.Add(World.Current.GetTileAt(2, 1));
        Route r = new Route();
        r.Tiles = new List<Tile>();
        tiles.ForEach(t => {
            RoadStructure road = new RoadStructure(ID, PrototypeData);
            road.City = mockutil.City;
            road.Route = r;
            road.Tiles = new List<Tile>() { t };
            r.Tiles.Add(t);
            t.Structure = road;
        });
        tiles.ForEach(x=> {
            var r = x.Structure as RoadStructure;
            r.UpdateOrientation();
        });
        Road.OnDestroy();
        AssertThat(tiles.Select(x => x.Structure as RoadStructure)).AllSatisfy(t => t.connectOrientation == "_");
    }

    [Test]
    public void RouteChange_Callback() {
        Route old = new Route();
        Road.Route = old;
        Road.RegisterOnRouteCallback(mockutil.Callbacks.Object.RouteChange);
        Road.Route = new Route();
        AssertThat(mockutil.Callbacks).HasInvoked(c => c.RouteChange(old, Road.Route));
    }

}
