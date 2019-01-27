using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ServiceTarget { All, Damageable, Military, Homes, Production, Service, NeedStructure, SpecificRange, City, None }

public class ServicePrototypeData : OutputPrototypData {
    public ServiceTarget targets = ServiceTarget.All;
    public string functionName;
    public Structure[] specificRange = null;
    public Effect[] effectsOnTargets;

}

public class ServiceStructure : OutputStructure {

    Action<Structure> workOnTarget;


    protected ServiceStructure(ServiceStructure s) {
        OutputCopyData(s);
    }

    public override Structure Clone() {
        return new ServiceStructure(this);
    }

    public override void OnBuild() {

    }

    public void ExtinguishStructurefire(Structure str) {

    }
    public void RepairStructure(Structure str) {

    }
    public void ImproveStructure(Structure str) {

    }
    public void RemoveIllness(Structure str) {

    }

    public void ImproveCity() {
        //Will run once on build and needs to be removed on destroy
    }
}
