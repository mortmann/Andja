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
        AssertThat(GameEvent.position).IsEquivalentTo(new Vector2(50, 50));
    }

    [Test]
    public void IsWorldEvent() {
        PrototypeData.maxDuration = 5;
        GameEvent.StartEvent(new Vector2(50, 50));
        AssertThat(GameEvent.currentDuration).IsEqualTo(5);
        AssertThat(GameEvent.position.IsFlooredVector(new Vector2(50, 50))).IsTrue();
    }

    [Test]
    public void EffectTarget() {
    }

}
