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
    private Attack MilitaryAttack;
    MilitaryPrototypeData PrototypeData;
    private MockUtil mockutil;
    string UnitID = "UnitID";
    Unit Unit;
    UnitPrototypeData UnitPrototypeData;
    private AttackPrototypeData _attackPrototypeData;

    [SetUp]
    public void SetUp() {
        Military = new TestMilitary(ID, null);
        MilitaryAttack = Military.GetElement<Attack>();
        _attackPrototypeData = new AttackPrototypeData() {
            damage = 5,
            attackRange = 6,
            projectileSpeed = 10,
        };
        PrototypeData = new MilitaryPrototypeData() {
            ID = ID,
            structureRange = 4,
            buildTimeModifier = 1,
            buildQueueLength = 2,
            elements = { { typeof(AttackPrototypeData), _attackPrototypeData } }
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
        prototypeControllerMock.Setup(m => m.GetUnitPrototypeDataForID(UnitID)).Returns(() => UnitPrototypeData);

        CreateFourByFour();
    }

    private void CreateFourByFour() {
        Military.City = mockutil.City;
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;
        Military.Tiles =
            Military.GetBuildingTiles(World.Current.GetTileAt(Military.StructureRange, Military.StructureRange));
        Military.RangeTiles = new HashSet<Tile>();
        Military.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void HasEnoughResources() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), It.IsAny<int>())).Returns(true);
        Assert.IsTrue(Military.HasEnoughResources(Unit));
    }

    [Test]
    public void UpdateBuildUnit() {
        Unit secondInQueue = new Unit("Second", UnitPrototypeData);
        mockutil.PrototypControllerMock.Setup(m => m.GetUnitPrototypeDataForID("Second")).Returns(UnitPrototypeData);
        Military.ToBuildUnits = new Queue<Unit>();
        Military.ToBuildUnits.Enqueue(Unit);
        Military.ToBuildUnits.Enqueue(secondInQueue);
        Military.ToPlaceUnitTiles = new List<Tile>() { World.Current.GetTileAt(1, 1) };

        for (int i = 0; i < 10; i++) {
            Military.UpdateBuildUnit(0.2f);
        }

        Assert.AreEqual(((IBaseThing)Military.CurrentlyBuildingUnit).ID, ((IBaseThing)secondInQueue).ID);
        mockutil.WorldMock.Verify(x => x.CreateUnit(Unit, It.IsAny<Player>(), World.Current.GetTileAt(1, 1), 0),
            Times.Once());
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
        tile.Structure = new HomeStructure("BLA", new HomePrototypeData());
        mockutil.PrototypControllerMock.Setup(p => p.GetStructurePrototypDataForID("BLA"))
            .Returns(new HomePrototypeData());
        Military.NeighbourTiles = new HashSet<Tile> { tile };

        Military.OnBuild();

        AssertThat(Military.ToPlaceUnitTiles).DoesNotContain(tile);
    }

    [Test]
    public void OnBuild_BuildShip() {
        PrototypeData.canBuildShips = true;
        var tile = new Tile();
        mockutil.PrototypControllerMock.Setup(p => p.GetStructurePrototypDataForID("BLA"))
            .Returns(new OutputPrototypData());
        Military.NeighbourTiles = new HashSet<Tile> { tile };

        Military.OnBuild();

        AssertThat(Military.ToPlaceUnitTiles).ContainsExactly(tile);
    }

    [Test]
    public void UpdateAttackTarget() {
        MilitaryAttack.Cooldown = 1;
        var target = SetupTarget();
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        _attackPrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        SetTarget(target);

        MilitaryAttack.OnUpdate(1);

        AssertThat(MilitaryAttack.Cooldown).IsEqualTo(0);

        MilitaryAttack.OnUpdate(1);

        AssertThat(mockutil.WorldMock).HasInvoked(w => w.OnCreateProjectile(It.IsAny<Projectile>()));
    }

    private Mock<ITarget> SetupTarget() {
        Mock<ITarget> target = new Mock<ITarget>();
        Mock<IBaseThing> baseTarget = new Mock<IBaseThing>();
        target.Setup(t => t.IsAttackableFrom(MilitaryAttack)).Returns(true);
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(2, 2));
        target.SetupGet(t => t.Parent).Returns(baseTarget.Object);
        return target;
    }

    private void SetTarget(Mock<ITarget> target) {
        Military.AttackCommand = new AttackCommand(target.Object);
    }

    [Test]
    public void UpdateAttackTarget_NotAtWar() {
        MilitaryAttack.Cooldown = 1;

        var target = SetupTarget();
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(false);
        _attackPrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        SetTarget(target);

        MilitaryAttack.OnUpdate(1);

        AssertThat(MilitaryAttack.Cooldown).IsEqualTo(1);
    }

    [Test]
    public void UpdateAttackTarget_NoTarget() {
        MilitaryAttack.Cooldown = 1;

        MilitaryAttack.OnUpdate(1);

        AssertThat(MilitaryAttack.Cooldown).IsEqualTo(1);
    }

    [Test]
    public void UpdateAttackTarget_TargetNotInRange() {
        MilitaryAttack.Cooldown = 1;
        var target = SetupTarget();
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "canDamage" });
        SetTarget(target);
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        _attackPrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "canDamage" }, 1 }
            }
        };
        SetTarget(target);
        target.SetupGet(t => t.CurrentPosition).Returns(new Vector2(200, 202));
        MilitaryAttack.OnUpdate(1);

        AssertThat(MilitaryAttack.Cooldown).IsEqualTo(1);
    }

    [Test]
    public void UpdateAttackTarget_CannotAttack() {
        MilitaryAttack.Cooldown = 1;
        var target = SetupTarget();
        mockutil.PlayerControllerMock.Setup(p => p.ArePlayersAtWar(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        target.SetupGet(t => t.ArmorType).Returns(new ArmorType { ID = "cannotDamage" });
        _attackPrototypeData.damageType = new DamageType() {
            damageMultiplier = new Dictionary<ArmorType, float> {
                { new ArmorType { ID = "cannotDamage" }, 0 }
            }
        };
        SetTarget(target);

        MilitaryAttack.OnUpdate(1);

        AssertThat(MilitaryAttack.Cooldown).IsEqualTo(1);
    }

    class TestMilitary : MilitaryStructure {
        public TestMilitary(string iD, MilitaryPrototypeData mpd) : base(iD, mpd) {
            AddElement(new StructureAttack(this));
        }
        
        public Queue<Unit> ToBuildUnits {
            get => toBuildUnits;
            set => toBuildUnits = value;
        }

        public List<Tile> ToPlaceUnitTiles {
            get => toPlaceUnitTiles;
            set => toPlaceUnitTiles = value;
        }

    }
}