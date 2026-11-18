using System;
using System.Collections.Generic;

namespace HpModifierUtil
{
    public static class HpModifierManager
    {
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

        /// <summary>
        /// TakeDamage 中だけ保持する、攻撃者側の補正値のスナップショット。
        /// </summary>
        private readonly struct AttackSnapshot(float fixedValue, float rateAdditive, float rateMultiplier)
        {
            public readonly float Fixed = fixedValue;
            public readonly float RateAdditive = rateAdditive;
            public readonly float RateMultiplier = rateMultiplier;
        }

        private static readonly Dictionary<BattleUnitModel, AttackSnapshot> AttackSnapshots = [];

        #region 攻撃者補正の一時保管

        public static void Set(BattleUnitModel unit, BattleUnitModel attacker)
        {
            if (attacker == null)
                return;

            GetHpAttackRate(attacker, out float additive, out float multiplier);
            AttackSnapshots[unit] = new AttackSnapshot(GetHpAttackFixed(attacker), additive, multiplier);
        }

        public static void Reset(BattleUnitModel unit)
        {
            AttackSnapshots.Remove(unit);
        }

        public static float GetFixed(BattleUnitModel unit)
        {
            return AttackSnapshots.TryGetValue(unit, out AttackSnapshot s) ? s.Fixed : 0f;
        }

        public static float GetRateAdditive(BattleUnitModel unit)
        {
            return AttackSnapshots.TryGetValue(unit, out AttackSnapshot s) ? s.RateAdditive : 0f;
        }

        public static float GetRateMultiplier(BattleUnitModel unit)
        {
            // 未登録時は「補正なし」= 1 を返す(0 を返すとダメージが全て無効化される)
            return AttackSnapshots.TryGetValue(unit, out AttackSnapshot s) ? s.RateMultiplier : 1f;
        }

        #endregion

        #region 共通ヘルパー

        /// <summary>
        /// 有効なパッシブと、破棄されていない有効なバフのうち T を実装しているものを列挙する。
        /// </summary>
        private static IEnumerable<T> GetModifiers<T>(BattleUnitModel unit) where T : class
        {
            var passives = unit.passiveDetail?.PassiveList;
            if (passives != null)
            {
                foreach (PassiveAbilityBase passive in passives)
                {
                    if (passive.isActiavted && passive is T modifier)
                        yield return modifier;
                }
            }

            var bufs = unit.bufListDetail?.GetActivatedBufList();
            if (bufs != null)
            {
                foreach (BattleUnitBuf buf in bufs)
                {
                    if (!buf.IsDestroyed() && buf is T modifier)
                        yield return modifier;
                }
            }
        }

        /// <summary>
        /// 0以上は加算側、負は乗算側(1 + rate/100)に積む。
        /// 乗算側は 0 未満にならないようにする。
        /// </summary>
        private static void AddRate(float rate, ref float additive, ref float multiplier)
        {
            if (rate >= 0f)
                additive += rate;
            else
                multiplier *= Math.Max(0f, 1f + rate / 100f);
        }

        #endregion

        #region 回復

        public static float GetHpIncreaseFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpIncreaseModifier m in GetModifiers<IHpIncreaseModifier>(unit))
                result += m.IncreaseFixed;
            return result;
        }

        public static float GetHpIncreaseRate(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpIncreaseModifier m in GetModifiers<IHpIncreaseModifier>(unit))
                result += m.IncreaseRate;
            return result;
        }

        #endregion

        #region ダメージ

        public static float GetHpDecreaseFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpDecreaseModifier m in GetModifiers<IHpDecreaseModifier>(unit))
                result += m.DecreaseFixed;
            return result;
        }

        /// <summary>
        /// Decrease + Change の率を、加算分と乗算分に分けて一度の走査で取得する。
        /// </summary>
        public static void GetHpDecreaseRate(BattleUnitModel unit, out float additive, out float multiplier)
        {
            additive = 0f;
            multiplier = 1f;

            foreach (IHpDecreaseModifier m in GetModifiers<IHpDecreaseModifier>(unit))
                AddRate(m.DecreaseRate, ref additive, ref multiplier);

            foreach (IHpChangeModifier m in GetModifiers<IHpChangeModifier>(unit))
                AddRate(m.ChangeRate, ref additive, ref multiplier);
        }

        public static float GetHpDecreaseRateAdditive(BattleUnitModel unit)
        {
            GetHpDecreaseRate(unit, out float additive, out _);
            return additive;
        }

        public static float GetHpDecreaseRateMultiplier(BattleUnitModel unit)
        {
            GetHpDecreaseRate(unit, out _, out float multiplier);
            return multiplier;
        }

        #endregion

        #region 増減両方

        public static float GetHpChangeFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpChangeModifier m in GetModifiers<IHpChangeModifier>(unit))
                result += m.ChangeFixed;
            return result;
        }

        public static float GetHpChangeRate(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpChangeModifier m in GetModifiers<IHpChangeModifier>(unit))
                result += m.ChangeRate;
            return result;
        }

        #endregion

        #region 攻撃側

        public static float GetHpAttackFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (IHpAttackModifier m in GetModifiers<IHpAttackModifier>(unit))
                result += m.AttackDecreaseFixed;
            return result;
        }

        public static void GetHpAttackRate(BattleUnitModel unit, out float additive, out float multiplier)
        {
            additive = 0f;
            multiplier = 1f;

            foreach (IHpAttackModifier m in GetModifiers<IHpAttackModifier>(unit))
                AddRate(m.AttackDecreaseRate, ref additive, ref multiplier);
        }

        public static float GetHpAttackRateAdditive(BattleUnitModel unit)
        {
            GetHpAttackRate(unit, out float additive, out _);
            return additive;
        }

        public static float GetHpAttackRateMultiplier(BattleUnitModel unit)
        {
            GetHpAttackRate(unit, out _, out float multiplier);
            return multiplier;
        }

        #endregion
    }
}
