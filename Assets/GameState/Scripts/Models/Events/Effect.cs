using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class EffectPrototypeData : LanguageVariables {

    public string NameOfVariable; // what does it change
    public string Change; // how it changes the Variable?
    public int[] TargetIDs; // what it can target

}

public class Effect {

    public int ID;
    public IGEventable target;
    public InfluenceTyp InfluenceTyp { protected set; get; }
    public InfluenceRange InfluenceRange { protected set; get; }


    /// <summary>
    /// Object is the influencetyp that is of type of the targe
    /// </summary>
    public Action<GameEvent, bool, object> Function;
    public Effect(IGEventable t) {
        this.target = t;
    }

}