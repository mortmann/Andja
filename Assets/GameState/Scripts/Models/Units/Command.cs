using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public abstract class Command {
    public abstract bool IsFinished { get; }
    public abstract UnitMainModes MainMode { get; }
}
[JsonObject(MemberSerialization.OptIn)]
public class MoveCommand : Command {
    public Vector2 position;
    public override UnitMainModes MainMode => UnitMainModes.Moving;
    bool isFinsihed;
    public override bool IsFinished => isFinsihed;
    public MoveCommand(Vector2 destination){
        position = destination;
    }

    internal void SetFinished() {
        isFinsihed = true;
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class AttackCommand : Command {
    public override bool IsFinished => target.IsDestroyed;
    public override UnitMainModes MainMode => UnitMainModes.Attack;

    public ITargetable target;
    public AttackCommand(ITargetable target){
        this.target = target;
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class CaptureCommand : Command {
    public override bool IsFinished => target.Captured;
    public override UnitMainModes MainMode => UnitMainModes.Capture;
    public ICapturable target;
    public CaptureCommand(ICapturable target){
        this.target = target;
    }
}