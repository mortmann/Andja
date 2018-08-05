using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[JsonObject]
public class Crate {
    const float crateDespawnTime = 180f;
    public const float pickUpDistance = 2f;
    public float despawnTime;
    public Vector2 position;
    public Item item;
    public Action<Crate> onDespawn;

    public Crate(Vector2 position, Item item) {
        this.despawnTime = crateDespawnTime;
        this.position = position;
        this.item = item;
    }

    public void Update(float deltaTime) {
        despawnTime -= deltaTime;
        if (despawnTime < 0)
            onDespawn?.Invoke(this);
    }

    internal void RemoveItemAmount(int pickedup) {
        item.count -= pickedup;
        if(item.count <= 0) {
            onDespawn?.Invoke(this);
        }
    }
}
