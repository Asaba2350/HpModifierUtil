using HarmonyLib;
using System;
using System.Reflection;

namespace HpModifierUtil
{
    public class PatchClass
    {
        [HarmonyPatch]
        public static class HpSetterPatch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(BattleUnitModel), "set_hp", [typeof(float)]);
            }

            public static void Prefix(BattleUnitModel __instance, ref float value)
            {
                int current = (int)__instance.hp;
                int target = (int)value;

                int delta = target - current;

                if (delta == 0)
                    return;

                // 最大体力超過状態から最大体力ちょうどへの切り詰め
                // (RecoverHP 内や、他modの最大体力減少時の処理)はダメージ扱いにしない
                if (delta < 0 && current > __instance.MaxHp && target == __instance.MaxHp)
                    return;

                if (delta > 0)
                {
                    // TakeDamage で一旦減らしてから戻すケース用のガード
                    if (__instance.IsImmortal() && __instance.hp <= 1f)
                    {
                        value = 1f;
                        return;
                    }

                    int minHp = __instance.GetMinHp();
                    if (__instance.hp < (float)minHp)
                    {
                        value = (float)minHp;
                        return;
                    }

                    float fixedValue = HpModifierManager.GetHpIncreaseFixed(__instance)
                                     + HpModifierManager.GetHpChangeFixed(__instance);

                    float additiveRate = HpModifierManager.GetHpIncreaseRate(__instance)
                                       + HpModifierManager.GetHpChangeRate(__instance);

                    int modifiedDelta = (int)((delta + fixedValue) * (1f + additiveRate / 100f));
                    modifiedDelta = Math.Max(0, modifiedDelta);

                    // 補正後の回復量を反映
                    value = current + modifiedDelta;
                }
                else
                {
                    HpModifierManager.GetHpDecreaseRate(__instance, out float decreaseAdditive, out float decreaseMultiplier);

                    float fixedValue = -(HpModifierManager.GetFixed(__instance)
                                       + HpModifierManager.GetHpDecreaseFixed(__instance)
                                       + HpModifierManager.GetHpChangeFixed(__instance));

                    float additiveRate = HpModifierManager.GetRateAdditive(__instance) + decreaseAdditive;
                    float multiplierRate = HpModifierManager.GetRateMultiplier(__instance) * decreaseMultiplier;

                    int modifiedDelta = (int)((delta + fixedValue) * (1f + additiveRate / 100f) * multiplierRate);
                    modifiedDelta = Math.Min(0, modifiedDelta);

                    value = current + modifiedDelta;
                }
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.TakeDamage))]
        public static class TakeDamagePatch
        {
            public static void Prefix(BattleUnitModel __instance, BattleUnitModel attacker)
            {
                // attacker が null の場合は Set 側で何もしない
                HpModifierManager.Set(__instance, attacker);
            }

            public static Exception Finalizer(BattleUnitModel __instance, BattleUnitModel attacker, Exception __exception)
            {
                if (attacker != null)
                    HpModifierManager.Reset(__instance);

                return __exception;
            }
        }
    }
}