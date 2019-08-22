using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class Crate {
    const float crateDespawnTime = 180f;
    public const float pickUpDistance = 2f;
    [JsonPropertyAttribute] public float despawnTime;
    [JsonPropertyAttribute] public Vector2 position;
    [JsonPropertyAttribute] public Item item;
    public Action<Crate> onDespawn;
    public bool despawned = false;

    public Crate(Vector2 position, Item item) {
        this.despawnTime = crateDespawnTime;
        this.position = position;
        this.item = item;
    }

    public void Update(float deltaTime) {
        despawnTime -= deltaTime;
        if (despawnTime < 0)
            Despawn();
    }

    internal void RemoveItemAmount(int pickedup) {
        item.count -= pickedup;
        if (item.count <= 0) {
            Despawn();
        }
    }
    public void Despawn() {
        onDespawn?.Invoke(this);
        item = null;
        despawned = true;
    }

    internal bool IsInRange(Vector2 currentPosition) {
        Vector2 distance = position - currentPosition;
        if (distance.magnitude > Crate.pickUpDistance)
            return false;
        return true;
    }
}
