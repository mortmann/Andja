using Andja.Model;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Andja.Controller;
using UnityEngine;
using static Andja.Model.HomeStructure;
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;

public class ServiceStructureTest {
    private const string TestEffectStructureID = "TestEffectStructureID";
    private const string NotTestEffectStructureID = "Not" + TestEffectStructureID;
    private const string ID = "service";
    TestServiceStructure Service;
    ServiceStructurePrototypeData PrototypeData;
    private StructurePrototypeData TestEffectPrototypeData;
    private MockUtil mockutil;
    private Effect _effect;

    [SetUp]
    public void SetUp() {
        Service = new TestServiceStructure(ID, PrototypeData) {
        };
        PrototypeData = new ServiceStructurePrototypeData() {
            structureRange = 10,
            effectsOnTargets = Array.Empty<Effect>()
        };
        mockutil = new MockUtil();
        var prototypeControllerMock = mockutil.PrototypControllerMock;
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(ID)).Returns(PrototypeData);
        TestEffectPrototypeData = new StructurePrototypeData();
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(TestEffectStructureID)).Returns(TestEffectPrototypeData);
        prototypeControllerMock.Setup(m => m.GetStructurePrototypDataForID(NotTestEffectStructureID)).Returns(TestEffectPrototypeData);

        _effect = new Effect("effect", new EffectPrototypeData() {
            targets = new TargetGroup() {
                Targets = new HashSet<Target> { Target.HomeStructure }
            }
        });

        Service.City = mockutil.City;
        CreateTwoByTwo();
    }


    private void CreateTwoByTwo() {
        PrototypeData.tileWidth = 2;
        PrototypeData.tileHeight = 2;
        Service.Tiles = Service.GetBuildingTiles(World.Current.GetTileAt(Service.StructureRange, Service.StructureRange));
        Service.RangeTiles = new HashSet<Tile>();
        Service.RangeTiles.UnionWith(PrototypeData.PrototypeRangeTiles);
    }

    [Test]
    public void OnBuild_CityEffect() {
        PrototypeData.targets = ServiceTarget.City;
        Effect effect = new Effect("effect", new EffectPrototypeData() {
            targets = new TargetGroup() {
                Targets = new HashSet<Target> { Target.City }
            }
        });
        PrototypeData.effectsOnTargets = new[] { effect };
        PrototypeData.usageItems = new[] { ItemProvider.Wood_1, ItemProvider.Stone_1 };

        Service.OnBuild();
        AssertThat(Service.remainingUsageItems.Length).IsEqualTo(2);
        AssertThat(mockutil.CityMock)
            .HasInvoked(c => c.AddEffect(It.IsIn(PrototypeData.effectsOnTargets)))
            .Exactly(PrototypeData.effectsOnTargets.Length);
    }
    [Test]
    public void OnDestroy_CityEffect() {
        PrototypeData.targets = ServiceTarget.City;
        Effect effect = new Effect("effect", new EffectPrototypeData() {
            targets = new TargetGroup() {
                Targets = new HashSet<Target> { Target.HomeStructure }
            }
        });
        PrototypeData.effectsOnTargets = new[] { effect };
        Service.OnDestroy();
        
        
        AssertThat(mockutil.CityMock)
            .HasInvoked(c => c.RemoveEffect(It.IsIn(PrototypeData.effectsOnTargets), false))
            .Exactly(PrototypeData.effectsOnTargets.Length);
    }
    [Test]
    public void OnDestroy_AddEffect_ButActuallyRemovesIt() {
        PrototypeData.targets = ServiceTarget.Homes;
        PrototypeData.function = ServiceFunction.AddEffect;
        Service.SetupCallbacksTest();

        Worker worker = new Worker();
        Service.Workers.Add(worker);
        PrototypeData.effectsOnTargets = new[] { _effect };
        foreach (Tile tile in Service.RangeTiles) {
            tile.Structure = new TestEffectStructure(TestEffectStructureID, _effect);
        }

        TestEffectStructure first = (TestEffectStructure)Service.RangeTiles.First().Structure;
        string noteffectID = "noteffect";
        first.TestAddEffect(new Effect(noteffectID, new EffectPrototypeData())); 
        AssertThat(Service.RangeTiles).AllSatisfy(t => ((TestEffectStructure)t.Structure).TestHasEffect(_effect));
        Service.OnDestroy();
        AssertThat(mockutil.CityMock).HasInvoked(c => c.UnregisterStructureAdded(It.IsAny<Action<Structure>>())).Once();
        AssertThat(mockutil.CityMock).HasNotPerformedOtherInvocations();
        AssertThat(worker.IsAlive).IsFalse();
        AssertThat(Service.RangeTiles).AllSatisfy(t => ((TestEffectStructure)t.Structure).TestHasEffect(_effect) == false);
        AssertThat(first.TestHasEffect(new Effect(noteffectID, new EffectPrototypeData())));
    }

    [Test]
    public void Load() {
        Worker worker = new Worker();
        Service.Workers.Add(worker);

        PrototypeData.usageItems = new[] { ItemProvider.Wood_1, ItemProvider.Stone_1 };
        Service.TestSetRemainingUsageItems(new[] { ItemProvider.Wood_1 });
        Service.Load();

        AssertThat(Service.remainingUsageItems.Length).IsEqualTo(PrototypeData.usageItems.Length);
        AssertThat(Service.Workers).AllSatisfy(w => w.Home == Service);
    }

    [Test]
    public void SetUpStructures() {
        Service.TodoOnNewTarget += mockutil.Callbacks.Object.Structure;
        Service.OnEffectChange += mockutil.Callbacks.Object.EventableEffectChange;
        Service.RangeTiles.ToList().ForEach(x=>x.Structure = new TestEffectStructure(TestEffectStructureID, _effect));
        mockutil.Callbacks.Setup(cb => cb.Structure(It.IsAny<Structure>()));
        mockutil.Callbacks.Setup(cb => cb.EventableEffectChange(It.IsAny<IGEventable>(), It.IsAny<Effect>(), It.IsAny<bool>()));
        Service.TestSetUpStructures();
        AssertThat(Service.GetTodoOnNewTargetCallbackCount()).IsEqualTo(2);
        AssertThat(mockutil.Callbacks)
            .HasInvoked(cb => cb.Structure(It.IsAny<Structure>()))
            .Exactly(Service.RangeTiles.Count);
        AssertThat(mockutil.Callbacks)
            .HasInvoked(cb => 
                cb.EventableEffectChange(It.IsAny<IGEventable>(), It.IsAny<Effect>(), It.IsAny<bool>()))
            .Exactly(Service.RangeTiles.Count);
    }
    [Test]
    public void SetUpStructures_SpecificRange() {
        PrototypeData.specificRange = new Structure[] { new TestEffectStructure(TestEffectStructureID, _effect) };
        Service.TodoOnNewTarget += mockutil.Callbacks.Object.Structure;
        Service.OnEffectChange = mockutil.Callbacks.Object.EventableEffectChange;
        const int effectStructuresCount = 20;
        var temp = Service.RangeTiles.Take(effectStructuresCount).ToList();
        temp.ForEach(x => x.Structure = new TestEffectStructure(TestEffectStructureID, _effect));
        Service.RangeTiles.Where(x => x.Structure == null).ToList().ForEach(x => x.Structure = new TestEffectStructure(NotTestEffectStructureID, _effect));
        Service.TestSetUpStructures();
        AssertThat(Service.GetTodoOnNewTargetCallbackCount()).IsEqualTo(2);
        AssertThat(mockutil.Callbacks)
            .HasInvoked(cb => cb.Structure(It.IsAny<Structure>()))
            .Exactly(effectStructuresCount);
        AssertThat(mockutil.Callbacks)
            .HasInvoked(cb =>
                cb.EventableEffectChange(It.IsAny<IGEventable>(), It.IsAny<Effect>(), It.IsAny<bool>()))
            .Exactly(effectStructuresCount);
    }

    [Test]
    public void CheckForMissingEffect() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        Service.TestCheckForMissingEffect(mockutil.EventableMock.Object, _effect, false);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.AddEffect(It.Is<Effect>(e=>e.ID == _effect.ID))).Once();
    }
    [Test]
    public void CheckForMissingEffect_TargetHasEffect_NotUnique() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        mockutil.EventableMock.Setup(e => e.HasEffect(It.IsAny<Effect>())).Returns(true);
        Service.TestCheckForMissingEffect(mockutil.EventableMock.Object, _effect, false);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.AddEffect(It.Is<Effect>(e => e.ID == _effect.ID))).Once();
    }
    [Test]
    public void CheckForMissingEffect_TargetHasEffect_Unique() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        _effect.EffectPrototypeData.unique = true;
        mockutil.EventableMock.Setup(e => e.HasEffect(It.IsAny<Effect>())).Returns(true);
        Service.TestCheckForMissingEffect(mockutil.EventableMock.Object, _effect, false);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.AddEffect(It.Is<Effect>(e => e.ID == _effect.ID))).Never();
    }
    [Test]
    public void CheckForMissingEffect_DifferentEffect() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        Service.TestCheckForMissingEffect(mockutil.EventableMock.Object, new Effect("Different", new EffectPrototypeData()), false);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.AddEffect(It.Is<Effect>(e => e.ID == _effect.ID))).Never();
    }
    [Test]
    public void CheckForMissingEffect_EffectStarted() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        Service.TestCheckForMissingEffect(mockutil.EventableMock.Object, _effect, true);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.AddEffect(_effect)).Never();
    }

    [Test]
    public void RemoveEffectOverTime() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        PrototypeData.workSpeed = 1;
        TestEffectStructure structure = new TestEffectStructure(TestEffectStructureID, _effect);
        Service.TestRemoveEffectOverTime(structure, 0.5f);
        AssertThat(structure.TestHasEffect(_effect)).IsTrue();
        Service.TestRemoveEffectOverTime(structure, 1f);
        AssertThat(structure.TestHasEffect(_effect)).IsFalse();
    }

    [Test]
    public void RepairStructure() {
        PrototypeData.workSpeed = 50;
        TestEffectStructure structure = new TestEffectStructure(TestEffectStructureID, _effect);
        structure.CurrentHealth = 1;
        TestEffectPrototypeData.maxHealth = 500;
        Service.TestRepairStructure(structure, 1f);
        AssertThat(structure.CurrentHealth).IsEqualTo(51);
        Service.TestRepairStructure(structure, 123f);
        AssertThat(structure.CurrentHealth).IsEqualTo(structure.MaxHealth);
    }

    [Test]
    public void PreventEffect() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        Service.TestPreventEffect(mockutil.EventableMock.Object, _effect, true);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.RemoveEffect(_effect, true)).Once();
    }
    [Test]
    public void PreventEffect_EffectEnded() {
        PrototypeData.effectsOnTargets = new[] { _effect };
        Service.TestPreventEffect(mockutil.EventableMock.Object, _effect, false);
        AssertThat(mockutil.EventableMock).HasInvoked(e => e.RemoveEffect(_effect, true)).Never();
    }
    class TestEffectStructure : Structure {
        public TestEffectStructure(string ID, Effect effect) {
            this.ID = ID;
            effects = new List<Effect>();
            effects.Add(effect);
        }
        public void TestAddEffect(Effect effect) {
            effects.Add(effect);
        }
        public bool TestHasEffect(Effect effect) {
            return effects.Contains(effect);
        }

        public override Structure Clone() {
            return null;
        }
        public override void OnBuild() {
        }
    }

    class TestServiceStructure : ServiceStructure {
        
        public List<Worker> Workers => workers;
        public Action<Structure> TodoOnNewTarget { get; set; }
        public Action<IGEventable, Effect, bool> OnEffectChange;
        public TestServiceStructure(string iD, ServiceStructurePrototypeData prototypeData) : base(iD, prototypeData) {
            workers = new List<Worker>();
            //setting directly DOES NOT WORK?!?
        }

        public void SetupCallbacksTest() {
            SetCallbacks();
        }

        public void TestSetRemainingUsageItems(Item[] items) {
            remainingUsageItems = new[] { 0.5f };
        }

        public void TestSetUpStructures() {
            todoOnNewTarget += structure => TodoOnNewTarget.Invoke(structure);
            onTargetEffectChange += (eventable, effect, arg3) => OnEffectChange.Invoke(eventable, effect, arg3);
            SetUpStructures();
        }

        public int GetTodoOnNewTargetCallbackCount() {
            return todoOnNewTarget.GetInvocationList().Length;
        }

        public void TestCheckForMissingEffect(IIGEventable eventable, Effect _effect, bool start) {
            CheckForMissingEffect(eventable, _effect, start);
        }

        public void TestRemoveEffectOverTime(TestEffectStructure structure, float deltaTime) {
            RemoveEffectOverTime(structure, deltaTime);
        }

        public void TestRepairStructure(TestEffectStructure structure, float deltaTime) {
            RepairStructure(structure, deltaTime);
        }

        public void TestPreventEffect(IIGEventable eventableMockObject, Effect effect, bool added) {
            PreventEffect(eventableMockObject, effect, added);
        }
    }
}
