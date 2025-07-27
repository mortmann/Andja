using Andja.Model;
using UnityEngine;

public interface IBaseThing : IGEventable {
    string ID{ get; set; }
    bool IsActive { get; }
    float MaximumHealth { get; }
    int UpkeepCost { get; }
    bool IsDestroyed { get; }
    bool CanTakeDamage { get; }
    int BuildCost { get; }
    Item[] BuildingItems { get; }
    string SpriteName { get; }
    int PopulationLevel { get; }
    int PopulationCount { get; }
    bool IsStructure { get; }
    bool IsUnit { get; }
    Vector2 Position { get; }
    float CurrentHealth { get; set; }
    int PlayerNumber { get; }
    void OnBaseThingBuild(bool loading = false);
    void OnBuild(bool loading = false);
    void ReduceHealth(float damage, IAttack attack = null);
    void RepairHealth(float heal);
    void Update(float deltaTime);
    void ChangeHealth(float change);
    
    /// <summary>
    /// Destroys this immedietly and without any further checks. 
    /// </summary>
    /// <param name="destroyer"></param>
    /// <param name="onLoad"></param>
    /// <returns></returns>
    bool Destroy(IAttack destroyer = null, bool onLoad = false);

    bool IsInRange(ITarget target, float range);
    bool AddElement(Element element);
    void Load();
    T GetElement<T>() where T : Element;
    bool HasElement<T>() where T : Element;
    T GetElementData<T>() where T : ElementData;
}