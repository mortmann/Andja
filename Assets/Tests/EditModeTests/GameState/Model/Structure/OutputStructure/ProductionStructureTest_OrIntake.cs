using System.Collections.Generic;
using Andja.Model;
using NUnit.Framework;
using static AssertNet.Assertions;

public class ProductionStructureTest_OrIntake {
    string ID = "Production";
    private MockUtil mockutil;
    TestProductionStructure Production;
    ProductionPrototypeData PrototypeData;

    string ProducerID = "Production";
    TestProductionStructure Producer;
    ProductionPrototypeData ProducerPrototypeData;

    [SetUp]
    public void SetUp() {
        PrototypeData = new ProductionPrototypeData() {
            ID = ID,
            produceTime = 2f,
            maxOutputStorage = 2,
            intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 },
            output = new Item[] { ItemProvider.Stone_1 },
            inputTyp = InputTyp.OR
        };
        Producer = new TestProductionStructure(ProducerID, null);
        ProducerPrototypeData = new ProductionPrototypeData() {
            output = new Item[] { ItemProvider.Wood_1 },
            maxOutputStorage=2,
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
    }
    private void CreateTwoByThree() {
        Production = new TestProductionStructure(ID, PrototypeData);
        Production.City = mockutil.City;
        Production.Tiles = Production.GetBuildingTiles(World.Current.GetTileAt(Production.StructureRange + 1, Production.StructureRange + 1));
        PrototypeData.structureRange = 10;
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void HasRequiredIntake() {
        CreateTwoByThree();
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1 };
        Assert.IsTrue(Production.HasRequiredInput());
    }

    [Test]
    public void HasRequiredIntake_False() {
        CreateTwoByThree();
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish_1 };
        Assert.IsFalse(Production.HasRequiredInput());
    }

    [Test]
    public void ChangeInput() {
        CreateTwoByThree();
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1 };
        Assert.IsTrue(Production.HasRequiredInput());
        Production.SetProgress(0.42f);
        Production.ChangeInput(PrototypeData.intake[1]);
        Assert.IsFalse(Production.HasRequiredInput());
        Assert.AreEqual(PrototypeData.intake[1].ID, Production.Intake[0].ID);
        Assert.AreEqual(Production.Progress, 0);
    }

    [Test]
    public void OnUpdate_CanProduce_HasEnoughForOne_First() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood_1 };
        for (int i = 0; i < 4; i++) {
            Production.Update(1f);
        }
        Assert.AreEqual(1, Production.Output[0].count);
    }
    [Test]
    public void OnUpdate_CanProduce_HasEnoughForOne_Second() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.ChangeInput(PrototypeData.intake[1]);
        Production.Intake = new Item[] { ItemProvider.Fish_2 };
        for (int i = 0; i < 4; i++) {
            Production.Update(1f);
        }
        Assert.AreEqual(1, Production.Output[0].count);
    }

    [Test]
    public void AddToIntake() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Fish };
        Production.AddToIntake(new UnitInventory() {
            Items = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 }
        });
        Assert.AreEqual(2, Production.Intake[0].count);
    }

    [Test]
    public void GetRequiredItems_Item() {
        CreateTwoByThree();
        PrototypeData.maxOutputStorage = 1;
        PrototypeData.structureRange = 10;
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
        Production.Intake = new Item[] { ItemProvider.Wood };

        AssertThat(Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Wood_1 })).AllItemsAreSame(ItemProvider.Wood_1);
    }

    [Test]
    public void GetRequiredItems_NoItem() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Fish };
        Assert.AreEqual(0, Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Wood }).Length);
    }

    [Test]
    public void OnOutputChangedStructure_OrItem() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Fish };
        Production.WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
        Producer.Output = new Item[] { ItemProvider.Wood_5 };
        Production.RegisteredStructures[Producer] = Producer.Output;

        Production.OnOutputChangedStructure(Producer);

        Assert.AreEqual(0, Production.WorkerJobsToDo.Count);
    }

    [Test]
    public void GetMaxIntakeForIndex() {
        CreateTwoByThree();
        Production.OnBuild();
        Assert.AreEqual(PrototypeData.intake[0].count * ProductionStructure.INTAKE_MULTIPLIER,
                Production.GetMaxIntakeForIndex(0));
    }

    [Test]
    public void Load() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Load();
        Assert.AreEqual(ItemProvider.Wood.ID, Production.Intake[0].ID);
    }

    [Test]
    public void Load_ItemsChanged() {
        CreateTwoByThree();
        Production.OnBuild();
        PrototypeData.intake = new Item[] { ItemProvider.Brick, ItemProvider.Fish };
        Production.Load();
        Assert.AreEqual(ItemProvider.Brick.ID, Production.Intake[0].ID);
    }

    class TestProductionStructure : ProductionStructure {
        public TestProductionStructure(string id, ProductionPrototypeData productionPrototypeData) : base(id, productionPrototypeData) {
        }

        public List<Worker> Workers {
            get => workers;
            set => workers = value;
        }

        public void SetProgress(float progress) {
            ProduceTimer = progress;
        }
    }
}
