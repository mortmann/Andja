using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Andja.Controller;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class CityTest {

    TestCity PlayerCity;
    MockUtil MockUtil;
    [SetUp]
    public void SetUp() {
        MockUtil = new MockUtil().WithOnePopulationLevels();
        PlayerCity = new TestCity(0, MockUtil.IslandMock.Object);
    }

    [Test]
    public void StuffThatHappensOnCreate() {
        AssertThat(MockUtil.IslandMock).HasInvoked(i => i.RegisterOnEvent(PlayerCity.OnEventCreate, PlayerCity.OnEventEnded));
        AssertThat(PlayerCity.PopulationLevels.Count).IsEqualTo(1);
    }

    [Test]
    public void Update_Empty() {
        PlayerCity.Update(100f);
        AssertThat(PlayerCity.Expanses).IsEqualTo(0);
        AssertThat(PlayerCity.Income).IsEqualTo(0);
        AssertThat(PlayerCity.UseTickTimer).IsEqualTo(City.UseTick);
    }

    [Test]
    public void Update_WithHome() {
        var plpd = PrototypController.Instance.GetPopulationLevelPrototypDataForLevel(0);
        var home = new Mock<IHomeStructure>();
        home.SetupGet(home => home.People).Returns(1);
        PlayerCity.AddHome(home.Object);
        PlayerCity.Update(City.UseTick - 1f);
        PlayerCity.PopulationLevels[0].PopulationCount = 1;
        AssertThat(home).HasInvoked(h => h.OnUpdate(City.UseTick - 1f));
        AssertThat(PlayerCity.UseTickTimer).IsEqualTo(1);
        PlayerCity.Update(1f);
        AssertThat(PlayerCity.Expanses).IsEqualTo(0);
        AssertThat(PlayerCity.Income).IsEqualTo(plpd.taxPerPerson);
        AssertThat(PlayerCity.UseTickTimer).IsEqualTo(City.UseTick);
    }
    class TestCity : City {
        public TestCity(int playerNr, IIsland island) : base(playerNr, island) {

        }
        public void AddHome(IHomeStructure ihome) {
            Homes.Add(ihome);
        }
    }
}