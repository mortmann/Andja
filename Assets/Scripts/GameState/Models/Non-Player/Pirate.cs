using UnityEngine;
using System.Collections.Generic;

public class Pirate : MonoBehaviour {

    float startCooldown = 5f;
    List<Ship> Ships;

    // Use this for initialization
    void Start() {
        Ships = new List<Ship>();
    }

    // Update is called once per frame
    void Update() {
        if (WorldController.Instance.IsPaused) {
            return;
        }
        if (startCooldown > 0) {
            startCooldown -= Time.deltaTime;
            return;
        }
        if (Ships.Count < 2) {
            AddShip();
        }

        foreach (Ship s in Ships) {
            //check for ships that needs commands?
            if (s.pathfinding.IsAtDestination) {
                int x = Random.Range(0, World.Current.Width);
                int y = Random.Range(0, World.Current.Height);
                Tile t = World.Current.GetTileAt(x, y);
                if (t.Type == TileType.Ocean)
                    s.GiveMovementCommand(t);
            }
        }
    }

    public void AddShip() {
        //Tile t = World.Current.GetTileAt (Random.Range (0, World.Current.Height), 0);
        //Ship s = (Ship)World.Current.CreateUnit (t, -1, true);
        //s.RegisterOnDestroyCallback (OnShipDestroy);
        //myShips.Add (s);
    }
    public void OnShipDestroy(Unit u) {
        Ships.Remove((Ship)u);
    }

}
