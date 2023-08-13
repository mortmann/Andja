using Andja.Model;
using Andja.Model.Data;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;
using System.Linq;
public class SupplyChainTest {
    SupplyChain SupplyChain;
    Mock<IProduce> mockProducer1 = new Mock<IProduce>();
    Mock<IPlayer> Player = new Mock<IPlayer>();
    Fertility fertility = new Fertility("TestFertility", null);

    [SetUp]
    public void SetUp() {
        SupplyChain = new SupplyChain(new Produce() {
            ProducerStructure = new OutputPrototypData() { ID = "SupplyChainStart", buildingItems = new Item[0] }
        });
    }

    [Test]
    public void AddProduce() {
        SupplyChain.AddProduce(mockProducer1.Object, 1, 1);
        Mock<IProduce> mockProducer2 = new Mock<IProduce>();
        SupplyChain.AddProduce(mockProducer2.Object, 2, 2);
        Mock<IProduce> mockProducer3 = new Mock<IProduce>();
        SupplyChain.AddProduce(mockProducer3.Object, 3, 3);

        AssertThat(SupplyChain.tiers[1]).AllSatisfy(x => x.Producer == mockProducer1.Object && x.Ratio == 1);
        AssertThat(SupplyChain.tiers[2]).AllSatisfy(x => x.Producer == mockProducer2.Object && x.Ratio == 2);
        AssertThat(SupplyChain.tiers[3]).AllSatisfy(x => x.Producer == mockProducer3.Object && x.Ratio == 3);
    }

    [Test]
    public void IsUnlocked_AllStructureUnlocked() {
        SupplyChain.tiers = new Dictionary<int, List<ProduceRatio>> {
            { 1 , GetProduceList(1, "Test") },
            { 2 , GetProduceList(1, "Test2") },
            { 3 , GetProduceList(1, "Test3") }
        };
        Player.Setup(p => p.HasStructureUnlocked(It.IsAny<string>())).Returns(true);

        AssertThat(SupplyChain.IsUnlocked(Player.Object)).IsTrue();
    }
    [Test]
    public void IsUnlocked_OneStructureNotUnlocked() {
        SupplyChain.tiers = new Dictionary<int, List<ProduceRatio>> {
            { 1 , GetProduceList(1, "Test") },
            { 2 , GetProduceList(1, "Test2") },
            { 3 , GetProduceList(1, "Test3") }
        };
        Player.Setup(p => p.HasStructureUnlocked("Test")).Returns(true);
        Player.Setup(p => p.HasStructureUnlocked("Test2")).Returns(false);
        Player.Setup(p => p.HasStructureUnlocked("Test3")).Returns(true);

        AssertThat(SupplyChain.IsUnlocked(Player.Object)).IsFalse();
    }
    [Test]
    public void StructureToBuildForOneRatio() {
        SupplyChain.tiers = new Dictionary<int, List<ProduceRatio>> {
            { 1 , GetProduceList(1, "Test") },
            { 2 , GetProduceList(1.5f, "Test2", "Test3") },
            { 3 , GetProduceList(3, "Test4") }
        };
        Dictionary<string, int> expected = new Dictionary<string, int> {
            { "SupplyChainStart", 1 },
            { "Test", 1 },
            { "Test2", 2 },
            { "Test3", 2 },
            { "Test4", 3 }
        };
        AssertThat(SupplyChain.StructureToBuildForOneRatio()).ContainsExactlyInAnyOrder(expected);
    }
    [Test]
    public void CalculateCost() {
        FarmPrototypeData farmData = new FarmPrototypeData() {
            ID = "Farm",
            buildingItems = new Item[] { ItemProvider.Brick_25, ItemProvider.Fish_25 },
            buildCost = 300,
            growable = new GrowableStructure("TestGrow", new GrowablePrototypeData() { fertility = fertility })
        };
        SupplyChain.ProduceRatio = new Dictionary<IProduce, float>() {
            { new Produce() { ProducerStructure = farmData}, 1 },
            { new Produce() { ProducerStructure = GetOutputPrototypData("T1")}, 2 },
            { new Produce() { ProducerStructure = GetOutputPrototypData("T2")}, 3 },
        };
        new MockUtil();
        SupplyChain.CalculateCost();
        SupplyChainCost cost = SupplyChain.cost;
        AssertThat(cost.requiredFertilites).ContainsExactly(fertility);
        AssertThat(cost.TotalItemCost).AllSatisfy((item) => new Item[] { 
                ItemProvider.Stone_N(25 * 5),
                ItemProvider.Wood_N(5 * 5),
                ItemProvider.Brick_25,
                ItemProvider.Fish_25
            }.Any(inItem => Item.AreSame(inItem, item))
        );
        AssertThat(cost.TotalBuildCost).IsEqualTo(5 * 500 + 300);
    }
    private OutputPrototypData GetOutputPrototypData(string id) {
        return new OutputPrototypData() {
            ID = id,
            buildingItems = new Item[] { ItemProvider.Stone_25, ItemProvider.Wood_5 },
            buildCost = 500,
        };
    }
    private static List<ProduceRatio> GetProduceList(float ratio, params string[] ids) {
        return ids.Select((id) => {
            return new ProduceRatio() {
                Ratio = ratio,
                Producer = new Produce() {
                    ProducerStructure = new OutputPrototypData() { ID = id }
                }
            };
        }).ToList();
    }

}
