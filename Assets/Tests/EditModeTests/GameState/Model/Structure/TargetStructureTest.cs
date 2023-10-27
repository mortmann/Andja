using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Andja;
using Andja.Controller;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class TargetStructureTest {
    private const string ID = "service";
    TestTargetStructure Target;
    StructurePrototypeData PrototypeData;
    private Mock<IPrototypController> prototypeControllerMock;
    private IWarfare iWarfare;
    private MockUtil mockutil;
    ArmorType armor = new ArmorType() { ID = "Armor" };
    [SetUp]
    public void SetUp() {
        Target = new TestTargetStructure(ID) {
        };
        PrototypeData = new StructurePrototypeData {
            maxHealth = 1000,
            canTakeDamage = true,
        };
        mockutil = new MockUtil();
        prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(p => p.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        prototypeControllerMock.SetupGet(p => p.StructureArmor).Returns(armor);
        iWarfare = mockutil.IWarfareMock.Object;
        
        mockutil.CityMock.SetupGet(c => c.PlayerNumber).Returns(1);
        Target.City = mockutil.City;
        Target.Tiles = new List<Tile>();
    }

    [Test]
    public void IsAttackableFrom() {
        mockutil.IWarfareMock.SetupGet(w => w.DamageType).Returns(new DamageType {
            damageMultiplier = new Dictionary<ArmorType, float> {
                {armor, 1}
            }
        });
        mockutil.IWarfareMock.Setup(w => w.GetCurrentDamage(PrototypController.Instance.StructureArmor)).Returns(0.1f);
        AssertThat(Target.IsAttackableFrom(iWarfare)).IsTrue();

    }
    [Test]
    public void IsAttackableFrom_DamageZero() {
        mockutil.IWarfareMock.SetupGet(w => w.DamageType).Returns(new DamageType {
            damageMultiplier = new Dictionary<ArmorType, float> {
                {armor, 0}
            }
        });
        mockutil.IWarfareMock.Setup(w => w.GetCurrentDamage(PrototypController.Instance.StructureArmor)).Returns(0);
        AssertThat(Target.IsAttackableFrom(iWarfare)).IsFalse();
    }
    [Test]
    public void IsAttackableFrom_CanTakeDamageFalse_Not() {
        mockutil.IWarfareMock.SetupGet(w => w.DamageType).Returns(new DamageType {
            damageMultiplier = new Dictionary<ArmorType, float> {
                {armor, 1}
            }
        });
        PrototypeData.canTakeDamage = false;
        mockutil.IWarfareMock.Setup(w => w.GetCurrentDamage(PrototypController.Instance.StructureArmor)).Returns(0);
        AssertThat(Target.IsAttackableFrom(iWarfare)).IsFalse();
    }
    [Test]
    public void TakeDamageFrom() {
        Target.CurrentHealth = 150;
        mockutil.IWarfareMock.Setup(w => w.GetCurrentDamage(PrototypController.Instance.StructureArmor)).Returns(50);
        Target.TakeDamageFrom(iWarfare);
        AssertThat(Target.CurrentHealth).IsEqualTo(100);
    }
    [Test]
    public void TakeDamageFrom_Destroyed() {
        Target.CurrentHealth = 150;
        mockutil.IWarfareMock.Setup(w => w.GetCurrentDamage(PrototypController.Instance.StructureArmor)).Returns(151);
        Target.TakeDamageFrom(iWarfare);
        AssertThat(Target.CurrentHealth).IsLesserThanOrEqualTo(0);
        AssertThat(Target.IsDestroyed).IsTrue();
    }
    public class TestTargetStructure : TargetStructure {
        public TestTargetStructure(string iD) {
            ID = iD;
        }


        public override Structure Clone() {
            throw new NotImplementedException();
        }

        public override void OnBuild(bool loading = false) {
            throw new NotImplementedException();
        }
    }
}
