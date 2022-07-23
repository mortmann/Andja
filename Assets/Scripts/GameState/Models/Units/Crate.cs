using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class Crate {
        private const float crateDespawnTime = 180f;
        public const float pickUpDistance = 2f;
        [JsonPropertyAttribute] public float despawnTime;
        [JsonPropertyAttribute] public Vector2 position;
        [JsonPropertyAttribute] public Item item;
        public Action<Crate> onDespawn;
        public bool despawned;

        public Crate(Vector2 position, Item item) {
            despawnTime = crateDespawnTime;
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
            if (distance.magnitude > pickUpDistance)
                return false;
            return true;
        }
    }
}