
using System;
using System.Collections.Generic;

namespace HpModifierUtil
{
    public static class MaxHpModifierManager
    {
        private static readonly Dictionary<BattleUnitModel, int>
            maxHpReduction = [];

        // 外部Modが実装するインターフェイス
        public interface IMinMaxHp
        {
            int? MinMaxHp { get; }
        }

        // 最大体力減少量を取得
        public static int GetMaxHpReduction(BattleUnitModel unit)
        {
            if (unit == null)
                return 0;

            return maxHpReduction.TryGetValue(unit, out int value)
                ? value
                : 0;
        }

        // 最大体力を減少させる
        public static void DecreaseMaxHp(BattleUnitModel unit, int amount)
        {
            if (unit == null || amount <= 0)
                return;

            int current = GetMaxHpReduction(unit);

            // intのオーバーフローを防止
            maxHpReduction[unit] =
                amount > int.MaxValue - current
                    ? int.MaxValue
                    : current + amount;

            UpdateHpMax(unit);
        }

        // 回復可能体力上限を取得
        public static int GetRecoverableHpMax(BattleUnitModel unit)
        {
            if (unit == null)
                return 0;

            return Math.Max(
                0,
                unit.MaxHp - GetMaxHpReduction(unit)
            );
        }

        // 現在体力が上限を超えていないか確認
        public static void UpdateHpMax(BattleUnitModel unit)
        {
            if (unit == null)
                return;

            int maxHp = GetRecoverableHpMax(unit);

            if (unit.hp > maxHp)
                unit.SetHp(maxHp);
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
        public static int GetMinMaxHp(BattleUnitModel unit)
        {
            int result = 1;

            foreach (IMinMaxHp provider in GetModifiers<IMinMaxHp>(unit))
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

        // 戦闘データを初期化
        public static void Clear()
        {
            maxHpReduction.Clear();
        }
    }
}
