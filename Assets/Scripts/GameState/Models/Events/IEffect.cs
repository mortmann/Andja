using Andja.Controller;

namespace Andja.Model {
    public interface IEffect {
        string ID { get; }
        EffectTypes AddType { get; }
        bool CanSpread { get; }
        float Change { get; }
        EffectClassification Classification { get; }
        string Description { get; }
        EffectPrototypeData EffectPrototypeData { get; }
        string HoverOver { get; }
        InfluenceRange InfluenceRange { get; }
        InfluenceTyp InfluenceTyp { get; }
        bool IsNegative { get; }
        bool IsSpecial { get; }
        bool IsUnique { get; }
        bool IsUpdateChange { get; }
        EffectModifier ModifierType { get; }
        string Name { get; }
        string NameOfVariable { get; }
        string OnMapSpriteName { get; }
        float SpreadProbability { get; }
        int SpreadTileRange { get; }
        TargetGroup Targets { get; }
        string UiSpriteName { get; }
        EffectUpdateChanges UpdateChange { get; }
        bool ShouldSerializeEffect();
        void Update(float deltaTime, IGEventable target);
    }
}