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
    }

    [Test]
    public void CreateVolcanicEruption() {
        GameEvent.CreateVolcanicEruption();

        AssertThat(mockUtil.EventSpriteControllerMock).HasInvoked(esp => esp.CreateEventTileSprites(ID, GameEvent));
    }

}
