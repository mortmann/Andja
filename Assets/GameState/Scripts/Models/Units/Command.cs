using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public abstract class Command {
    public virtual Vector2 Position => Vector2.zero;
    public abstract bool IsFinished { get; }
    public abstract UnitMainModes MainMode { get; }
}
[JsonObject(MemberSerialization.OptIn)]
public class MoveCommand : Command {
    public override UnitMainModes MainMode => UnitMainModes.Moving;
    bool isFinished;
    public override bool IsFinished => isFinished;
    public override Vector2 Position => _position;
    private Vector2 _position;

    public MoveCommand(Vector2 destination) {
        _position = destination;
    }

    internal void SetFinished() {
        isFinished = true;
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class AttackCommand : Command {
    public override bool IsFinished => target.IsDestroyed;
    public override UnitMainModes MainMode => UnitMainModes.Attack;
    public override Vector2 Position => target.CurrentPosition;

    public ITargetable target;
    public AttackCommand(ITargetable target) {
        this.target = target;
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class CaptureCommand : Command {
    public override bool IsFinished => target.Captured;
    public override UnitMainModes MainMode => UnitMainModes.Capture;
    public ICapturable target;
    public override Vector2 Position => target.CurrentPosition;
    public CaptureCommand(ICapturable target) {
        this.target = target;
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class PickUpCrateCommand : Command {
    public override bool IsFinished => crate.despawned;
    public override UnitMainModes MainMode => UnitMainModes.PickUpCrate;
    public Crate crate;
    public override Vector2 Position => crate.position;
    public PickUpCrateCommand(Crate crate) {
        this.crate = crate;
    }
}