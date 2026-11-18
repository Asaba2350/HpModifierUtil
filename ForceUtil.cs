using HarmonyLib;

namespace HpModifierUtil
{
    public class ForceUtil
    {
        private static readonly AccessTools.FieldRef<BattleUnitModel, float> HpRef = AccessTools.FieldRefAccess<BattleUnitModel, float>("<hp>k__BackingField");

        public static void ForceSetHp(BattleUnitModel target, int newHp)
        {
            if (target == null)
                return;

            HpRef(target) = newHp;
        }
    }
}