using Andja.Controller;
using Andja.Model;
using Moq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockUtil {
    public Mock<ITestCallback> Callbacks;

    public Mock<IIsland> IslandMock;
    public Mock<IWorld> WorldMock;
    public Mock<ICity> CityMock;
    public Mock<ICity> OtherCityMock;

    public ICity City => CityMock.Object;
    public ICity OtherCity => OtherCityMock.Object;
    public Mock<IIGEventable> EventableMock;
    public Mock<IPrototypController> PrototypControllerMock;
    public Mock<IPlayerController> PlayerControllerMock;

    public Island WorldIsland;
    Dictionary<(int, int), Tile> tiles = new Dictionary<(int, int), Tile>();
    public MockUtil() {
        Callbacks = new Mock<ITestCallback>();
        PrototypControllerMock = new Mock<IPrototypController>();
        PrototypControllerMock.Setup(p => p.GetCopieOfAllItems()).Returns(new Dictionary<string, Item>() {
            { ItemProvider.Brick.ID, ItemProvider.Brick.Clone() },
            { ItemProvider.Tool.ID, ItemProvider.Tool.Clone() },
            { ItemProvider.Wood.ID, ItemProvider.Wood.Clone() },
            { ItemProvider.Fish.ID, ItemProvider.Fish.Clone() },
            { ItemProvider.Stone.ID, ItemProvider.Stone.Clone() },
        });
        PrototypController.Instance = PrototypControllerMock.Object;
        PlayerControllerMock = new Mock<IPlayerController>();
        PlayerControllerMock.Setup(pc => pc.HasEnoughMoney(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        PlayerControllerMock.Setup(pc => pc.HasEnoughMoney(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        PlayerControllerMock.SetupGet(pc => pc.Players).Returns(new List<Player>() { new Player()});

        PlayerController.Instance = PlayerControllerMock.Object;

        WorldIsland = new Island();
        WorldIsland.Wilderness = new City(-1, WorldIsland);
        WorldIsland.Cities = new List<ICity>();

        WorldMock = new Mock<IWorld>();
        World.Current = WorldMock.Object;
        IslandMock = new Mock<IIsland>();
        CityMock = new Mock<ICity>();
        CityMock.Setup(c => c.PlayerNumber).Returns(0);
        CityMock.Setup(x => x.RemoveStructure(It.IsAny<Structure>()));
        CityMock.Setup(x => x.RemoveTile(It.IsAny<Tile>()));
        CityMock.Setup(c => c.Island).Returns(WorldIsland);
        CityInventory inventory = new CityInventory(1);
        CityMock.SetupGet(c => c.Inventory).Returns(inventory);
        OtherCityMock = new Mock<ICity>();
        OtherCityMock.Setup(c => c.PlayerNumber).Returns(1);

        EventableMock = new Mock<IIGEventable>();

        WorldMock.Setup(w => w.GetTileAt(It.IsAny<float>(), It.IsAny<float>())).Returns((float x, float y) => CreateTile(x, y));
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => CreateTile(x, y));
    }

    private Tile CreateTile(float fx, float fy) {
        int x = (int)fx;
        int y = (int)fy;
        if(tiles.ContainsKey((x,y))) {
            return tiles[(x, y)];
        }
        LandTile t = new LandTile(x, y) {
            Type = TileType.Dirt,
            Island = WorldIsland
        };
        tiles[(x, y)] = t;
        return t;
    }
}
