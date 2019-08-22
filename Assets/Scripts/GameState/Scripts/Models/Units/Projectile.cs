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
    [JsonPropertyAttribute] SeriaziableVector3 _position;
    [JsonPropertyAttribute] SeriaziableVector3 _destination;
    [JsonPropertyAttribute] ITargetable target;
    const float Speed = 2f;
    public Vector3 Position { get { return _position.Vec; } protected set { _position.Vec = value; } }
    Vector3 Destination { get { return _destination.Vec; } set { _destination.Vec = value; } }


    Action<Projectile> cbOnDestroy;
    Action<Projectile> cbOnChange;

    public Projectile() { }
    public Projectile(IWarfare origin, Vector3 startPosition, ITargetable target, Vector2 destination, float travelDistance) {
        remainingTravelDistance = travelDistance;
        _position = new SeriaziableVector3(startPosition);
        _destination = new SeriaziableVector3(destination); // needs some kind of random factor
        this.origin = origin;
        this.target = target;
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
        Vector3 dir = Destination * Speed * deltaTime;
        remainingTravelDistance -= dir.magnitude;
        Position += dir;
    }

    private void Destroy() {
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
