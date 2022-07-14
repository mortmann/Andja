using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class HomeStructureTest {
    private const string ID = "farm";
    TestHomeStructure Home;
    HomePrototypeData PrototypeData;
    private MockUtil mockutil;

    [SetUp]
    public void SetUp() {
        Home = new TestHomeStructure(ID, PrototypeData) {
        };
        PrototypeData = new HomePrototypeData() {
            structureRange = 5,
            maxLivingSpaces = 2
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        Home.City = mockutil.City;
        CreateTwoByTwo();
    }

    private void CreateTwoByTwo() {
        PrototypeData.tileWidth = 4;
        PrototypeData.tileHeight = 4;
        Home.Tiles = Home.GetBuildingTiles(World.Current.GetTileAt(Home.StructureRange, Home.StructureRange));
    }

    [Test]
    public void TryToIncreasePeople() {
        Home.SetCurrentMood(HomeStructure.CitizienMoods.Happy);
        AssertThat(Home.People).IsEqualTo(1);
        Home.TestTryToIncreasePeople();
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(mockutil.CityMock).HasInvoked(c => c.AddPeople(Home.PopulationLevel, 1)).Once();
    }
    [Test]
    public void TryToDecreasePeople() {
        Home.SetCurrentMood(HomeStructure.CitizienMoods.Mad);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        Home.TestTryToDecreasePeople();
        AssertThat(Home.People).IsEqualTo(1);
        Home.TestTryToDecreasePeople();
        AssertThat(Home.IsAbandoned).IsTrue();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(Home.PopulationLevel, 1)).Exactly(2);
    }
    private class TestHomeStructure : HomeStructure {
        public TestHomeStructure(string pid, HomePrototypeData proto) : base(pid, proto) {
        }

        public void SetPeople(int people) {
            People = people;
        }
        public void SetCurrentMood(CitizienMoods mood) {
            CurrentMood = mood;
        }

        public void TestTryToIncreasePeople() {
            TryToIncreasePeople();
        }
        public void TestTryToDecreasePeople() {
            TryToDecreasePeople();
        }
    }
}
