using UnityEngine;
using System.Collections;
using System;

public class WorldController : MonoBehaviour {
    public static WorldController Instance { get; protected set; }

    // The world and tile data
    public World world { get; protected set; }

    // Use this for initialization
    void OnEnable() {
        if (Instance != null) {
            Debug.LogError("There should never be two world controllers.");
        }
        Instance = this;
        this.world = new World();
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    // Update is called once per frame
    void Update() {
        world.update(Time.deltaTime);
    }

    internal Tile GetTileAtWorldCoord(Vector3 currFramePosition) {
        return world.GetTileAt(Mathf.FloorToInt(currFramePosition.x), Mathf.FloorToInt(currFramePosition.y));
    }
}