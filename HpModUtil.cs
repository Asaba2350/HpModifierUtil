using HarmonyLib;

namespace HpModifierUtil
{
    //外部公開用
    public class HpModUtil
    {
        public interface IMinMaxHp
        {
            int? MinMaxHp { get; }
        }

        public interface IHpIncreaseModifier
        {
            float IncreaseFixed { get; }
            float IncreaseRate { get; }
        }

        public interface IHpDecreaseModifier
        {
            float DecreaseFixed { get; }
            float DecreaseRate { get; }
        }

        public interface IHpChangeModifier
        {
            float ChangeFixed { get; }
            float ChangeRate { get; }
        }

        public interface IHpAttackModifier
        {
            float AttackDecreaseFixed { get; }
            float AttackDecreaseRate { get; }
        }

        public static void DecreaseMaxHp(BattleUnitModel unit, int amount)
        {
            MaxHpModifierManager.DecreaseMaxHp(unit, amount);
        }

        public static void DecreaseRecoverableHp(BattleUnitModel unit, int amount)
        {
            MaxHpModifierManager.DecreaseRecoverableHp(unit, amount);
        }

        private static readonly AccessTools.FieldRef<BattleUnitModel, float> HpRef = AccessTools.FieldRefAccess<BattleUnitModel, float>("<hp>k__BackingField");

        public static void ForceSetHp(BattleUnitModel target, int newHp)
        {
            if (target == null)
                return;

            HpRef(target) = newHp;
        }
    }
}