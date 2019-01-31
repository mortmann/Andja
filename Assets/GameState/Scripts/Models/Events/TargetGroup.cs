﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetGroup  {

    public HashSet<Target> Targets;
    public TargetGroup(params Target[] targets) {
        Targets = new HashSet<Target>();
        Targets.UnionWith(targets);
    }
    public TargetGroup(ICollection<Target> targets) {
        Targets = new HashSet<Target>();
        Targets.UnionWith(targets);
    }

    internal void AddTargets(TargetGroup target) {
        Targets.UnionWith(target.Targets);
    }

    public bool IsTargeted(IEnumerable<Target> beingTargeted) {
        return Targets.Overlaps(beingTargeted);
    }
    public bool IsTargeted(TargetGroup other) {
        return Targets.Overlaps(other.Targets);
    }

}
