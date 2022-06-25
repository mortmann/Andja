using Andja.Controller;
using Andja.Model;
using Moq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockUtil {
    public Mock<IIsland> IslandMock;
    public Mock<IWorld> WorldMock;
    public Mock<ICity> CityMock;
    public Mock<IPrototypController> PrototypControllerMock;
    public Island WorldIsland;
    public City WorldCity;
    public MockUtil() {
        PrototypControllerMock = new Mock<IPrototypController>();
        PrototypController.Instance = PrototypControllerMock.Object;
        WorldMock = new Mock<IWorld>();
        World.Current = WorldMock.Object;
        IslandMock = new Mock<IIsland>();
        CityMock = new Mock<ICity>();
        WorldIsland = new Island();
        WorldCity = new City();
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<float>(), It.IsAny<float>())).Returns((float x, float y) => {
            LandTile t = new LandTile((int)x, (int)y);
            t.Type = TileType.Dirt;
            t.Island = WorldIsland;
            return t;
        });
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => {
            LandTile t = new LandTile(x, y);
            t.Type = TileType.Dirt;
            t.Island = WorldIsland;
            return t;
        });
    }

}
