using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[JsonObject(MemberSerialization.OptIn)]
public class Projectile {
    //for now there will be NO friendly fire!
    [JsonPropertyAttribute] IWarfare origin;
    [JsonPropertyAttribute] float remainingTravelDistance;
    [JsonPropertyAttribute] SeriaziableVector2 _position;
    [JsonPropertyAttribute] SeriaziableVector2 _destination;
    [JsonPropertyAttribute] ITargetable target;
    [JsonPropertyAttribute] public bool HasHitbox;
    [JsonPropertyAttribute] public bool Impact;
    [JsonPropertyAttribute] public int ImpactRange;
    [JsonPropertyAttribute] public string SpriteName = "cannonball_1";
    float Speed = 2f;
    public Vector2 Position { get { return _position; } protected set { _position = value; } }
    Vector2 Destination { get { return _destination.Vec; } set { _destination.Vec = value; } }

    public SeriaziableVector2 Velocity { get; internal set; }

    Action<Projectile> cbOnDestroy;
    Action<Projectile> cbOnChange;

    public Projectile() { }
    public Projectile(IWarfare origin, Vector3 startPosition, ITargetable target, Vector2 destination, 
        Vector3 move, float travelDistance, bool HasHitbox, float speed = 2, bool impact = false, int impactRange = 1) {
        Speed = speed;
        remainingTravelDistance = travelDistance;
        Velocity = move * speed;
        _position = startPosition;
        _destination = destination; // needs some kind of random factor
        this.origin = origin;
        this.target = target;
        this.HasHitbox = HasHitbox;
        this.Impact = impact;
        this.ImpactRange = impactRange;
    }

    public void Update(float deltaTime) {
        //Vector3 dir = Destination - Position;
        //if (dir.magnitude < 0.1f) {
        //    Destroy();
        //}
        if (remainingTravelDistance < 0) {
            Destroy();
            return;
        }
        Vector2 dir = Velocity.Vec * deltaTime;
        remainingTravelDistance -= dir.magnitude;
        Position += dir;
    }

    private void Destroy() {
        if(Impact) {
            List<Tile> tiles = Util.CalculateCircleTiles(ImpactRange, 0, 0, Position.x, Position.y);
            foreach (Tile t in tiles) {
                if (t.Structure != null) {
                    t.Structure.ReduceHealth(origin.CurrentDamage); //TODO: think about this.
                }
            }
            //TODO: show impact crater
        }
        cbOnDestroy?.Invoke(this);
    }

    public bool OnHit(ITargetable hit) {
        if (ConfirmHit(hit) == false)
            return false;
        if (hit.IsAttackableFrom(origin) == false)
            return false;
        hit.TakeDamageFrom(origin);
        Destroy();
        return true;
    }
    private bool ConfirmHit(ITargetable hit) {
        //Does it have to be the targeted unit it damages???
        //if (hit == target)
        //    return true;
        if (hit.PlayerNumber == origin.PlayerNumber)
            return false;
        if (PlayerController.Instance.ArePlayersAtWar(origin.PlayerNumber, hit.PlayerNumber)) {
            return true;
        }
        return false;
    }

    public void RegisterOnDestroyCallback(Action<Projectile> cb) {
        cbOnDestroy += cb;
    }
    public void UnregisterOnDestroyCallback(Action<Projectile> cb) {
        cbOnDestroy -= cb;
    }
}
