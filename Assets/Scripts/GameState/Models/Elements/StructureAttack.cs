using System.Collections.Generic;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Andja.Model {
    public class StructureAttack : Attack {
        protected override AttackCommand AttackCommand => ((MilitaryStructure)Parent).AttackCommand;

        public StructureAttack(BaseThing baseThing) : base(baseThing) {
        }
        protected override void StopAttack() {
            ((MilitaryStructure)Parent).AttackCommand = null;
        }
    }
}