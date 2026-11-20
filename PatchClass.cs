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

            public static void Postfix(BattleUnitModel __instance)
            {
                MaxHpModifierManager.UpdateHpMax(__instance);
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

        [HarmonyPatch(typeof(BattleUnitModel), "get_MaxHp")]
        public static class MaxHpPatch
        {
            public static void Postfix(
                BattleUnitModel __instance,
                ref int __result)
            {
                if (__instance == null)
                    return;

                // 最大体力を減少させる
                long reducedMaxHp =
                    (long)__result -
                    MaxHpModifierManager.GetMaxHpReduction(__instance);

                // 最低最大体力を適用する
                int min =
                    MaxHpModifierManager.GetMinMaxHp(__instance);

                __result = (int)Math.Max(
                    reducedMaxHp,
                    (long)min
                );
            }
        }

        // HP取得時に回復可能体力上限を適用
        [HarmonyPatch]
        public static class GetHpPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(BattleUnitModel),
                    "get_hp"
                );
            }

            public static void Prefix(BattleUnitModel __instance)
            {
                MaxHpModifierManager.UpdateHpMax(__instance);
            }
        }


        // 戦闘開始時に初期化
        [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
        public static class StartBattlePatch
        {
            public static void Prefix()
            {
                MaxHpModifierManager.Clear();
            }
        }

        // 戦闘終了時に初期化
        [HarmonyPatch(typeof(StageController), nameof(StageController.EndBattle))]
        public static class EndBattlePatch
        {
            public static void Postfix()
            {
                MaxHpModifierManager.Clear();
            }
        }

        [HarmonyPatch(typeof(StageController), "RoundStartPhase_System")]
        public static class RoundStartPhase_SystemPatch
        {
            public static void Prefix(StageController __instance)
            {
                MaxHpModifierManager.DieClear();
            }
        }

        [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.RecoverHP))]
        public static class RecoverHPPatch
        {
            public static void Prefix()
            {
                MaxHpModifierManager.updateLock++;
            }

            public static Exception Finalizer(Exception __exception)
            {
                MaxHpModifierManager.updateLock--;

                if (MaxHpModifierManager.updateLock == 0)
                {
                    foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList())
                    {
                        MaxHpModifierManager.UpdateHpMax(unit);
                    }
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(BattleUnitEmotionDetail), nameof(BattleUnitEmotionDetail.ApplyEmotionCard))]
        public static class ApplyEmotionCardPatch
        {
            public static void Prefix()
            {
                MaxHpModifierManager.updateLock++;
            }

            public static Exception Finalizer(Exception __exception)
            {
                MaxHpModifierManager.updateLock--;

                if (MaxHpModifierManager.updateLock == 0)
                {
                    foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList())
                    {
                        MaxHpModifierManager.UpdateHpMax(unit);
                    }
                }

                return __exception;
            }
        }

    }
}