using System;
using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using Andja;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class StructureTest {
    string ID = "Structure";
    private MockUtil mockutil;
    TestStructure Structure;
    StructurePrototypeData PrototypeData;

    [SetUp]
    public void SetUp() {
        PrototypeData = new StructurePrototypeData() {
            ID = ID,
            tileWidth = 1,
            tileHeight = 1,
        };
        Structure = new TestStructure(ID);
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
    }

    [Test]
    public void CheckPlaceStructure_containsNull() {
        AssertThat(Structure.CheckPlaceStructure(new List<Tile> { null }, 1)).IsFalse();
    }
    [Test]
    public void CheckPlaceStructure_empty() {
        AssertThat(Structure.CheckPlaceStructure(new List<Tile> { }, 1)).IsFalse();
    }
    [Test]
    public void CheckPlaceStructure_containsBlockage() {
        var tiles = new List<Tile> { new LandTile(new Tile(), TileType.Mountain) };
        AssertThat(mockutil.BuildControllerMock)
            .HasInvoked(b => b.BuildError(MapErrorMessage.NoSpace, tiles, Structure, 0));
        AssertThat(Structure.CheckPlaceStructure(tiles, 1)).IsFalse();
    }
    [Test]
    public void CheckPlaceStructure_SpecialCheckFailure() {
        Structure.ReturnSpecialBuild = false;
        var tiles = new List<Tile> { new LandTile(new Tile(), TileType.Grass) };
        AssertThat(mockutil.BuildControllerMock)
            .HasInvoked(b => b.BuildError(MapErrorMessage.CanNotBuildHere, tiles, Structure, 0));
        AssertThat(Structure.CheckPlaceStructure(tiles, 1)).IsFalse();
    }
    [Test]
    public void CheckPlaceStructure() {
        AssertThat(Structure.CheckPlaceStructure(new List<Tile> { new LandTile(new Tile(), TileType.Grass) }, 0)).IsTrue();
    }

    [Test]
    public void InCityCheck() {

        AssertThat(Structure.InCityCheck(new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2) }, 0)).IsTrue();
    }
    [Test]
    public void InCityCheck_50percent() {

        AssertThat(Structure.InCityCheck(new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInOtherCityTile(1, 2) }, 0)).IsTrue();
    }
    [Test]
    public void InCityCheck_45percent() {

        AssertThat(Structure.InCityCheck(new List<Tile> { 
            mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2),
            mockutil.GetInCityTile(1, 3), mockutil.GetInCityTile(1, 4), 
            mockutil.GetInOtherCityTile(1, 5), mockutil.GetInOtherCityTile(1, 6), mockutil.GetInOtherCityTile(1, 7),
            mockutil.GetInOtherCityTile(1, 8), mockutil.GetInOtherCityTile(1, 9), mockutil.GetInOtherCityTile(1, 10), 
        }, 0)).IsFalse();
    }
    [Test]
    public void InCityCheck_33percent() {

        AssertThat(Structure.InCityCheck(new List<Tile> { mockutil.GetInCityTile(1, 1),
            mockutil.GetInOtherCityTile(1, 2), mockutil.GetInOtherCityTile(1, 3) }, 0)).IsFalse();
    }

    [Test]
    public void PlaceStructure() {
        var tiles = new List<Tile> { mockutil.GetInCityTile(1, 1) };
        PrototypeData.maxHealth = 100;
        Structure.City = mockutil.City;

        Structure.PlaceStructure(tiles, false);

        AssertThat(Structure.Tiles).ContainsExactlyInAnyOrder(tiles);
        AssertThat(Structure.CurrentHealth).IsEqualTo(PrototypeData.maxHealth);
        AssertThat(Structure.OnBuildCalled).IsTrue();
        AssertThat(Structure.RangeTiles).IsNull();
        AssertThat(Structure.NeighbourTiles).ContainsExactlyInAnyOrder(
            mockutil.GetInCityTile(1, 0), mockutil.GetInCityTile(0, 1),
            mockutil.GetInCityTile(2, 1), mockutil.GetInCityTile(1, 2)
        );
        AssertThat(mockutil.CityMock)
            .HasInvoked(c => c.RegisterOnEvent(It.IsAny<Action<GameEvent>>(), It.IsAny<Action<GameEvent>>())).Once();
    }

    [Test]
    public void CalculateNeighbourTiles() {
        var tiles = new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2), 
                                     mockutil.GetInCityTile(2, 1), mockutil.GetInCityTile(2, 2) };
        Structure.Tiles = tiles;

        Structure.TestCalculateNeighbourTiles();

        AssertThat(Structure.NeighbourTiles).ContainsExactlyInAnyOrder(
            mockutil.GetInCityTile(1, 0), mockutil.GetInCityTile(0, 1),
            mockutil.GetInCityTile(2, 0), mockutil.GetInCityTile(0, 2),
            mockutil.GetInCityTile(3, 1), mockutil.GetInCityTile(1, 3),
            mockutil.GetInCityTile(3, 2), mockutil.GetInCityTile(2, 3)
        );

    }
    [Test]
    public void CalculateNeighbourTiles_nonSquare() {
        var tiles = new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2) };
        Structure.Tiles = tiles;

        Structure.TestCalculateNeighbourTiles();

        AssertThat(Structure.NeighbourTiles).ContainsExactlyInAnyOrder(
            mockutil.GetInCityTile(0, 1), mockutil.GetInCityTile(1, 0),
            mockutil.GetInCityTile(2, 1), mockutil.GetInCityTile(2, 2),
            mockutil.GetInCityTile(1, 3), mockutil.GetInCityTile(0, 2)
        );
    }

    [Test]
    public void DecideClimateSprite() {
        Structure.Tiles = new List<Tile> { mockutil.GetInCityTile(0, 0)};
        StructureSpriteController.StructureSprites = new Dictionary<string, Sprite>();
        string[] test = new[] { "test", "yep", "whatever" };
        UnityEngine.Random.InitState(0);
        Structure.TestDecideClimateSprite(Climate.Cold, test);
        AssertThat(Structure.SpriteVariant).IsIn(test);
        UnityEngine.Random.InitState(4);
        string previous = Structure.SpriteVariant;
        Structure.TestDecideClimateSprite(Climate.Cold, test);
        AssertThat(Structure.SpriteVariant).IsNotEqualTo(previous);
    }

    [Test]
    public void IsTileCityViable() {
        AssertThat(Andja.Model.Structure.IsTileCityViable(mockutil.GetInCityTile(0, 0), 0)).IsTrue();
    }
    [Test]
    public void IsTileCityViable_NotInTheCorrect() {
        AssertThat(Andja.Model.Structure.IsTileCityViable(mockutil.GetInCityTile(0, 0), 1)).IsFalse();
    }
    [Test]
    public void IsTileCityViable_Wilderness_Viable() {
        LandTile lt = new LandTile(0, 0);
        mockutil.CityMock.Setup(x => x.IsWilderness()).Returns(true);
        lt.Island = mockutil.WorldIsland;
        lt.City = mockutil.City;
        AssertThat(Andja.Model.Structure.IsTileCityViable(lt, 0)).IsTrue();
    }

    [Test]
    public void GetBuildingTiles() {
        var tiles = new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2),
                                     mockutil.GetInCityTile(2, 1), mockutil.GetInCityTile(2, 2) };
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 2;
        AssertThat(Structure.GetBuildingTiles(mockutil.GetInCityTile(1, 1))).ContainsExactly(tiles);
    }
    [Test]
    public void GetBuildingTiles_IgnoreRotation() {
        var tiles = new List<Tile> { mockutil.GetInCityTile(1, 1), mockutil.GetInCityTile(1, 2) };
        PrototypeData.tileWidth = 1;
        PrototypeData.tileHeight = 2;
        Structure.ChangeRotation(90);
        AssertThat(Structure.GetBuildingTiles(mockutil.GetInCityTile(1, 1), true)).ContainsExactly(tiles);
    }

    [Test]
    public void GetInRangeTiles() {
        PrototypeData.tileWidth = 1;
        PrototypeData.tileHeight = 1;
        PrototypeData.structureRange = 1;
        var tiles = Structure.GetInRangeTiles(mockutil.GetInCityTile(5, 5));
        AssertThat(tiles).IsNotEmpty();
        AssertThat(tiles).ContainsExactlyInAnyOrder(new Tile[] { 
            mockutil.GetInCityTile(4, 5), mockutil.GetInCityTile(6, 5),
            mockutil.GetInCityTile(5, 4), mockutil.GetInCityTile(5, 6) });

    }

    [Test]
    public void GetNeighbourStructuresInTileDistance_1Tile() {
        var tiles = new Tile[] {
            mockutil.GetInCityTile(4, 5), mockutil.GetInCityTile(6, 5),
            mockutil.GetInCityTile(5, 4), mockutil.GetInCityTile(5, 6),
            mockutil.GetInCityTile(6, 7) };
        Structure.Tiles = new List<Tile>{mockutil.GetInCityTile(5, 5)};
        tiles[0].Structure = new TestStructure(ID);
        tiles[3].Structure = new TestStructure(ID);
        tiles[4].Structure = new TestStructure(ID);
        AssertThat(Structure.GetNeighbourStructuresInTileDistance(1))
            .ContainsExactlyInAnyOrder(tiles[0].Structure, tiles[3].Structure).DoesNotContain(tiles[4].Structure);
    }
    [Test]
    public void GetNeighbourStructuresInTileDistance_2Tile() {
        var tiles = new Tile[] {
            mockutil.GetInCityTile(4, 5), mockutil.GetInCityTile(6, 5),
            mockutil.GetInCityTile(5, 4), mockutil.GetInCityTile(5, 6), 
            mockutil.GetInCityTile(6, 7), mockutil.GetInCityTile(7, 8) };
        Structure.Tiles = new List<Tile> { mockutil.GetInCityTile(5, 5) };
        tiles[0].Structure = new TestStructure(ID);
        tiles[3].Structure = new TestStructure(ID);
        tiles[4].Structure = new TestStructure(ID);
        tiles[5].Structure = new TestStructure(ID);

        AssertThat(Structure.GetNeighbourStructuresInTileDistance(2))
            .ContainsExactlyInAnyOrder(tiles[0].Structure, tiles[3].Structure, tiles[4].Structure)
            .DoesNotContain(tiles[5].Structure);
    }
    [Test]
    public void CanBuildOnSpot() {
        AssertThat(Structure.CanBuildOnSpot(new List<Tile> { mockutil.GetInCityTile(1, 1) })).IsTrue();
    }
    [Test]
    public void CanBuildOnSpot_NotAble() {
        AssertThat(Structure.CanBuildOnSpot(new List<Tile> { new LandTile(1,1){Type = TileType.Mountain} })).IsFalse();
    }
    [Test]
    public void CheckForCorrectSpot() {
        AssertThat(Structure.CheckForCorrectSpot(new List<Tile> { mockutil.GetInCityTile(1, 1) }))
            .Contains(new KeyValuePair<Tile, bool>( mockutil.GetInCityTile(1, 1), true));
    }
    [Test]
    public void CheckForCorrectSpot_False() {
        LandTile lt = mockutil.GetInCityTile(1, 1);
        lt.Type = TileType.Volcano;
        AssertThat(Structure.CheckForCorrectSpot(new List<Tile> { lt }))
            .Contains(new KeyValuePair<Tile, bool>(mockutil.GetInCityTile(1, 1), false));
    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles() {
        TwoByTwoWithBuildTileTypes();
        var Tiles = new List<Tile> {
            GetGrassTile(2, 1), GetMountainTile(2, 2),
            GetGrassTile(1, 1), GetMountainTile(1, 2),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], true }, { Tiles[3], true }
        };
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);
    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_WithNull() {
        PrototypeData.tileHeight = 3;
        PrototypeData.tileWidth = 2;
        PrototypeData.buildTileTypes = new TileType?[,] {
            { TileType.Grass,  TileType.Mountain, TileType.Mountain, },
            { TileType.Grass, TileType.Mountain, null},
        };

        var Tiles = new List<Tile> {
            GetGrassTile(2, 1), GetMountainTile(2, 2), GetDirtTile(2, 3),
            GetGrassTile(1, 1), GetMountainTile(1, 2), GetMountainTile(1, 3),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], true },
            { Tiles[3], true }, { Tiles[4], true }, { Tiles[5], true }
        };
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);
    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_WithNull_NonBuildTile() {
        PrototypeData.tileHeight = 3;
        PrototypeData.tileWidth = 2;
        PrototypeData.buildTileTypes = new TileType?[,] {
            { TileType.Grass,  TileType.Mountain, TileType.Mountain, },
            { TileType.Grass, TileType.Mountain, null},
        };

        var Tiles = new List<Tile> {
            GetGrassTile(2, 1), GetMountainTile(2, 2), GetMountainTile(2, 3),
            GetGrassTile(1, 1), GetMountainTile(1, 2), GetMountainTile(1, 3),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], false },
            { Tiles[3], true }, { Tiles[4], true }, { Tiles[5], true }
        };
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);
    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_Cannot() {
        TwoByTwoWithBuildTileTypes();
        var Tiles = new List<Tile> {
            GetGrassTile(2, 1), GetMountainTile(2, 2),
            GetMountainTile(1, 1), GetMountainTile(1, 2),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], false }, { Tiles[3], true }
        };
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);
    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_90() {
        TwoByTwoWithBuildTileTypes();
        var Tiles = new List<Tile> {
            GetMountainTile(2, 1), GetMountainTile(2, 2),
            GetGrassTile(1, 1), GetGrassTile(1, 2),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], true }, { Tiles[3], true }
        };
        Structure.ChangeRotation(90);
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);
    }

    private void TwoByTwoWithBuildTileTypes() {
        PrototypeData.tileHeight = 2;
        PrototypeData.tileWidth = 2;
        PrototypeData.buildTileTypes = new TileType?[,] {
            { TileType.Grass, TileType.Mountain, },
            { TileType.Grass, TileType.Mountain, },
        };
    }

    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_180() {
        TwoByTwoWithBuildTileTypes();
        var Tiles = new List<Tile> {
            GetMountainTile(2, 1), GetGrassTile(2, 2),
            GetMountainTile(1, 1), GetGrassTile(1, 2),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], true }, { Tiles[3], true }
        };
        Structure.ChangeRotation(180);
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);

    }
    [Test]
    public void CheckForCorrectSpot_HasToBeBuildOnSpecificTiles_270() {
        TwoByTwoWithBuildTileTypes();
        var Tiles = new List<Tile> {
            GetGrassTile(2, 1), GetGrassTile(2, 2),
            GetMountainTile(1, 1), GetMountainTile(1, 2),
        };
        var dic = new Dictionary<Tile, bool> {
            { Tiles[0], true }, { Tiles[1], true }, { Tiles[2], true }, { Tiles[3], true }
        };
        Structure.ChangeRotation(270);
        AssertThat(Structure.CheckForCorrectSpot(Tiles)).ContainsExactlyInAnyOrder(dic);

    }
    [Test]
    public void ReduceHealth() {
        PrototypeData.canTakeDamage = true;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.ReduceHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(25);
    }
    [Test]
    public void ReduceHealth_NegativeValue_NoDamage() {
        PrototypeData.canTakeDamage = true;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.ReduceHealth(-25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(50);
    }
    [Test]
    public void ReduceHealth_Destroyed() {
        PrototypeData.canTakeDamage = true;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50; 
        Structure.City = mockutil.City;
        LandTile t = mockutil.GetInCityTile(1, 1);
        t.Structure = Structure;
        Structure.Tiles = new List<Tile> { t };
        Structure.ReduceHealth(55);
        AssertThat(Structure.CurrentHealth).IsLesserThanOrEqualTo(0);
        AssertThat(Structure.IsDestroyed).IsTrue();
        AssertThat(t.Structure).IsNull();
    }
    [Test]
    public void ReduceHealth_CantTakeDamage() {
        PrototypeData.canTakeDamage = false;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.ReduceHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(50);
    }
    [Test]
    public void RepairHealth() {
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.RepairHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(75);
    }
    [Test]
    public void RepairHealth_IsDestroyed_NoHealNoMore() {
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 0;
        Structure.RepairHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(0);
    }
    [Test]
    public void RepairHealth_Maximum() {
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 500;
        Structure.RepairHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(500);
    }
    [Test]
    public void RepairHealth_NegativeValue_NoDamage() {
        PrototypeData.canTakeDamage = true;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.RepairHealth(-25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(50);
    }
    [Test]
    public void ChangeHealth_Positive() {
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.ChangeHealth(25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(75);
    }
    [Test]
    public void ChangeHealth_Negative() {
        PrototypeData.canTakeDamage = true;
        PrototypeData.maxHealth = 500;
        Structure.CurrentHealth = 50;
        Structure.ChangeHealth(-25);
        AssertThat(Structure.CurrentHealth).IsEqualTo(25);
    }

    [Test]
    public void Destroy() {
        Structure.City = mockutil.City;
        LandTile t = mockutil.GetInCityTile(1, 1);
        t.Structure = Structure;
        Structure.Tiles = new List<Tile> { t };
        Structure.RegisterOnDestroyCallback(mockutil.Callbacks.Object.StructureDestroy);
        Structure.Destroy(mockutil.IWarfareMock.Object, false);
        AssertThat(Structure.CurrentHealth).IsLesserThanOrEqualTo(0);
        AssertThat(Structure.IsDestroyed).IsTrue();
        AssertThat(t.Structure).IsNull();
        AssertThat(Structure.OnDestroyCalled).IsTrue();
        AssertThat(mockutil.Callbacks).HasInvoked(cb=>cb.StructureDestroy(Structure, mockutil.IWarfareMock.Object)).Once();
    }
    [Test]
    public void CanReachStructure() {
        TestStructure other = new TestStructure(ID);
        Route r = new Route();
        other.TestAddRoute(r);
        Structure.TestAddRoute(r);
        AssertThat(Structure.CanReachStructure(other)).IsTrue();
    }
    [Test]
    public void CanReachStructure_False() {
        TestStructure other = new TestStructure(ID);
        other.TestAddRoute(new Route());
        Structure.TestAddRoute(new Route());
        AssertThat(Structure.CanReachStructure(other)).IsFalse();
    }

    [Test]
    public void ChangeRotation() {
        for (int i = 0; i < 5; i++) {
            AssertThat(Structure.ChangeRotation(90 * i)).IsEqualTo((90 * i) % 360);
        }
    }
    [Test]
    public void Rotate() {
        for (int i = 0; i < 5; i++) {
            AssertThat(Structure.Rotation).IsEqualTo((90 * i) % 360);
            Structure.Rotate();
        }
    }

    [Test]
    public void AddTimes90ToRotate() {
        Structure.AddTimes90ToRotate(3);
        AssertThat(Structure.Rotation).IsEqualTo((90 * 3) % 360);

    }
    [Test]
    public void AddRoadStructure() {
        RoadStructure road = new RoadStructure();
        road.Route = new Route();
        Structure.AddRoadStructure(road);

        AssertThat(Structure.RoadsAroundStructure()).Contains(road);
        AssertThat(Structure.GetRoutes()).Contains(road.Route);
    }
    [Test]
    public void OnRouteChange() {
        Route old = new Route();
        Route newer = new Route();
        Structure.TestAddRoute(old);
        Structure.TestOnRouteChange(old, newer);
        AssertThat(Structure.GetRoutes()).Contains(newer);
        AssertThat(Structure.GetRoutes()).DoesNotContain(old);

    }
    [Test]
    public void OnRoadDestroy() {
        RoadStructure road = new RoadStructure();
        road.Route = new Route();
        Structure.TestAddRoute(road.Route);

        Structure.TestOnRoadDestroy(road);

        AssertThat(Structure.RoadsAroundStructure()).DoesNotContain(road);
        AssertThat(Structure.GetRoutes()).DoesNotContain(road.Route);
    }
    [Test]
    public void RemoveRoute() {
        Route old = new Route();

        Structure.RemoveRoute(old);

        AssertThat(Structure.GetRoutes()).DoesNotContain(old);
    }

    [Test]
    public void ToggleActive() {
        AssertThat(Structure.IsActive).IsTrue();
        Structure.ToggleActive();
        AssertThat(Structure.IsActive).IsFalse();
        Structure.ToggleActive();
        AssertThat(Structure.IsActive).IsTrue();
    }

    [Test]
    public void Demolish() {
        Structure.City = mockutil.City;
        Structure.Tiles = new List<Tile>();

        AssertThat(Structure.Demolish()).IsTrue();
    }
    [Test]
    public void Demolish_NegetiveEffect_SoDontDestroy() {
        Structure.TestAddNegativeEffect(new Effect("test",
            new EffectPrototypeData() { classification = EffectClassification.Negative }));


        AssertThat(Structure.Demolish()).IsFalse();

    }
    [Test]
    public void Demolish_NegetiveEffect_ButGod_SoDestroy() {
        Structure.TestAddNegativeEffect(new Effect("test",
            new EffectPrototypeData() { classification = EffectClassification.Negative }));
        Structure.City = mockutil.City;
        Structure.Tiles = new List<Tile>();

        AssertThat(Structure.Demolish(true)).IsTrue();
    }
    private Tile GetMountainTile(int x, int y) {
        LandTile lt = mockutil.GetInCityTile(x, y);
        lt.Type = TileType.Mountain;
        return lt;
    }
    private Tile GetGrassTile(int x, int y) {
        LandTile lt = mockutil.GetInCityTile(x, y);
        lt.Type = TileType.Grass;
        return lt;
    }
    private Tile GetDirtTile(int x, int y) {
        LandTile lt = mockutil.GetInCityTile(x, y);
        lt.Type = TileType.Dirt;
        return lt;
    }
    class TestStructure : Structure {
        public bool ReturnSpecialBuild = true;
        public bool OnBuildCalled = false;
        public bool OnDestroyCalled = false;

        public string SpriteVariant => spriteVariant;
        public TestStructure(string iD) {
            ID = iD;
            
        }

        public override bool SpecialCheckForBuild(List<Tile> tiles) {
            return ReturnSpecialBuild;
        }

        public void TestCalculateNeighbourTiles() {
            CalculateNeighbourTiles();
        }

        public void TestDecideClimateSprite(Climate climate, string[] name) {
            Data.climateSpriteModifier ??= new Dictionary<Climate, string[]>();
            ClimateSpriteModifier[climate] = name;
            DecideClimateSprite();
        }
        public override Structure Clone() {
            throw new System.NotImplementedException();
        }

        public override void OnDestroy() {
            OnDestroyCalled = true;
        }

        public override void OnBuild(bool loading = false) {
            OnBuildCalled = true;
        }

        public void TestAddRoute(Route route) {
            Routes.Add(route);
        }

        public void TestOnRouteChange(Route old, Route newer) {
            OnRouteChange(old, newer);
        }

        public void TestOnRoadDestroy(RoadStructure road) {
            OnRoadDestroy(road,null);
        }

        public void TestAddNegativeEffect(Effect effect) {
            effects ??= new List<Effect>();
            effects.Add(effect);
            HasNegativeEffect = true;
        }
    }
}
