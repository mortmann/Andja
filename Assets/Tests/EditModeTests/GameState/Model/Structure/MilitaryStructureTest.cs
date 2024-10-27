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
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class MilitaryStructureTest {
    string ID = "MilitaryStructure";
    TestMilitary Military;
    MilitaryPrototypeData PrototypeData;
    private MockUtil mockutil;
    string UnitID = "UnitID";
    Unit Unit;
    UnitPrototypeData UnitPrototypeData;

    [SetUp]
    public void SetUp() {
        Military = new TestMilitary(ID, null);
        PrototypeData = new MilitaryPrototypeData() {
            ID = ID,
            structureRange = 4,
            buildTimeModifier = 1,
            buildQueueLength = 2,
            damage = 5,
            attackRange = 6,
            projectileSpeed = 10
        };
        Unit = new Unit(UnitID, null);
        UnitPrototypeData = new UnitPrototypeData() {
            ID = ID,
            buildingItems = new Item[] { ItemProvider.Wood_5, ItemProvider.Tool_5 },
            buildTime = 2f,
        };

        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        prototypeControllerMock.Setup(m => m.GetUnitPrototypDataForID(UnitID)).Returns(() => UnitPrototypeData);

        CreateFourByFour();
    }
    private void CreateFourByFour() {
        Military.City = mockutil.City;
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;
        Military.Tiles = Military.GetBuildingTiles(World.Current.GetTileAt(Military.StructureRange, Military.StructureRange));
        Military.RangeTiles = new HashSet<Tile>();
        Military.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }
    [Test]
    public void HasEnoughResources() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(),It.IsAny<int>())).Returns(true);
        Assert.IsTrue(Military.HasEnoughResources(Unit));
    }

    [Test]
    public void UpdateBuildUnit() {
        Unit secondInQueue = new Unit("Second", UnitPrototypeData);
        mockutil.PrototypControllerMock.Setup(m => m.GetUnitPrototypDataForID("Second")).Returns(UnitPrototypeData);
        Military.ToBuildUnits = new Queue<Unit>();
        Military.ToBuildUnits.Enqueue(Unit);
        Military.ToBuildUnits.Enqueue(secondInQueue);
        Military.ToPlaceUnitTiles = new List<Tile>() { World.Current.GetTileAt(1,1) };

        for (int i = 0; i < 10; i++) {
            Military.UpdateBuildUnit(0.2f);
        }
        Assert.AreEqual(Military.CurrentlyBuildingUnit.ID, secondInQueue.ID);
        mockutil.WorldMock.Verify(x => x.CreateUnit(Unit, It.IsAny<Player>(), World.Current.GetTileAt(1, 1), 0), Times.Once());
    }

    [Test]
    public void AddUnitToBuildQueue() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.RemoveItems(It.IsAny<Item[]>()));
        Military.ToBuildUnits = new Queue<Unit>();
        Military.AddUnitToBuildQueue(Unit);
        Assert.AreEqual(Military.CurrentlyBuildingUnit, Unit);
    }

    [Test]
    public void OnBuild() {
        Military.NeighbourTiles = new HashSet<Tile> { mockutil.GetInCityTile(1, 1) };

        Military.OnBuild();

        AssertThat(Military.ToPlaceUnitTiles).ContainsExactly(mockutil.GetInCityTile(1, 1));
    }
    [Test]
    public void OnBuild_NoFreeTiles() {
        var tile = mockutil.GetInCityTile(1, 1);
        tile.Structure = new MarketStructure("BLA", new MarketPrototypeData());
        mockutil.PrototypControllerMock.Setup(p => p.GetStructurePrototypDataForID("BLA"))
            .Returns(new MarketPrototypeData());
        Military.NeighbourTiles = new HashSet<Tile> { tile };

        Military.OnBuild();

        AssertThat(Military.ToPlaceUnitTiles).DoesNotContain(tile);
    }
    [Test]
    public void OnBuild_BuildShip() {
        PrototypeData.canBuildShips = true;
        var tile = new Tile();
        mockutil.PrototypControllerMock.Setup(p => p.GetStructurePrototypDataForID("BLA"))
            .Returns(new MarketPrototypeData());
        Military.NeighbourTiles = new HashSet<Tile> { tile };

        Military.OnBuild();

        AssertThat(Military.ToPlaceUnitTiles).ContainsExactly(tile);
    }

    [Test]
    public void UpdateAttackTarget() {
        Military.AttackCooldownTimer = 1;

        Mock<ITargetable> target = new Mock<ITargetable>();
        target.Setup(t => t.IsAttackableFrom(Military)).Returns(true);
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(2, 2));
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        PrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        Military.Target = target.Object;

        Military.UpdateAttackTarget(1);

        AssertThat(Military.AttackCooldownTimer).IsEqualTo(0);

        Military.UpdateAttackTarget(1);

        AssertThat(mockutil.WorldMock).HasInvoked(w => w.OnCreateProjectile(It.IsAny<Projectile>()));
    }
    [Test]
    public void UpdateAttackTarget_NotAtWar() {
        Military.AttackCooldownTimer = 1;

        Mock<ITargetable> target = new Mock<ITargetable>();
        target.Setup(t => t.IsAttackableFrom(Military)).Returns(true);
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(2, 2));
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        PrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        Military.Target = target.Object;

        Military.UpdateAttackTarget(1);

        AssertThat(Military.AttackCooldownTimer).IsEqualTo(1);
    }
    [Test]
    public void UpdateAttackTarget_NoTarget() {
        Military.AttackCooldownTimer = 1;

        Military.UpdateAttackTarget(1);

        AssertThat(Military.AttackCooldownTimer).IsEqualTo(1);
    }
    [Test]
    public void UpdateAttackTarget_TargetNotInRange() {
        Military.AttackCooldownTimer = 1;
        Mock<IWarfare> target = new Mock<IWarfare>();
        target.Setup(t => t.IsAttackableFrom(Military)).Returns(true);
        Military.Target = target.Object;
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        PrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        Military.Target = target.Object;
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(200, 202));
        Military.UpdateAttackTarget(1);

        AssertThat(Military.AttackCooldownTimer).IsEqualTo(1);
    }
    [Test]
    public void UpdateAttackTarget_CannotAttack() {
        Military.AttackCooldownTimer = 1;
        Mock<IWarfare> target = new Mock<IWarfare>();
        target.Setup(t => t.IsAttackableFrom(Military)).Returns(false);
        Military.Target = target.Object;
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(2, 2));
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "cannotDamage" });
        PrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "cannotDamage" }, 0 }
            }
        };
        Military.Target = target.Object;

        Military.UpdateAttackTarget(1);

        AssertThat(Military.AttackCooldownTimer).IsEqualTo(1);
    }
    class TestMilitary : MilitaryStructure {
        public ITargetable Target {
            get => CurrentTarget;
            set => CurrentTarget = value;
        }

        public TestMilitary(string iD, MilitaryPrototypeData mpd) : base(iD, mpd) {
        }
        public Queue<Unit> ToBuildUnits {
            get => toBuildUnits;
            set => toBuildUnits = value;
        }
        public List<Tile> ToPlaceUnitTiles {
            get => toPlaceUnitTiles;
            set => toPlaceUnitTiles = value;
        }

        public float AttackCooldownTimer {
            get => attackCooldownTimer;
            set => attackCooldownTimer = value;
        }
    }

}
