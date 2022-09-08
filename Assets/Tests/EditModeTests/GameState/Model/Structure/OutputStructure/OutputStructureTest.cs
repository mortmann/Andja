using System.Collections;
using System.Collections.Generic;
using Andja.Controller;
using Andja.Model;
using Andja.Utility;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using System;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class OutputStructureTest {
    string ID = "OUTPUTSTRUCTURE";
    TestOutputStructure OutputTestStructure;
    OutputPrototypData PrototypeData;
    private MockUtil mockutil;

    [SetUp]
    public void SetUp() {
        OutputTestStructure = new TestOutputStructure(ID, null);
        PrototypeData = new OutputPrototypData() {
            ID = ID,
            produceTime = 2f,
            maxOutputStorage = 2,
        };
        mockutil = new MockUtil();
        PrototypeData.output = new Item[] { ItemProvider.Stone_1 };
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(() => PrototypeData);
        prototypeControllerMock.Setup(m => m.GetEffectPrototypDataForID(OutputStructure.InactiveEffectID)).Returns(() =>
            new EffectPrototypeData() {
                targets = new TargetGroup(Target.OutputStructure),
            });
        CreateTwoByThree();
    }
    private void CreateTwoByThree() {
        OutputTestStructure.City = mockutil.City;
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 3;
        
        OutputTestStructure.Tiles = OutputTestStructure.GetBuildingTiles(World.Current.GetTileAt(OutputTestStructure.StructureRange, OutputTestStructure.StructureRange));
        OutputTestStructure.City = mockutil.City;
    }

    [Test]
    public void GetRequiredItems_Item() {
        CreateTwoByThree();
        PrototypeData.structureRange = 10;
        OutputTestStructure.OnBuild();
        OutputTestStructure.Workers = new List<Worker>();
        OutputTestStructure.RangeTiles = new HashSet<Tile>();
        OutputTestStructure.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
        TestOutputStructure Producer = new TestOutputStructure() {
            Output = new Item[] { ItemProvider.Stone_1 },
        };
        Assert.AreEqual(ItemProvider.Stone.ID, OutputTestStructure.GetRequiredItems(Producer, new Item[] { ItemProvider.Stone })[0].ID);
    }
    [Test]
    public void GetRequiredItems_NoItem() {
        CreateTwoByThree();
        PrototypeData.structureRange = 10;
        OutputTestStructure.OnBuild();
        OutputTestStructure.Workers = new List<Worker>();
        OutputTestStructure.RangeTiles = new HashSet<Tile>();
        OutputTestStructure.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
        TestOutputStructure Producer = new TestOutputStructure() {
            Output = new Item[] { ItemProvider.Fish_1 },
        };
        Assert.AreEqual(0, OutputTestStructure.GetRequiredItems(Producer, new Item[] { ItemProvider.Wood }).Length);
    }

    [Test]
    public void WorkerComeBack() {
        OutputTestStructure.Workers = new List<Worker>();
        Worker worker = new Worker();
        OutputTestStructure.Workers.Add(worker);
        OutputTestStructure.WorkerComeBack(worker);
        Assert.AreEqual(0, OutputTestStructure.Workers.Count);
        Assert.IsFalse(worker.IsAlive);
    }

    [Test]
    public void AddToOutput() {
        OutputTestStructure.Output = PrototypeData.output.CloneArray();
        OutputTestStructure.AddToOutput(new UnitInventory() {
            Items = new Item[] { ItemProvider.Stone_1 },
        });
        Assert.AreEqual(1, OutputTestStructure.Output[0].count);
    }

    [Test]
    public void AddToOutput_NotAdded() {
        OutputTestStructure.Output = PrototypeData.output.CloneArray();
        OutputTestStructure.AddToOutput(new UnitInventory() {
            Items = new Item[] { ItemProvider.Fish_1 },
        });
        Assert.AreEqual(0, OutputTestStructure.Output[0].count);
    }

    [Test]
    public void GetOutput() {
        Item[] items = new Item[] { ItemProvider.Stone_N(5), ItemProvider.Wood_N(5) };
        OutputTestStructure.Output = items.CloneArrayWithCounts();
        Item[] output = OutputTestStructure.GetOutput();
        Assert.AreEqual(items.Length, output.Length);
        Assert.IsTrue(Item.AreSame(items[0], output[0]));
        Assert.IsTrue(Item.AreSame(items[1], output[1]));
        Assert.AreEqual(0, OutputTestStructure.Output[0].count);
        Assert.AreEqual(0, OutputTestStructure.Output[1].count);
    }

    [Test]
    public void GetOutput_WithItemAndMax() {
        Item[] items = new Item[] { ItemProvider.Stone_N(5), ItemProvider.Wood_N(5) };
        OutputTestStructure.Output = items.CloneArrayWithCounts();
        Item[] output = OutputTestStructure.GetOutput(items, new int[] {2,3});
        Assert.AreEqual(items.Length, output.Length);
        Assert.AreEqual(items[0].ID, output[0].ID);
        Assert.AreEqual(items[1].ID, output[1].ID);
        Assert.AreEqual(2, output[0].count);
        Assert.AreEqual(3, output[1].count);
        Assert.AreEqual(3, OutputTestStructure.Output[0].count);
        Assert.AreEqual(2, OutputTestStructure.Output[1].count);
    }

    [Test]
    public void GetOutputWithItemCountAsMax() {
        Item[] items = new Item[] { ItemProvider.Stone_N(5), ItemProvider.Wood_N(5) };
        OutputTestStructure.Output = items.CloneArrayWithCounts();
        Item[] output = OutputTestStructure.GetOutputWithItemCountAsMax(items);
        Assert.AreEqual(items.Length, output.Length);
        Assert.AreEqual(items[0].ID, output[0].ID);
        Assert.AreEqual(items[1].ID, output[1].ID);
        Assert.AreEqual(items[0].count, output[0].count);
        Assert.AreEqual(items[1].count, output[1].count);
        Assert.AreEqual(0, OutputTestStructure.Output[0].count);
        Assert.AreEqual(0, OutputTestStructure.Output[1].count);
    }

    [Test]
    public void GetOneOutput() {
        Item[] items = new Item[] { ItemProvider.Stone_N(5), ItemProvider.Wood_N(5) };
        OutputTestStructure.Output = items.CloneArrayWithCounts();
        Item output = OutputTestStructure.GetOneOutput(items[0]);
        Assert.AreEqual(items[0].ID, output.ID);
        Assert.AreEqual(items[0].count, output.count);
        Assert.AreEqual(0, OutputTestStructure.Output[0].count);
    }

    [Test]
    public void ResetOutputClaimed() {
        OutputTestStructure.Output = PrototypeData.output.CloneArrayWithCounts();
        OutputTestStructure.outputClaimed = true;
        OutputTestStructure.ResetOutputClaimed();
        Assert.IsFalse(OutputTestStructure.outputClaimed);
    }
    [Test]
    public void ToggleActive_Off() {
        Assert.IsTrue(OutputTestStructure.IsActive);
        OutputTestStructure.ToggleActive();
        Assert.IsFalse(OutputTestStructure.IsActive);
        Assert.IsTrue(OutputTestStructure.Effects.ToList().Exists(x=>x.ID == OutputStructure.InactiveEffectID));
    }
    [Test]
    public void ToggleActive_On() {
        OutputTestStructure.AddEffect(new Effect(OutputStructure.InactiveEffectID));
        OutputTestStructure.ToggleActive();
        OutputTestStructure.ToggleActive();
        Assert.IsTrue(OutputTestStructure.IsActive);
        Assert.IsFalse(OutputTestStructure.Effects.ToList().Exists(x => x.ID == OutputStructure.InactiveEffectID));
    }
    [Test]
    public void OnDestroy() {
        OutputTestStructure.Workers = new List<Worker> { new Worker() };
        OutputTestStructure.OnDestroy();
        Assert.IsFalse(OutputTestStructure.Workers[0].IsAlive);
    }

    [Test]
    public void Load() {
        OutputTestStructure.Workers = new List<Worker> { new Worker() };
        OutputTestStructure.Output = new Item[] { ItemProvider.Stone_1 };

        OutputTestStructure.Load();

        AssertThat(OutputTestStructure.Output.Length).IsEqualTo(1);
        AssertThat(Item.AreSame(OutputTestStructure.Output[0], ItemProvider.Stone_1)).IsTrue();
    }
    [Test]
    public void Load_OutputChanged_More() {
        OutputTestStructure.Workers = new List<Worker> { new Worker() };
        PrototypeData.output = new Item[] { ItemProvider.Stone_1, ItemProvider.Fish_1 };
        OutputTestStructure.Output = new Item[] { ItemProvider.Stone_1 };

        OutputTestStructure.Load();

        AssertThat(OutputTestStructure.Output.Length).IsEqualTo(2);
        AssertThat(OutputTestStructure.Output[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(OutputTestStructure.Output[1].ID).IsEqualTo(ItemProvider.Fish.ID);
    }
    [Test]
    public void Load_OutputChanged_Less() {
        OutputTestStructure.Workers = new List<Worker> { new Worker() };
        PrototypeData.output = new Item[] { ItemProvider.Fish_1 };
        OutputTestStructure.Output = new Item[] { ItemProvider.Stone_1, ItemProvider.Fish_1 };

        OutputTestStructure.Load();

        AssertThat(OutputTestStructure.Output.Length).IsEqualTo(1);
        AssertThat(OutputTestStructure.Output[0].ID).IsEqualTo(ItemProvider.Fish.ID);
    }
    [Test]
    public void SendOutWorkerIfCan() {
        CreateTwoByThree();
        OutputTestStructure.TestAddJobStructure(new TestOutputStructure(ID, new OutputPrototypData()) {
                Output = new Item[] { ItemProvider.Stone_1 },
                Tiles = new List<Tile>{mockutil.GetInCityTile(10,10)},
        },new Item[] { ItemProvider.Stone_1 });
        PrototypeData.workerID = "worker";
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker"))
            .Returns(new WorkerPrototypeData());

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems.Length).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].count).IsEqualTo(PrototypeData.maxOutputStorage);
    }
    [Test]
    public void SendOutWorkerIfCan_ButAlreadyHasWorkerOut_ReduceGetItems() {
        CreateTwoByThree();
        OutputTestStructure.TestAddJobStructure(new TestOutputStructure(ID, new OutputPrototypData()) {
            Output = new Item[] { ItemProvider.Stone_1 },
            Tiles = new List<Tile> { mockutil.GetInCityTile(10, 10) },
        }, new Item[] { ItemProvider.Stone_1 });
        PrototypeData.workerID = "worker";
        PrototypeData.maxOutputStorage = 5;
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker"))
            .Returns(new WorkerPrototypeData());
        OutputTestStructure.Workers = new List<Worker> { new Worker(){ToGetItems = new []{ ItemProvider.Stone_N(2) } } };
        OutputTestStructure.Output = new[] { ItemProvider.Stone_1 };

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems.Length).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].count).IsEqualTo(2);
    }
    [Test]
    public void SendOutWorkerIfCan_LimitedNeeded() {
        CreateTwoByThree();
        OutputTestStructure.TestAddJobStructure(new TestOutputStructure(ID, new OutputPrototypData()) {
            Output = new Item[] { ItemProvider.Stone_1 },
            Tiles = new List<Tile> { mockutil.GetInCityTile(10, 10) }
        }, new Item[] { ItemProvider.Stone_N(3) });
        PrototypeData.workerID = "worker";
        OutputTestStructure.Output = new[] { ItemProvider.Stone_1 };
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker"))
            .Returns(new WorkerPrototypeData());

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems.Length).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].count).IsEqualTo(1);
    }
    [Test]
    public void SendOutWorkerIfCan_HasToFollowRoads_NoConnection() {
        CreateTwoByThree();
        OutputTestStructure.TestAddJobStructure(new TestOutputStructure(ID, new OutputPrototypData()) {
            Output = new Item[] { ItemProvider.Stone_1 },
            Tiles = new List<Tile> { mockutil.GetInCityTile(10, 10) }
        }, new Item[] { ItemProvider.Stone_N(3) });
        PrototypeData.workerID = "worker";
        OutputTestStructure.Output = new[] { ItemProvider.Stone_1 };
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker"))
            .Returns(new WorkerPrototypeData(){ hasToFollowRoads = true });

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(0);
    }

    [Test]
    public void SendOutWorkerIfCan_HasToFollowRoads() {
        Route route = new Route();

        CreateTwoByThree();
        OutputTestStructure.TestAddJobStructure(new TestOutputStructure(ID, new OutputPrototypData()) {
            Output = new Item[] { ItemProvider.Stone_1 },
            Tiles = new List<Tile> { mockutil.GetInCityTile(10, 10) },
            TestRoutes = new HashSet<Route> { route }
        }, new Item[] { ItemProvider.Stone_N(3) });
        PrototypeData.workerID = "worker";
        OutputTestStructure.Output = new[] { ItemProvider.Stone_1 };
        OutputTestStructure.TestRoutes = new HashSet<Route> { route };
        mockutil.PrototypControllerMock.Setup(p => p.GetWorkerPrototypDataForID("worker"))
            .Returns(new WorkerPrototypeData() { hasToFollowRoads = true });

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems.Length).IsEqualTo(1);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].ID).IsEqualTo(ItemProvider.Stone.ID);
        AssertThat(OutputTestStructure.Workers[0].ToGetItems[0].count).IsEqualTo(1);
    }

    [Test]
    public void SendOutWorkerIfCan_NoTargets() {

        OutputTestStructure.TestTrySendOutWorker();

        AssertThat(OutputTestStructure.Workers.Count).IsEqualTo(0);
    }

    public class TestOutputStructure : OutputStructure {
        public HashSet<Route> TestRoutes {
            get => Routes;
            set => Routes = value;
        }
        public List<Worker> Workers {
            get => workers;
            set => workers = value;
        }
        public TestOutputStructure() { }
        public TestOutputStructure(string ID, OutputPrototypData data) {
            this.ID = ID;
            _outputData = data;
        }
        public override Structure Clone() {
            return new TestOutputStructure(ID, _outputData);
        }
        public override void OnBuild() {
        }
        internal bool? IsOutputChangedCallbackRegistered() {
            return cbOutputChange != null;
        }

        public void TestTrySendOutWorker() {
            Workers ??= new List<Worker>();
            SendOutWorkerIfCan();
        }

        public void TestAddJobStructure(TestOutputStructure testOutputStructure, Item[] items) {
            WorkerJobsToDo ??= new Dictionary<OutputStructure, Item[]>();
            WorkerJobsToDo[testOutputStructure] = items;
        }
    }

}

