using Andja.Model.Data;
using Moq;
using NUnit.Framework;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class SupplyChainTest {
    SupplyChain SupplyChain;
    Mock<IProduce> mockProducer1 = new Mock<IProduce>();

    [SetUp]
    public void SetUp() {
        SupplyChain = new SupplyChain();
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
    
}
