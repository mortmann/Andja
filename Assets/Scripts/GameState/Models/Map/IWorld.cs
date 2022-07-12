using Andja.Model.Generator;
using Andja.Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {
    public interface IWorld {
        Vector2 Center { get; }
        List<Crate> Crates { get; }
        int Height { get; }
        List<Island> Islands { get; }
        List<Projectile> Projectiles { get; }
        Tile[] Tiles { get; }
        WorldGraph WorldGraph { get; }
        bool[][] TilesMap { get; }
        List<Unit> Units { get; }
        int Width { get; }

        void ChangeWorldGraph(Tile t, bool b);
        void CreateIsland(MapGenerator.IslandData islandStruct);
        void CreateIslands(List<MapGenerator.IslandData> doneIslands);
        void CreateItemOnMap(Item i, Vector2 toSpawnPosition);
        Unit CreateUnit(Unit prefabUnit, Player player, Tile startTile, int nonPlayerNumber = 0);
        void CreateWorkerGameObject(Worker worker);
        void DespawnItem(Crate c);
        void Destroy();
        void FixedUpdate(float deltaTime);
        Fertility GetFertility(string ID);
        IEnumerable<IGEventable> GetLandUnits();
        int GetPlayerNumber();
        Tile GetRandomOceanTile();
        IEnumerable<IGEventable> GetShipUnits();
        Tile GetTileAt(float fx, float fy);
        Tile GetTileAt(int x, int y);
        Tile GetTileAt(Vector2 vec);
        Queue<Tile> GetTilesQueue(Queue<Vector2> q);
        bool IsInTileAt(Tile t, float x, float y);
        void Load();
        void LoadTiles(Tile[] tiles, int width, int height);
        void LoadWaterTiles();
        void OnCreateProjectile(Projectile pro);
        void OnEventCreate(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        void OnTileChanged(Tile t);
        void OnUnitDestroy(Unit u, IWarfare warfare);
        void RegisterAnyUnitDestroyed(Action<Unit, IWarfare> onAnyUnitDestroyed);
        void RegisterCrateDespawned(Action<Crate> onDespawned);
        void RegisterCrateSpawned(Action<Crate> onSpawned);
        void RegisterOnCreateProjectileCallback(Action<Projectile> cb);
        void RegisterTileChanged(Action<Tile> callbackfunc);
        void RegisterUnitCreated(Action<Unit> callbackfunc);
        void RegisterWorkerCreated(Action<Worker> callbackfunc);
        void SetTileAt(int x, int y, Tile t);
        void SetupWorld();
        void UnregisterCrateDespawned(Action<Crate> onDespawned);
        void UnregisterCrateSpawned(Action<Crate> onSpawned);
        void UnregisterOnCreateProjectileCallback(Action<Projectile> cb);
        void UnregisterTileChanged(Action<Tile> callbackfunc);
        void UnregisterUnitCreated(Action<Unit> callbackfunc);
        void UnregisterUnitDestroyed(Action<Unit, IWarfare> onAnyUnitDestroyed);
        void UnregisterWorkerCreated(Action<Worker> callbackfunc);
        void Update(float deltaTime);
    }
}