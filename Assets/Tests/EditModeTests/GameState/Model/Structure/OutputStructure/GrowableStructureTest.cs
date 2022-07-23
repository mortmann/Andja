using System.Collections;
using System.Collections.Generic;
using Andja.Model;
using Moq;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using UnityEngine;
using UnityEngine.TestTools;
using Andja.Controller;

public class GrowableStructureTest {
    private const string ID = "growable";
    private GrowablePrototypeData growablePrototypeData;
    GrowableStructure growable;
    private Mock<IPrototypController> prototypeControllerMock;

    private Mock<ICity> CityMock;

    [SetUp]
    public void SetUp() {
        prototypeControllerMock = new Mock<IPrototypController>();

        growablePrototypeData = new GrowablePrototypeData {
            ageStages = 2,
            produceTime = 1,
            output = new[] {ItemProvider.Wood_1},
        };
        growable = new GrowableStructure(ID, null);
        PrototypController.Instance = prototypeControllerMock.Object;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => growablePrototypeData);
        CityMock = new Mock<ICity>();
    }
    [Test]
    public void OnBuild_Allgood() {
        BuildCityHasFertility();
        AreEqual(1, growable.LandGrowModifier);
    }
    [Test]
    public void OnBuild_NoFertility() {
        growablePrototypeData.fertility = new Fertility("Fake", null);
        AreEqual(0, growable.LandGrowModifier);
    }
    [Test]
    public void OnUpdate_Produce() {
        BuildCityHasFertility();
        IsFalse(growable.hasProduced);
        UpdateGrowable();
        IsTrue(growable.hasProduced);
        AreEqual(1, growable.Output[0].count);
    }
    [Test]
    public void OnUpdate_NotProduced() {
        IsFalse(growable.hasProduced);
        UpdateGrowable();
        IsFalse(growable.hasProduced);
        AreEqual(0, growable.Output[0].count);
    }
    [Test]
    public void Harvest() {
        BuildCityHasFertility();
        UpdateGrowable();
        IsTrue(growable.hasProduced);
        growable.Harvest();
        AreEqual(0, growable.Output[0].count);
        IsFalse(growable.hasProduced);
    }
    private void BuildCityHasFertility() {
        CityMock.Setup(c => c.HasFertility(It.IsAny<Fertility>())).Returns(true);
        growable.OnBuild();
    }
    private void UpdateGrowable() {
        for (int i = 0; i < growable.ProduceTime + 1; i++) {
            growable.OnUpdate(1);
        }
    }
}
