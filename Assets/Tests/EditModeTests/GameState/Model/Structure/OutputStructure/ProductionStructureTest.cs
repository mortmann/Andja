using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using Andja.Utility;

public class ProductionStructureTest {
    string ID = "Production";
    private MockUtil mockutil;
    ProductionStructure Production;
    ProductionPrototypeData PrototypeData;

    string ProducerID = "Production";
    ProductionStructure Producer;
    ProductionPrototypeData ProducerPrototypeData;

    [SetUp]
    public void SetUp() {
        PrototypeData = new ProductionPrototypeData() {
            ID = ID,
            produceTime = 2f,
            maxOutputStorage = 2,
            intake = new Item[] {ItemProvider.Wood_1, ItemProvider.Fish_2},
            output = new Item[] {ItemProvider.Stone_1}
        };
        Producer = new ProductionStructure(ProducerID, null);
        ProducerPrototypeData = new ProductionPrototypeData() {
            output = new Item[] { ItemProvider.Wood_1 },
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        CreateTwoByThree();
    }
    private void CreateTwoByThree() {
        Production = new ProductionStructure(ID, PrototypeData);
        Production.City = mockutil.City;
        //Production.City = mockutil.City;
        Production.Tiles = Production.GetBuildingTiles(World.Current.GetTileAt(Production.StructureRange, Production.StructureRange));
        PrototypeData.structureRange = 10;
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void HasRequiredIntake() {
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 };
        Assert.IsTrue(Production.HasRequiredInput());
    }

    [Test]
    public void HasRequiredIntake_False() {
        Production.OnBuild();
        Assert.IsFalse(Production.HasRequiredInput());
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish };
        Assert.IsFalse(Production.HasRequiredInput());
    }

    [Test]
    public void OnUpdate_CanProduce_HasEnoughForOne() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 };
        for (int i = 0; i < 4; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(1, Production.Output[0].count);
    }

    [Test]
    public void OnUpdate_CanProduce_StopsAtMaxOut() {
        CreateTwoByThree();
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood_50, ItemProvider.Fish_25 };
        for (int i = 0; i < 12; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(2, Production.Output[0].count);
    }
    
    [Test]
    public void OnUpdate_CannotProduce() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        for (int i = 0; i < 4; i++) {
            Production.OnUpdate(1f);
        }
        Assert.AreEqual(0, Production.Output[0].count);
    }

    [Test]
    public void AddToIntake() {
        Production.OnBuild();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Production.AddToIntake(new UnitInventory() {
            Items = new Item[] { ItemProvider.Wood_1, ItemProvider.Fish_2 }
        });
        Assert.AreEqual(1, Production.Intake[0].count);
        Assert.AreEqual(2, Production.Intake[1].count);
    }

    [Test]
    public void GetRequiredItems_Item() {
        PrototypeData.structureRange = 10;
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.RangeTiles = new HashSet<Tile>();
        Production.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Assert.AreEqual(ItemProvider.Wood.ID, Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Wood })[0].ID);
    }

    [Test]
    public void GetRequiredItems_NoItem() {
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Assert.AreEqual(0, Production.GetRequiredItems(Producer, new Item[] { ItemProvider.Brick }).Length);
    }

    [Test]
    public void OnOutputChangedStructure() {
        Production.OnBuild();
        Production.Workers = new List<Worker>();
        Production.Intake = new Item[] { ItemProvider.Wood, ItemProvider.Fish };
        Production.WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
        Producer.Output = new Item[] { ItemProvider.Wood_5 };
        Production.RegisteredStructures[Producer] = Producer.Output;

        Production.OnOutputChangedStructure(Producer);

        Assert.AreEqual(Producer, Production.WorkerJobsToDo.First().Key);
        Assert.AreEqual(1, Production.WorkerJobsToDo.First().Value.Length);
        Assert.AreEqual(ItemProvider.Wood.ID, Production.WorkerJobsToDo.First().Value[0].ID);
        Assert.AreEqual(PrototypeData.intake[0].count * ProductionStructure.INTAKE_MULTIPLIER,
                        Production.WorkerJobsToDo.First().Value[0].count);
    }

    [Test]
    public void GetMaxIntakeForIndex() {
        Production.OnBuild();
        Assert.AreEqual(PrototypeData.intake[0].count * ProductionStructure.INTAKE_MULTIPLIER,
                Production.GetMaxIntakeForIndex(0));
        Assert.AreEqual(PrototypeData.intake[1].count * ProductionStructure.INTAKE_MULTIPLIER,
                Production.GetMaxIntakeForIndex(1));
    }

    [Test]
    public void Load() {
        Production.OnBuild();
        Production.Load();
        Assert.AreEqual(ItemProvider.Wood.ID, Production.Intake[0].ID);
        Assert.AreEqual(ItemProvider.Fish.ID, Production.Intake[1].ID);
    }

    [Test]
    public void Load_ItemsChanged() {
        Production.OnBuild();
        PrototypeData.intake = new Item[] { ItemProvider.Brick };
        Production.Load();
        Assert.AreEqual(ItemProvider.Brick.ID, Production.Intake[0].ID);
        Assert.AreEqual(1, Production.Intake.Length);
    }
}
