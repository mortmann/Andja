using Andja.Controller;
using Andja.Model;
using Moq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Andja;
using UnityEngine;
using Andja.Pathfinding;

public class MockUtil {
    public Mock<ITestCallback> Callbacks;

    public Mock<IIsland> IslandMock;
    public Mock<IWorld> WorldMock;
    public Mock<ICity> CityMock;
    public Mock<ICity> OtherCityMock;
    public Mock<IWarfare> IWarfareMock;

    public ICity City => CityMock.Object;
    public ICity OtherCity => OtherCityMock.Object;
    public Mock<IGEventable> EventableMock;
    public Mock<IPrototypController> PrototypControllerMock;
    public Mock<IPlayerController> PlayerControllerMock;
    public Mock<IBuildController> BuildControllerMock;
    public Mock<PathfindingThreadHandler> pathfindingMock;

    public Island WorldIsland;
    Dictionary<(int, int), Tile> tiles = new Dictionary<(int, int), Tile>();
    public Dictionary<string, Item> AllItems = new Dictionary<string, Item>() {
        { ItemProvider.Brick.ID, ItemProvider.Brick.Clone()
        },
        { ItemProvider.Tool.ID, ItemProvider.Tool.Clone() },
        { ItemProvider.Wood.ID, ItemProvider.Wood.Clone() },
        { ItemProvider.Fish.ID, ItemProvider.Fish.Clone() },
        { ItemProvider.Stone.ID, ItemProvider.Stone.Clone() },
    };

    public readonly Mock<IEventSpriteController> EventSpriteControllerMock;

    public MockUtil() {
        Callbacks = new Mock<ITestCallback>();
        PrototypControllerMock = new Mock<IPrototypController>();
        PrototypControllerMock.Setup(p => p.GetCopieOfAllItems())
            .Returns(() => AllItems.ToDictionary(d=>d.Key, d=>d.Value.Clone()));
        PrototypControllerMock.Setup(p => p.GetItemPrototypDataForID(It.IsAny<string>()))
            .Returns(new ItemPrototypeData() { type = ItemType.Build });
        PrototypController.Instance = PrototypControllerMock.Object;
        PlayerControllerMock = new Mock<IPlayerController>();
        PlayerControllerMock.Setup(pc => pc.HasEnoughMoney(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
        PlayerControllerMock.SetupGet(pc => pc.Players).Returns(new List<Player>() { new Player() });
        PlayerControllerMock.Setup(pc => pc.GetPlayer(It.IsAny<int>())).Returns((int num) => new Player(num, false, 50000));

        PlayerController.Instance = PlayerControllerMock.Object;

        WorldIsland = new Island();
        WorldIsland.Wilderness = new City(-1, WorldIsland);
        WorldIsland.Cities = new List<ICity>();

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

        EventableMock = new Mock<IGEventable>();

        WorldMock = new Mock<IWorld>();
        World.Current = WorldMock.Object;
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<float>(), It.IsAny<float>())).Returns((float x, float y) => CreateTile(x, y));
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => CreateTile(x, y));
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<Vector2>())).Returns((Vector2 vec) => CreateTile(vec.x, vec.y));

        BuildControllerMock = new Mock<IBuildController>();
        BuildController.Instance = BuildControllerMock.Object;

        IWarfareMock = new Mock<IWarfare>();
        pathfindingMock = new Mock<PathfindingThreadHandler>();
        PathfindingThreadHandler.Instance = pathfindingMock.Object;

        EventSpriteControllerMock = new Mock<IEventSpriteController>();
        EventSpriteController.Instance = EventSpriteControllerMock.Object;
    }

    public MockUtil WithOnePopulationLevels() {
        PopulationLevelPrototypData plpd = new PopulationLevelPrototypData();
        PrototypControllerMock.Setup(p => p.GetPopulationLevelPrototypDataForLevel(0)).Returns(plpd);
        PrototypControllerMock.Setup(p => p.GetPopulationLevels(It.IsAny<ICity>()))
            .Returns<ICity>((c) => new List<PopulationLevel>() { new PopulationLevel(0, c, null) });
        return this;
    }

    public LandTile GetInCityTile(int x, int y) {
        LandTile tile = World.Current.GetTileAt(x,y) as LandTile;
        tile.City = City;
        return tile;
    }
    public LandTile GetInOtherCityTile(int x, int y) {
        LandTile tile = World.Current.GetTileAt(x, y) as LandTile;
        tile.City = OtherCity;
        return tile;
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
