using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;
using static UnityEngine.Networking.UnityWebRequest;


public class PopulationLevelTest {
    private MockUtil MockUtil;
    PopulationLevel Level;
    PopulationLevelPrototypData PrototypData;
    Mock<INeedGroup> NeedGroupMock;
    Mock<INeedGroup> NeedGroupMock2;

    [SetUp]
    public void Setup() {
        MockUtil = new MockUtil();
        NeedGroupMock = new Mock<INeedGroup>();
        NeedGroupMock.SetupGet(g => g.ID).Returns("NeedGroupPrototypeData");
        NeedGroupMock.Setup(g => g.CloneEmptyList()).Returns(() => NeedGroupMock.Object);
        NeedGroupMock.Setup(g => g.HasNeed(It.IsAny<Need>())).Returns(false);
        NeedGroupMock2 = new Mock<INeedGroup>();
        NeedGroupMock2.SetupGet(g => g.ID).Returns("NeedGroupPrototypeData2");
        NeedGroupMock2.Setup(g => g.CloneEmptyList()).Returns(() => NeedGroupMock2.Object);

        PrototypData = new PopulationLevelPrototypData() {
            needGroupList = new List<INeedGroup>() { NeedGroupMock.Object, NeedGroupMock2.Object }
        };
        MockUtil.PrototypControllerMock.Setup(p => p.GetPopulationLevelPrototypDataForLevel(0)).Returns(PrototypData);
        MockUtil.PrototypControllerMock.Setup(p => p.GetNeedPrototypDataForID(It.IsAny<string>()))
            .Returns(new NeedPrototypeData() { group = new NeedGroupPrototypeData() { ID = "NeedGroupPrototypeData" } });

        MockUtil.CityMock.Setup(c => c.GetOwner()).Returns(MockUtil.Player);
        MockUtil.IPlayerMock.SetupGet(p => p.UnlockedItemNeeds).Returns(new HashSet<string>[] { new HashSet<string>() });
        MockUtil.IPlayerMock.SetupGet(p => p.UnlockedStructureNeeds).Returns(new HashSet<string>[] { new HashSet<string>() });
        Level = new PopulationLevel(0, MockUtil.City, null);
    }

    [Test]
    public void Constructor() {
        AssertThat(MockUtil.IPlayerMock).HasInvoked(p => p.RegisterNeedUnlock(It.IsAny<Action<Need>>()));
        AssertThat(Level.AllNeedGroupList).HasSize(2);
    }

    [Test]
    public void Load() {
        MockUtil.IPlayerMock.SetupGet(p => p.UnlockedItemNeeds).Returns(new HashSet<string>[] { new HashSet<string> { "id", "id2" } });
        MockUtil.IPlayerMock.SetupGet(p => p.UnlockedStructureNeeds).Returns(new HashSet<string>[] { new HashSet<string> { "id3", "id4" } });
        Level.RegisterNeedUnlock(MockUtil.Callbacks.Object.NeedUnlock);
        PrototypData.needGroupList.Clear();
        var newNeedGroupMock = new Mock<INeedGroup>();
        newNeedGroupMock.SetupGet(g => g.ID).Returns("NeedGroupPrototypeData3");
        newNeedGroupMock.Setup(g => g.CloneEmptyList()).Returns(() => newNeedGroupMock.Object);
        PrototypData.needGroupList.Add(newNeedGroupMock.Object);
        PrototypData.needGroupList.Add(NeedGroupMock.Object);
        Level.Load(MockUtil.City);

        AssertThat(MockUtil.IPlayerMock).HasInvoked(p => p.RegisterNeedUnlock(It.IsAny<Action<Need>>()));
        AssertThat(Level.AllNeedGroupList).ContainsExactlyInAnyOrder(NeedGroupMock.Object, newNeedGroupMock.Object);
        AssertThat(MockUtil.Callbacks).HasInvoked(c => c.NeedUnlock(It.IsAny<Need>())).Exactly(4);
    }
    [Test]
    public void AddPeople() {
        Level.AddPeople(5);
        AssertThat(Level.PopulationCount).IsEqualTo(5);
        AssertThat(MockUtil.IPlayerMock).HasInvoked(p => p.UpdateMaxPopulationCount(0,5));
    }
    [Theory]
    [TestCase(0, 100, 100, 0)]
    [TestCase(0.5f, 100, 100, 5000)]
    [TestCase(0.5f, 100, 0, 0)]
    [TestCase(0.5f, 0, 100, 0)]
    [TestCase(2f, 100, 100, 20000)]
    public void GetIncome(float taxPercentage, int TaxPerPerson, int PopulationCount, int result) {
        PrototypData.taxPerPerson = TaxPerPerson;
        Level.SetTaxPercentage(taxPercentage);
        Level.AddPeople(PopulationCount);
        AssertThat(Level.GetTaxIncome()).IsEqualTo(result);
    }

    [Test]
    public void FulfillNeedsAndCalcHappiness() {
        Level.FulfillNeedsAndCalcHappiness();

        AssertThat(NeedGroupMock).HasInvoked(g => g.CalculateFulfillment(MockUtil.City, Level));
        AssertThat(NeedGroupMock2).HasInvoked(g => g.CalculateFulfillment(MockUtil.City, Level));
    }
}
