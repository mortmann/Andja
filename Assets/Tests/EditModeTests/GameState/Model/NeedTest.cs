using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class NeedTest {
    private const float UsageAmount = 0.25f;
    private MockUtil MockUtil;
    NeedPrototypeData PrototypeData;
    Need Need;
    readonly string ID = "test";
    [SetUp]
    public void SetUp() {
        MockUtil = new MockUtil().WithPopulationLevelMock();
        PrototypeData = new NeedPrototypeData {
            item = ItemProvider.Fish_1,
            UsageAmounts = new float[] { UsageAmount }
        };
        MockUtil.PrototypControllerMock.Setup(p => p.GetNeedPrototypDataForID(ID)).Returns(() => PrototypeData);
        MockUtil.PrototypControllerMock.SetupGet(p => p.NumberOfPopulationLevels).Returns(() => 1);

        Need = new Need(ID, PrototypeData);
    }

    [Theory]
    [TestCase(0, 0, 1)]
    [TestCase(0, 1, 0)]
    [TestCase(50, 100, 1)]
    [TestCase(50, 1000, 0.20f)]
    public void CalculateFulfillment(int inInventory, int popCount, float availability) {
        MockUtil.CityMock.Setup(c => c.GetAmountForThis(It.Is<Item>(i => i.ID == ItemProvider.Fish.ID))).Returns(inInventory);
        MockUtil.PopulationMock.SetupGet(c => c.PopulationCount).Returns(popCount);

        Need.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel);

        AssertThat(MockUtil.CityMock).HasInvoked(c => 
            c.RemoveItem(It.Is<Item>(i => i.ID == ItemProvider.Fish.ID),
            Mathf.CeilToInt(UsageAmount * popCount))
            );
        AssertThat(Need.PercentageAvailability[MockUtil.PopulationLevel.Level]).IsEqualTo(availability);
    }
    [Test]
    public void IsSatisfiedThroughStructure() {
        PrototypeData.structures = new NeedStructure[] { new NeedStructure("testID") };

        AssertThat(Need.IsSatisfiedThroughStructure(new System.Collections.Generic.List<NeedStructure>() {
            new NeedStructure("testID")
        })).IsTrue();
    }
    [Test]
    public void IsSatisfiedThroughStructure_IsFalse() {
        PrototypeData.structures = new NeedStructure[] { new NeedStructure("testID") };

        AssertThat(Need.IsSatisfiedThroughStructure(new System.Collections.Generic.List<NeedStructure>(){
            new NeedStructure("testID2")
        })).IsFalse();
    }
}
