using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class Pirate {

    [JsonPropertyAttribute] float startCooldown = 5f;
    [JsonPropertyAttribute] List<Ship> Ships;

    // Use this for initialization
    public Pirate() {
        Ships = new List<Ship>();
    }

    // Update is called once per frame
    public void Update(float deltaTime) {
        if (startCooldown > 0) {
            startCooldown -= deltaTime;
            return;
        }
        if (Ships.Count < 2) {
            AddShip();
        }
    }

    public void AddShip() {
        Ship ship = PrototypController.Instance.GetPirateShipPrototyp();
        Tile t = World.Current.GetTileAt(UnityEngine.Random.Range(0, World.Current.Height), 0);
        ship = (Ship) World.Current.CreateUnit(ship, null, t);
        ship.RegisterOnDestroyCallback(OnShipDestroy);
        ship.RegisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
        Ships.Add(ship);
    }

    private void OnShipArriveDestination(Unit unit, bool goal) {
        Ship ship = unit as Ship;
        if (Ships.Contains(ship) ==false) {
            Debug.LogError("Why did called when it is not a pirate ship?");
            return;
        }
        if (goal) {
            int x = UnityEngine.Random.Range(0, World.Current.Width);
            int y = UnityEngine.Random.Range(0, World.Current.Height);
            Tile t = World.Current.GetTileAt(x, y);
            if (t.Type == TileType.Ocean)
                ship.GiveMovementCommand(t);
        }
    }

    public void OnShipDestroy(Unit u) {
        u.UnregisterOnArrivedAtDestinationCallback(OnShipArriveDestination);
        u.UnregisterOnDestroyCallback(OnShipDestroy);
        Ships.Remove((Ship)u);
    }

}
