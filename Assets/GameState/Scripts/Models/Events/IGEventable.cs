using UnityEngine;
using System;

public abstract class IGEventable {

    protected Action<GameEvent> cbEventCreated;
    protected Action<GameEvent> cbEventEnded;
    protected int TargetID = -1;


    public void RegisterOnEvent(Action<GameEvent> create, Action<GameEvent> ending) {

    }
    public virtual int GetPlayerNumber() {
        return -1;
    }
    //	bool IsTarget (IGEventable target);
    /// <summary>
    /// Gets the type of the target.
    /// 1 = Player
    /// 10 = World
    /// 11 = island
    /// 12 = city
    /// 100 + StructureID = Specific Structure
    /// Gets only the generic Type.
    /// </summary>
    /// <returns>The target type.</returns>
    public int GetTargetType() {
        return TargetID;
    }
    public abstract void OnEventCreate(GameEvent ge);
    public abstract void OnEventEnded(GameEvent ge);


    public void AddEffects(Effect[] effects) {
        foreach (Effect effect in effects)
            AddEffect(effect);
    }

    public virtual void AddEffect(Effect effect) {
        Debug.Log("No implementation for effect " + effect.ID);
    }
}
