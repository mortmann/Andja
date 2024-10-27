using Andja.Model;
using Moq;
using NUnit.Framework;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class NeedGroupTest {
    private const float UsageAmount = 0.25f;
    private MockUtil MockUtil;
    NeedGroupPrototypeData PrototypeData;
    NeedGroup NeedGroup;
    readonly string ID = "test";
    Mock<INeed> NeedOneMock;
    Mock<INeed> NeedTwoMock;
    Mock<IHomeStructure> IHomeStructureMock;

    [SetUp]
    public void SetUp() {
        MockUtil = new MockUtil().WithPopulationLevelMock();
        PrototypeData = new NeedGroupPrototypeData {
            importanceLevel = 1f,
        };
        MockUtil.PrototypControllerMock.Setup(p => p.GetNeedGroupPrototypDataForID(ID)).Returns(() => PrototypeData);
        MockUtil.PrototypControllerMock.SetupGet(p => p.NumberOfPopulationLevels).Returns(() => 1);
        NeedOneMock = new Mock<INeed>();
        NeedTwoMock = new Mock<INeed>();
        IHomeStructureMock = new Mock<IHomeStructure>();
        NeedGroup = new NeedGroup(ID);
        NeedGroup.Needs.Add(NeedOneMock.Object);
    }

    [Test]
    public void CalculateFulfillment() {
        NeedOneMock.Setup(n => n.GetCombinedFulfillment()).Returns(() => 1);
        AssertThat(NeedOneMock).HasInvoked(n => n.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel));

        NeedGroup.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel);

        AssertThat(NeedGroup.LastFulfillmentPercentage).IsEqualTo(1);
    }

    [Test]
    public void CalculateFulfillment_Multiple() {
        NeedGroup.Needs.Add(NeedTwoMock.Object);
        NeedTwoMock.Setup(n => n.GetCombinedFulfillment()).Returns(() => 0.5f);
        NeedOneMock.Setup(n => n.GetCombinedFulfillment()).Returns(() => 1);
        AssertThat(NeedOneMock).HasInvoked(n => n.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel));

        NeedGroup.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel);

        AssertThat(NeedGroup.LastFulfillmentPercentage).IsEqualTo(0.75f);
    }

    [Test]
    public void CalculateFulfillment_Multiple_OneStructureNeed() {
        NeedGroup.Needs.Add(NeedTwoMock.Object);
        NeedTwoMock.Setup(n => n.GetCombinedFulfillment()).Returns(() => 0.5f);
        NeedTwoMock.Setup(n => n.IsStructureNeed()).Returns(true);
        NeedOneMock.Setup(n => n.GetCombinedFulfillment()).Returns(() => 1);
        AssertThat(NeedOneMock).HasInvoked(n => n.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel));

        NeedGroup.CalculateFulfillment(MockUtil.City, MockUtil.PopulationLevel);

        AssertThat(NeedGroup.LastFulfillmentPercentage).IsEqualTo(1f);
    }

    [Test]
    public void GetFulfillmentForHome() {
        NeedGroup.Needs.Add(NeedTwoMock.Object);
        NeedTwoMock.Setup(n => n.IsStructureNeed()).Returns(true);
        NeedOneMock.Setup(n => n.GetFulfillment(0)).Returns(() => 1);
        IHomeStructureMock.Setup(h => h.IsStructureNeedFulfilled(NeedTwoMock.Object)).Returns(true);

        var tuple = NeedGroup.GetFulfillmentForHome(IHomeStructureMock.Object);

        AssertThat(tuple.Item1).IsEqualTo(1f);
        AssertThat(tuple.Item2).IsEqualTo(false);
    }
    [Test]
    public void GetFulfillmentForHome_StructureNeedNotFulfilled() {
        NeedGroup.Needs.Add(NeedTwoMock.Object);
        NeedTwoMock.Setup(n => n.IsStructureNeed()).Returns(false);
        NeedOneMock.Setup(n => n.GetFulfillment(0)).Returns(() => 1);
        IHomeStructureMock.Setup(h => h.IsStructureNeedFulfilled(NeedTwoMock.Object)).Returns(false);

        var tuple = NeedGroup.GetFulfillmentForHome(IHomeStructureMock.Object);

        AssertThat(tuple.Item1).IsEqualTo(0.5f);
        AssertThat(tuple.Item2).IsEqualTo(true);
    }
    [Test]
    public void UpdateNeeds() {
        Mock<IPlayer> player = new Mock<IPlayer>();
        player.Setup(p => p.HasNeedUnlocked(NeedOneMock.Object)).Returns(true);
        NeedOneMock.Setup(n => n.Exists()).Returns(true);
        NeedGroup.UpdateNeeds(player.Object);

        AssertThat(NeedGroup.Needs).HasSize(1);
    }
    [Test]
    public void UpdateNeeds_NotUnlocked_DoesNotExist() {
        NeedGroup.Needs.Add(NeedTwoMock.Object);
        Mock<IPlayer> player = new Mock<IPlayer>();
        player.Setup(p => p.HasNeedUnlocked(NeedOneMock.Object)).Returns(false);

        NeedGroup.UpdateNeeds(player.Object);

        AssertThat(NeedGroup.Needs).HasSize(0);
    }
}
