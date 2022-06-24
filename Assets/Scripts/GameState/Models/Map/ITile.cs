using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public interface ITile {
        float BaseMovementCost { get; }
        City City { get; set; }
        Island Island { get; set; }
        float MovementCost { get; }
        string SpriteName { get; set; }
        Structure Structure { get; set; }
        TileType Type { get; set; }
        Vector3 Vector { get; }
        Vector2 Vector2 { get; }
        int X { get; }
        int Y { get; }

        void AddNeedStructure(NeedStructure ns);
        bool CheckTile(Structure upgradeTo = null);
        float DistanceFromVector(Vector3 vec);
        Tile East();
        List<NeedStructure> GetListOfInRangeCityNeedStructures();
        List<NeedStructure> GetListOfInRangeNeedStructures(int playernumber);
        Tile[] GetNeighbours(bool diagOkay = false);
        bool IsGenericBuildType();
        bool IsInRange(Vector3 vec, float Range);
        bool IsNeighbour(Tile tile, bool diagOkay = false);
        Tile North();
        void RegisterTileOldNewStructureChangedCallback(Action<Structure, Structure> callback);
        void RegisterTileStructureChangedCallback(Action<Tile, Structure> callback);
        void RemoveNeedStructure(NeedStructure ns);
        Tile South();
        LandTile toLandTile();
        LandTile toLandTile(TileType tileType);
        void UnregisterOldNewTileStructureChangedCallback(Action<Structure, Structure> callback);
        void UnregisterTileStructureChangedCallback(Action<Tile, Structure> callback);
        Tile West();
    }
}