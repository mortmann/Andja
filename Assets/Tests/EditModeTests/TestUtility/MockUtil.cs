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
    Dictionary<(int, int), Tile> tiles = new Dictionary<(int, int), Tile>();
    public MockUtil() {
        PrototypControllerMock = new Mock<IPrototypController>();
        PrototypController.Instance = PrototypControllerMock.Object;
        WorldMock = new Mock<IWorld>();
        World.Current = WorldMock.Object;
        IslandMock = new Mock<IIsland>();
        CityMock = new Mock<ICity>();
        WorldIsland = new Island();
        Mock<City> worldCityMock = new Mock<City>();
        worldCityMock.Setup(x => x.RemoveStructure(It.IsAny<Structure>()));
        worldCityMock.Setup(x => x.RemoveTile(It.IsAny<Tile>()));
        WorldCity = worldCityMock.Object;
        WorldIsland.Wilderness = new City(-1, WorldIsland);
        WorldIsland.Cities = new List<City>();

        WorldCity.Island = WorldIsland;
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<float>(), It.IsAny<float>())).Returns((float x, float y) => {
            return CreateTile(x, y);
        });
        WorldMock.Setup(w => w.GetTileAt(It.IsAny<int>(), It.IsAny<int>())).Returns((int x, int y) => {
            return CreateTile(x, y);
        });
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
