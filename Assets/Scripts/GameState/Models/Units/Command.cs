using System;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Command {
        public virtual Vector2 Position => Vector2.zero;
        public abstract bool IsFinished { get; }
        public abstract UnitMainModes MainMode { get; }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MoveCommand : Command {
        public override UnitMainModes MainMode => UnitMainModes.Moving;
        [JsonPropertyAttribute] private bool isFinished;
        public override bool IsFinished => isFinished;
        public override Vector2 Position => _position;
        [JsonPropertyAttribute] private Vector2 _position;

        public MoveCommand(Vector2 destination) {
            _position = destination;
        }

        internal void SetFinished() {
            isFinished = true;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AttackCommand : Command {
        public override bool IsFinished => Target.IsDestroyed;
        public override UnitMainModes MainMode => UnitMainModes.Attack;
        public override Vector2 Position => Target.CurrentPosition;

        [JsonPropertyAttribute] public ITargetable Target;
        [JsonPropertyAttribute] public int playerNumber;

        public AttackCommand(ITargetable target) {
            this.Target = target;
        }

        public AttackCommand() {
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class AggroCommand : AttackCommand {
        public override bool IsFinished => Target.IsDestroyed || isDone;
        public override UnitMainModes MainMode => UnitMainModes.Aggroing;

        [JsonPropertyAttribute] public Vector2 StartPosition;
        [JsonPropertyAttribute] bool isDone;

        public AggroCommand(ITargetable target, Vector2 startPosition) {
            this.Target = target;
            StartPosition = startPosition;
        }

        public AggroCommand() {
        }

        internal void SetFinished() {
            isDone = true;
        }

    }
    [JsonObject(MemberSerialization.OptIn)]
    public class CaptureCommand : Command {
        public override bool IsFinished => Target.GetElement<Capturable>().Captured;
        public override UnitMainModes MainMode => UnitMainModes.Capture;
        [JsonPropertyAttribute] public Structure Target;
        public override Vector2 Position => Target.Center;

        public CaptureCommand(Structure target) {
            this.Target = target;
        }

        public CaptureCommand() {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PickUpCrateCommand : Command {
        public override bool IsFinished => crate.despawned;
        public override UnitMainModes MainMode => UnitMainModes.PickUpCrate;
        [JsonPropertyAttribute] public Crate crate;
        public override Vector2 Position => crate.position;

        public PickUpCrateCommand(Crate crate) {
            this.crate = crate;
        }

        public PickUpCrateCommand() {
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PatrolCommand : Command {
        public Action<PatrolCommand> cbRouteChange;

        //this only exists for it to be removed from the queue
        //so if its removed it is not finished anymore
        //not sure if it is needed
        private bool currentlyActive;

        public override bool IsFinished => currentlyActive;
        public override UnitMainModes MainMode => UnitMainModes.Patrol;
        public override Vector2 Position => Positions.Peek.Vec;
        public int PositionCount => Positions.Count;
        [JsonPropertyAttribute] public RotatingList<SeriaziableVector2> Positions { get; protected set; }

        public PatrolCommand(RotatingList<SeriaziableVector2> positions) {
            Positions = positions;
        }

        public PatrolCommand() {
            if (Positions == null)
                Positions = new RotatingList<SeriaziableVector2>();
        }

        public void AddPosition(Vector2 pos) {
            Positions.Add(pos);
            cbRouteChange?.Invoke(this);
        }

        public void RemovePosition(Vector2 pos) {
            Positions.Remove(pos);
            cbRouteChange?.Invoke(this);
        }

        internal void ClearPositions() {
            Positions.Clear();
            cbRouteChange?.Invoke(this);
        }

        internal void ChangeToNextPosition() {
            Positions.GoToNext();
        }

        public void RegisterOnRouteChange(Action<PatrolCommand> change) {
            cbRouteChange += change;
        }

        public void UnregisterOnRouteChange(Action<PatrolCommand> change) {
            cbRouteChange += change;
        }

        public void SetActive(bool active) {
            currentlyActive = active;
        }

        internal Vector2[] ToPositionArray() {
            return Positions.ConvertAllToArray(sv => sv.Vec);
        }
    }
}