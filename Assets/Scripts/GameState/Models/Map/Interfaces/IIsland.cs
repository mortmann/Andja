using Andja.Pathfinding;
using System.Collections.Generic;

namespace Andja.Model {
    public interface IIsland {
        List<ICity> Cities { get; set; }
        Climate Climate { get; set; }
        List<IslandFeature> Features { get; set; }
        PathGrid Grid { get; }
        int Height { get; }
        Dictionary<string, int> Resources { get; set; }
        Tile StartTile { get; set; }
        int Width { get; }
        ICity Wilderness { get; set; }
        List<Fertility> Fertilities { get; set; }

        void AddResources(string resourceID, int count);
        void ChangeGridTile(LandTile landTile, bool cityChange = false);
        ICity CreateCity(int playerNumber);
        ICity FindCityByPlayer(int playerNumber);
        int GetPlayerNumber();
        bool HasResource(string resourceID);
        IEnumerable<Structure> Load();
        void OnEventCreate(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        void RemoveCity(ICity c);
        void RemoveResources(string resourceID, int count);
        void SetTiles(Tile[] tiles);
        void Update(float deltaTime);
    }
}