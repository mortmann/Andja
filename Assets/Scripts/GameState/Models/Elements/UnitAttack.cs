using System.Collections.Generic;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class UnitAttack : Attack {
        private Unit Unit => (Unit)Parent;
        protected override AttackCommand AttackCommand => Unit.CurrentCommand as AttackCommand;

        public UnitAttack(BaseThing baseThing) : base(baseThing) {
        }
        
        public bool CanAttackNowOrReach(Target target) {
            return CanAttack(target) || Unit.CanReach(Unit.ClosestTargetPosition(target.CurrentPosition));
        }

        public override void OnUpdate(float deltaTime) {
            if (Unit.CurrentMainMode != UnitMainModes.Attack)
                return;
            base.OnUpdate(deltaTime);
        }

        protected override void StopAttack() {
            Unit.GoIdle();
        }
        public void UpdateAttack() {
            if (HasAttack && Parent.IsInRange(CurrentTarget, AttackRange) == false) {
                if (Unit.CurrentDoingMode != UnitDoModes.Move) {
                    Unit.Pathfinding.cbIsAtDestination += OnArriveDestination;
                    Vector2 dest = CurrentTarget.CurrentPosition;
                    Unit.SetDestinationIfPossible(dest.x, dest.y);
                }
            }
            else if (Unit.CurrentDoingMode != UnitDoModes.Fight) {
                //is in range start fighting
                Unit.CurrentDoingMode = UnitDoModes.Fight;
            }
        }
        
        private void UpdateAggroing() {
            if (HasAttack == false || CurrentTarget == null) {
                Unit.CurrentMainMode = UnitMainModes.Idle;
                return;
            }

            //not in Range -> get in range
            if (Parent.IsInRange(CurrentTarget, AttackRange) == false) {
                if (Unit.CurrentDoingMode != UnitDoModes.Move) {
                    Vector2 dest = CurrentTarget.CurrentPosition;
                    if (Vector2.Distance(dest, CurrentPosition) < AttackRange + GameData.UnitAggroRange) {
                        Unit.SetDestinationIfPossible(dest.x, dest.y);
                    }
                }

                AggroCommand aggro = Unit.CurrentCommand as AggroCommand;
                if (Vector2.Distance(aggro.StartPosition, CurrentPosition) > GameData.UnitAggroRange) {
                    //Maybe just send it back to the startposition BUT not finish aggro -> if the other 
                    //is following it could get in range again and we could reaggro without the need to 
                    //go back to the startposition completly -> which requires this to 
                    // update aggro range & move at the sametime
                    aggro.SetFinished();
                    Unit.GiveMovementCommand(aggro.StartPosition);
                    Debug.Log("Finished AGGRO returning to start");
                }
            }
            else {
                //IN range go ahead fight
                if (Unit.CurrentDoingMode != UnitDoModes.Fight)
                    Unit.CurrentDoingMode = UnitDoModes.Fight;
            }
        }

        private void OnArriveDestination(bool atDest) {
            if (atDest == false) {
                return;
            }

            if (CurrentTarget != null)
                Unit.CurrentDoingMode = UnitDoModes.Fight;

            Unit.Pathfinding.cbIsAtDestination -= OnArriveDestination;
        }

    }
}