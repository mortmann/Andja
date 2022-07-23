using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static Andja.Model.HomeStructure;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class HomeStructureTest {
    private const string ID = "home";
    private const string NextLevelID = "nextlevel";
    TestHomeStructure Home;
    HomePrototypeData PrototypeData;
    private MockUtil mockutil;
    HomePrototypeData nextLevelHomePrototypeData;
    [SetUp]
    public void SetUp() {
        Home = new TestHomeStructure(ID, null) {
        };
        PrototypeData = new HomePrototypeData() {
            maxLivingSpaces = 2,
            canBeUpgradedTo = new [] { NextLevelID },
            increaseTime = 2f,
            decreaseTime = 2f
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        Home.City = mockutil.City;

        nextLevelHomePrototypeData = new HomePrototypeData() {
            ID = NextLevelID,
            buildCost = 500,
            buildingItems = new[] { ItemProvider.Wood_10, ItemProvider.Stone_1 },
            maxLivingSpaces = 5,
            prevLevel = Home
        };
        PrototypeData.nextLevel = new HomeStructure(NextLevelID, nextLevelHomePrototypeData);
        mockutil.PrototypControllerMock.Setup(m => m.GetStructurePrototypDataForID(NextLevelID)).Returns(nextLevelHomePrototypeData);

        CreateTwoByTwo();
    }

    private void CreateTwoByTwo() {
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 2;
        Home.Tiles = Home.GetBuildingTiles(World.Current.GetTileAt(Home.StructureRange, Home.StructureRange));

        Home.RegisterOnExtraUICallback(mockutil.Callbacks.Object.StructureBoolean);
    }

    [Test]
    public void TryToIncreasePeople() {
        Home.SetCurrentMood(CitizenMoods.Happy);
        AssertThat(Home.People).IsEqualTo(1);
        Home.TestTryToIncreasePeople();
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(mockutil.CityMock).HasInvoked(c => c.AddPeople(Home.PopulationLevel, 1)).Once();
    }

    [Test]
    public void TryToIncreasePeople_Upgrade() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(true);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);
        Home.SetCurrentMood(CitizenMoods.Happy);

        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        Home.TestTryToIncreasePeople();
        AssertThat(Home.People).IsEqualTo(3);
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(PrototypeData.populationLevel, PrototypeData.maxLivingSpaces)).Once();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.AddPeople(Home.PopulationLevel, PrototypeData.maxLivingSpaces)).Once();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.AddPeople(Home.PopulationLevel, 1)).Once();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemoveItems(nextLevelHomePrototypeData.buildingItems)).Once();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.ReduceTreasureFromOwner(nextLevelHomePrototypeData.buildCost)).Once();
        AssertThat(Home.HomeData).IsEqualTo(nextLevelHomePrototypeData);
        AssertThat(mockutil.Callbacks).HasInvoked(cb => cb.StructureBoolean(Home, true)).Once();
    }

    [Test]
    public void CanUpgrade_NotEnoughItems() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(false);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(true);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);

        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void CanUpgrade_NotEnoughMoney() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(false);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(true);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);

        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void CanUpgrade_NotOwnerUnlockedAllNeeds() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(false);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(true);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);

        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void CanUpgrade_MaxStructureLevel() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(false);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(false);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(0);
        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void CanUpgrade_NotHappy() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(false);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(false);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);
        Home.SetCurrentMood(CitizenMoods.Neutral);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void CanUpgrade_NotFull() {
        mockutil.CityMock.Setup(c => c.HasEnoughOfItems(It.IsAny<Item[]>(), 1)).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerEnoughMoney(It.IsAny<int>())).Returns(true);
        mockutil.CityMock.Setup(c => c.HasOwnerUnlockedAllNeeds(It.IsAny<int>())).Returns(false);
        mockutil.CityMock.SetupGet(c => c.AutoUpgradeHomes).Returns(false);
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);
        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.SetPeople(1);
        AssertThat(Home.CanBeUpgraded).IsFalse();
    }
    [Test]
    public void TryToDecreasePeople() {
        Home.SetCurrentMood(CitizenMoods.Mad);
        Home.SetPeople(2);
        AssertThat(Home.People).IsEqualTo(2);
        Home.TestTryToDecreasePeople();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(Home.PopulationLevel, 1)).Once();
    }
    [Test]
    public void TryToDecreasePeople_IsAbandoned() {
        Home.SetCurrentMood(CitizenMoods.Mad);
        AssertThat(Home.People).IsEqualTo(1);
        Home.TestTryToDecreasePeople();
        AssertThat(Home.People).IsEqualTo(0);
        Home.TestTryToDecreasePeople();
        AssertThat(Home.IsAbandoned).IsTrue();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(Home.PopulationLevel, 1)).Once();
    }

    [Test]
    public void OnDestroy() {
        Home.OnDestroy();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(Home.PopulationLevel, 1)).Once();
    }
    [Test]
    public void DowngradeHouse() {
        Home = new TestHomeStructure(NextLevelID, nextLevelHomePrototypeData);
        Home.SetPeople(2);
        Home.City = mockutil.City;
        Home.DowngradeHouse();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(nextLevelHomePrototypeData.populationLevel, 2)).Once();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.AddPeople(PrototypeData.populationLevel, 2)).Once();
        AssertThat(Home.ID).IsEqualTo(ID);
    }
    [Test]
    public void IsMaxLevel() {
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(0);
        AssertThat(Home.IsMaxLevel()).IsTrue();
    }
    [Test]
    public void IsMaxLevel_False() {
        mockutil.PrototypControllerMock.Setup(p => p.GetMaxStructureLevelForStructureType(It.IsAny<Type>())).Returns(1);
        AssertThat(Home.IsMaxLevel()).IsFalse();
    }
    [Test]
    public void OnCityChange() {
        Home.OnCityChange(Home, mockutil.City, mockutil.OtherCity);
        AssertThat(mockutil.CityMock).HasInvoked(c => c.RemovePeople(PrototypeData.populationLevel, 1)).Once();
        AssertThat(mockutil.OtherCityMock).HasInvoked(c => c.AddPeople(PrototypeData.populationLevel, 1)).Once();
    }
    [Test]
    public void IsStructureNeedFulfilled() {
        NeedStructure needStructure = new NeedStructure("bla", new NeedStructurePrototypeData());
        needStructure.City = mockutil.City;
        Home.AddNeedStructure(needStructure);
        AssertThat(Home.IsStructureNeedFulfilled(new Need("bla2", new NeedPrototypeData() {
            structures = new[] { needStructure }
        }))).IsTrue();
    }
    [Test]
    public void IsStructureNeedFulfilled_False_NoStructure() {
        NeedStructure needStructure = new NeedStructure("bla", new NeedStructurePrototypeData());
        needStructure.City = mockutil.City;
        Home.AddNeedStructure(new NeedStructure());
        AssertThat(Home.IsStructureNeedFulfilled(new Need("bla2", new NeedPrototypeData() {
            structures = new[] { needStructure }
        }))).IsFalse();
    }
    [Test]
    public void IsStructureNeedFulfilled_False_OtherCity() {
        NeedStructure needStructure = new NeedStructure("bla", new NeedStructurePrototypeData());
        needStructure.City = mockutil.OtherCity;
        Home.AddNeedStructure(needStructure);
        AssertThat(Home.IsStructureNeedFulfilled(new Need("bla2", new NeedPrototypeData() {
            structures = new[] { needStructure }
        }))).IsFalse();
    }
    [Test]
    public void UpdatePeopleChange_Happy() {
        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.PeopleDecreaseTimer = 1f;
        Home.PeopleIncreaseTimer = 0.5f;
        Home.TestUpdatePeopleChange(0.5f);
        AssertThat(Home.PeopleDecreaseTimer).IsEqualTo(0.5f, 0.001);
        AssertThat(Home.PeopleIncreaseTimer).IsEqualTo(1f, 0.001);
    }
    [Test]
    public void UpdatePeopleChange_Happy_Max() {
        Home.SetCurrentMood(CitizenMoods.Happy);
        Home.PeopleDecreaseTimer = 1f;
        Home.PeopleIncreaseTimer = 1.6f;
        Home.TestUpdatePeopleChange(0.5f);
        AssertThat(Home.PeopleDecreaseTimer).IsEqualTo(0.5f, 0.001);
        AssertThat(Home.PeopleIncreaseTimer).IsEqualTo(0, 0.001);
        AssertThat(Home.People).IsEqualTo(2);
    }
    [Test]
    public void UpdatePeopleChange_Mad() {
        Home.SetCurrentMood(CitizenMoods.Mad);
        Home.PeopleDecreaseTimer = 0.5f;
        Home.PeopleIncreaseTimer = 1f;
        Home.TestUpdatePeopleChange(0.5f);
        AssertThat(Home.PeopleDecreaseTimer).IsEqualTo(1f, 0.001);
        AssertThat(Home.PeopleIncreaseTimer).IsEqualTo(0.5f, 0.001);
    }
    [Test]
    public void UpdatePeopleChange_Mad_Max() {
        Home.SetCurrentMood(CitizenMoods.Mad);
        Home.PeopleDecreaseTimer = 1.6f;
        Home.PeopleIncreaseTimer = 1f;
        Home.TestUpdatePeopleChange(0.5f);
        AssertThat(Home.PeopleDecreaseTimer).IsEqualTo(0, 0.001);
        AssertThat(Home.PeopleIncreaseTimer).IsEqualTo(0.5f, 0.001);
        AssertThat(Home.People).IsEqualTo(0);
    }
    [Test]
    public void UpdatePeopleChange_Neutral() {
        Home.SetCurrentMood(CitizenMoods.Neutral);
        Home.PeopleDecreaseTimer = 0.5f;
        Home.PeopleIncreaseTimer = 1f;
        Home.TestUpdatePeopleChange(0.5f);
        AssertThat(Home.PeopleDecreaseTimer).IsEqualTo(0, 0.001);
        AssertThat(Home.PeopleIncreaseTimer).IsEqualTo(0.5f, 0.001);
    }
    [Test]
    public void IsInWilderness_IsMad() {
        mockutil.CityMock.Setup(c => c.IsWilderness()).Returns(true);
        Home.Update(0.1f);
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Mad);
    }

    [Test]
    public void CalculateMood_Happy() {
        Mock<INeedGroup> needgroup1 = new Mock<INeedGroup>();
        needgroup1.Setup(n => n.GetFulfillmentForHome(It.IsAny<HomeStructure>())).Returns(new Tuple<float, bool>(1, false));
        needgroup1.Setup(n => n.IsUnlocked()).Returns(true);
        needgroup1.SetupGet(n => n.ImportanceLevel).Returns(1);
        mockutil.CityMock.Setup(c => c.GetTaxPercentage(It.IsAny<int>())).Returns(1);
        mockutil.CityMock.Setup(c => c.GetPopulationNeedGroups(It.IsAny<int>())).Returns(new List<INeedGroup> { needgroup1.Object });
        Home.CalculateMood();
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Happy);
    }
    [Test]
    public void CalculateMood_Neutral_Missing() {
        Mock<INeedGroup> needgroup1 = new Mock<INeedGroup>();
        needgroup1.Setup(n => n.IsUnlocked()).Returns(true);
        needgroup1.SetupGet(n => n.ImportanceLevel).Returns(1);
        needgroup1.Setup(n => n.GetFulfillmentForHome(It.IsAny<HomeStructure>())).Returns(new Tuple<float, bool>(1f, true));
        mockutil.CityMock.Setup(c => c.GetTaxPercentage(It.IsAny<int>())).Returns(1);
        mockutil.CityMock.Setup(c => c.GetPopulationNeedGroups(It.IsAny<int>())).Returns(new List<INeedGroup> { needgroup1.Object });
        Home.CalculateMood();
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Neutral);
        AssertThat(mockutil.Callbacks).HasInvoked(cb => cb.StructureBoolean(Home, false)).Once();
    }
    [Test]
    public void CalculateMood_Mad_0() {
        Mock<INeedGroup> needgroup1 = new Mock<INeedGroup>();
        needgroup1.Setup(n => n.IsUnlocked()).Returns(true);
        needgroup1.SetupGet(n => n.ImportanceLevel).Returns(1);
        needgroup1.Setup(n => n.GetFulfillmentForHome(It.IsAny<HomeStructure>())).Returns(new Tuple<float, bool>(0.5f, false));
        mockutil.CityMock.Setup(c => c.GetTaxPercentage(It.IsAny<int>())).Returns(1);
        mockutil.CityMock.Setup(c => c.GetPopulationNeedGroups(It.IsAny<int>())).Returns(new List<INeedGroup> { needgroup1.Object });
        Home.CalculateMood();
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Mad);
        AssertThat(mockutil.Callbacks).HasInvoked(cb => cb.StructureBoolean(Home, false)).Once();
    }
    [Test]
    public void CalculateMood_Neutral() {
        Mock<INeedGroup> needgroup1 = new Mock<INeedGroup>();
        needgroup1.Setup(n => n.IsUnlocked()).Returns(true);
        needgroup1.SetupGet(n => n.ImportanceLevel).Returns(1);
        needgroup1.Setup(n => n.GetFulfillmentForHome(It.IsAny<HomeStructure>())).Returns(new Tuple<float, bool>(0.51f, false));
        mockutil.CityMock.Setup(c => c.GetTaxPercentage(It.IsAny<int>())).Returns(1);
        mockutil.CityMock.Setup(c => c.GetPopulationNeedGroups(It.IsAny<int>())).Returns(new List<INeedGroup> { needgroup1.Object });
        Home.CalculateMood();
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Neutral);
        AssertThat(mockutil.Callbacks).HasInvoked(cb => cb.StructureBoolean(Home, false)).Once();
    }
    [Test]
    public void CalculateMood_NegativeEffect_Mad() {
        Mock<INeedGroup> needgroup1 = new Mock<INeedGroup>();
        needgroup1.Setup(n => n.IsUnlocked()).Returns(true);
        needgroup1.SetupGet(n => n.ImportanceLevel).Returns(1);
        needgroup1.Setup(n => n.GetFulfillmentForHome(It.IsAny<HomeStructure>())).Returns(new Tuple<float, bool>(1f, false));
        mockutil.CityMock.Setup(c => c.GetTaxPercentage(It.IsAny<int>())).Returns(1);
        mockutil.CityMock.Setup(c => c.GetPopulationNeedGroups(It.IsAny<int>())).Returns(new List<INeedGroup> { needgroup1.Object });
        Home.SetNegativeEffect();
        Home.CalculateMood();
        AssertThat(Home.CurrentMood).IsEqualTo(CitizenMoods.Mad);
        AssertThat(mockutil.Callbacks).HasInvoked(cb => cb.StructureBoolean(Home, false)).Once();
    }
    private class TestHomeStructure : HomeStructure {
        public TestHomeStructure(string pid, HomePrototypeData proto) : base(pid, proto) {
        }
        public float PeopleDecreaseTimer {
            get => peopleDecreaseTimer;
            set => peopleDecreaseTimer = value;
        }

        public float PeopleIncreaseTimer {
            get => peopleIncreaseTimer;
            set => peopleIncreaseTimer = value;
        }

        public void SetPeople(int people) {
            People = people;
        }
        public void SetCurrentMood(CitizenMoods mood) {
            CurrentMood = mood;
        }
        public void AddNeedStructure(NeedStructure need) {
            NeedStructures ??= new List<NeedStructure>();
            NeedStructures.Add(need);
        }
        public void TestTryToIncreasePeople() {
            TryToIncreasePeople();
        }
        public void TestTryToDecreasePeople() {
            TryToDecreasePeople();
        }
        public void TestUpdatePeopleChange(float delta) {
            UpdatePeopleChange(delta);
        }

        internal void SetNegativeEffect() {
            HasNegativeEffect = true;
        }
    }
}
