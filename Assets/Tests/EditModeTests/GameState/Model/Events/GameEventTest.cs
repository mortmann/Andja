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
using static AssertNet.Assertions;
using static AssertNet.Moq.Assertions;
using Andja.Pathfinding;

public class GameEventTest {
    GameEvent GameEvent;
    private GameEventPrototypData PrototypeData;
    private MockUtil mockUtil;
    public string ID;

    [SetUp]
    public void SetUp() {
        ID = "test";
        GameEvent = new GameEvent(ID);
        mockUtil = new MockUtil();
        mockUtil.PrototypControllerMock.Setup(p => p.GetGameEventPrototypDataForID(ID))
            .Returns(() => PrototypeData);
        PrototypeData = new GameEventPrototypData();
    }

    [Test]
    public void CreateVolcanicEruption() {
        GameEvent.CreateVolcanicEruption();

        AssertThat(mockUtil.EventSpriteControllerMock).HasInvoked(esp => esp.CreateEventTileSprites(ID, GameEvent));
    }

    [Test]
    public void Update() {
        GameEvent.currentDuration = 2;
        GameEvent.triggerEffectCooldown = 1f;
        GameEvent.Update(1f);
        AssertThat(GameEvent.currentDuration).IsEqualTo(1f);
        AssertThat(GameEvent.triggerEffectCooldown).IsEqualTo(0f);
    }

    [Test]
    public void StartEvent() {
        PrototypeData.maxDuration = 5;
        PrototypeData.minDuration = 5;
        GameEvent.StartEvent(new Vector2(50,50));
        AssertThat(GameEvent.currentDuration).IsEqualTo(5);
        AssertThat(GameEvent.DefinedPosition).IsEquivalentTo(new Vector2(50, 50));
    }

    [Test]
    public void IsWorldEvent() {
        PrototypeData.maxDuration = 5;
        PrototypeData.minDuration = 5;
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.World));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        GameEvent.StartEvent(new Vector2(50, 50));
        AssertThat(GameEvent.currentDuration).IsEqualTo(5);
        AssertThat(GameEvent.DefinedPosition.IsFlooredVector(new Vector2(50, 50))).IsTrue();
        AssertThat(GameEvent.HasWorldEffect());
    }
    [Test]
    public void StartEvent_Duration() {
        PrototypeData.maxDuration = 20;
        PrototypeData.minDuration = 5;
        for (int i = 0; i < 100; i++) {
            GameEvent.StartEvent(new Vector2(50, 50));
            AssertThat(GameEvent.currentDuration).IsInRange(5, 20);
        }
    }
    [Test]
    public void EffectTarget() {
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.World));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        GameEvent.target = mockUtil.EventableMock.Object;
        AssertThat(mockUtil.EventableMock).HasInvoked(e => e.AddEffect(It.IsAny<Effect>()));
    }

    [Test]
    public void IsTarget() {
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        mockUtil.EventableMock.Setup(e => e.TargetGroups).Returns(new TargetGroup(Target.AllStructure));
        GameEvent.target = mockUtil.EventableMock.Object;
        AssertThat(GameEvent.IsTarget(mockUtil.EventableMock.Object)).IsTrue();
    }

    [Test]
    public void IsTarget_FalseOtherTarget() {
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        mockUtil.EventableMock.Setup(e => e.TargetGroups).Returns(new TargetGroup(Target.AllUnit));
        GameEvent.target = mockUtil.EventableMock.Object;
        AssertThat(GameEvent.IsTarget(mockUtil.EventableMock.Object)).IsFalse();
    }

    [Test]
    public void IsTarget_SpecialRange() {
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        mockUtil.EventableMock.Setup(e => e.TargetGroups).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.specialRange = new Dictionary<Target, List<string>> { { Target.AllStructure, new List<string> { "InRange" } } };
        mockUtil.EventableMock.Setup(e => e.GetID()).Returns("InRange");
        GameEvent.target = mockUtil.EventableMock.Object;
        AssertThat(GameEvent.IsTarget(mockUtil.EventableMock.Object)).IsTrue();
    }
    [Test]
    public void IsTarget_FalseNotInSpecialRange() {
        Mock<IEffect> effectMock = new Mock<IEffect>();
        effectMock.Setup(e => e.Targets).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.effects = new IEffect[] { effectMock.Object };
        mockUtil.EventableMock.Setup(e => e.TargetGroups).Returns(new TargetGroup(Target.AllStructure));
        PrototypeData.specialRange = new Dictionary<Target, List<string>> { { Target.AllStructure, new List<string> { "InRange" } } };
        mockUtil.EventableMock.Setup(e => e.GetID()).Returns("NotInRange");
        GameEvent.target = mockUtil.EventableMock.Object;
        AssertThat(GameEvent.IsTarget(mockUtil.EventableMock.Object)).IsFalse();
    }
}
