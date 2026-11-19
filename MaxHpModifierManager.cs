using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HpModifierUtil
{
    internal static class MaxHpModifierManager
    {
        // 最大体力減少量
        private static readonly Dictionary<BattleUnitModel, int>
            maxHpReduction = [];

        // 回復可能体力減少量
        private static readonly Dictionary<BattleUnitModel, int>
            recoverableHpReduction = [];

        // hpプロパティのバッキングフィールド
        private static readonly AccessTools.FieldRef<BattleUnitModel, float>
            HpRef = AccessTools.FieldRefAccess<BattleUnitModel, float>(
                "<hp>k__BackingField"
            );

        // 減少量を安全に加算
        private static void AddReduction(
            Dictionary<BattleUnitModel, int> dictionary,
            BattleUnitModel unit,
            int amount)
        {
            if (unit == null || amount <= 0)
                return;

            dictionary.TryGetValue(unit, out int current);

            long result = (long)current + amount;

            dictionary[unit] = (int)Math.Min(
                result,
                int.MaxValue
            );
        }

        // 最大体力減少量を取得
        internal static int GetMaxHpReduction(BattleUnitModel unit)
        {
            if (unit == null)
                return 0;

            return maxHpReduction.TryGetValue(unit, out int value)
                ? value
                : 0;
        }

        // 最大体力を減少させる
        internal static void DecreaseMaxHp(
            BattleUnitModel unit,
            int amount)
        {
            if (unit == null || amount <= 0)
                return;

            AddReduction(maxHpReduction, unit, amount);

            UpdateHpMax(unit);
        }

        // 回復可能体力減少量を取得
        internal static int GetRecoverableHpReduction(
            BattleUnitModel unit)
        {
            if (unit == null)
                return 0;

            return recoverableHpReduction.TryGetValue(unit, out int value)
                ? value
                : 0;
        }

        // 回復可能体力を減少させる
        internal static void DecreaseRecoverableHp(
            BattleUnitModel unit,
            int amount)
        {
            if (unit == null || amount <= 0)
                return;

            AddReduction(recoverableHpReduction, unit, amount);

            UpdateHpMax(unit);
        }

        // パッシブとバフから修正値を取得
        private static IEnumerable<T> GetModifiers<T>(
            BattleUnitModel unit) where T : class
        {
            if (unit == null)
                yield break;

            var passives = unit.passiveDetail?.PassiveList;

            if (passives != null)
            {
                foreach (PassiveAbilityBase passive in passives)
                {
                    if (passive.isActiavted &&
                        passive is T modifier)
                    {
                        yield return modifier;
                    }
                }
            }

            var bufs = unit.bufListDetail?.GetActivatedBufList();

            if (bufs != null)
            {
                foreach (BattleUnitBuf buf in bufs)
                {
                    if (!buf.IsDestroyed() &&
                        buf is T modifier)
                    {
                        yield return modifier;
                    }
                }
            }
        }

        // 最低最大体力を取得
        internal static int GetMinMaxHp(BattleUnitModel unit)
        {
            int result = 1;

            foreach (HpModUtil.IMinMaxHp provider in GetModifiers<HpModUtil.IMinMaxHp>(unit))
            {
                if (provider.MinMaxHp.HasValue)
                {
                    result = Math.Max(
                        result,
                        provider.MinMaxHp.Value
                    );
                }
            }

            return result;
        }

        // 回復可能体力上限を取得
        internal static int GetRecoverableHpMax(BattleUnitModel unit)
        {
            if (unit == null)
                return 0;

            long result =
                (long)unit.MaxHp - GetRecoverableHpReduction(unit);

            return (int)Math.Max(0L, result);
        }

        // 現在体力を回復可能体力上限以下にする
        private static readonly HashSet<BattleUnitModel> updatingUnits = [];

        internal static void UpdateHpMax(BattleUnitModel unit)
        {
            if (unit == null || !updatingUnits.Add(unit))
                return;

            try
            {
                int maxHp = GetRecoverableHpMax(unit);
                float currentHp = HpRef(unit);

                if (currentHp > maxHp)
                {
                    HpModUtil.ForceSetHp(unit, maxHp);
                }
            }
            finally
            {
                updatingUnits.Remove(unit);
            }
        }

        // 戦闘データを初期化
        internal static void Clear()
        {
            maxHpReduction.Clear();
            recoverableHpReduction.Clear();
        }
    }
}