using Andja.Model;

namespace Andja {

    public interface IWarfare {
        int PlayerNumber { get; }
        float CurrentDamage { get; }
        float MaximumDamage { get; }
        DamageType DamageType { get; }

        bool GiveAttackCommand(ITargetable warfare, bool overrideCurrent = false);

        void GoIdle();

        float GetCurrentDamage(ArmorType armorType);
    }
}