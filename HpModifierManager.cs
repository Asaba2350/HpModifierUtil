using System;
using System.Collections.Generic;

namespace HpModifierUtil
{
    internal static class HpModifierManager
    {


        /// <summary>
        /// TakeDamage 中だけ保持する、攻撃者側の補正値のスナップショット。
        /// </summary>
        private readonly struct AttackSnapshot(float fixedValue, float rateAdditive, float rateMultiplier)
        {
            public readonly float Fixed = fixedValue;
            public readonly float RateAdditive = rateAdditive;
            public readonly float RateMultiplier = rateMultiplier;
        }

        private static readonly Dictionary<BattleUnitModel, Stack<AttackSnapshot>> AttackSnapshots = [];

        #region 攻撃者補正の一時保管

        internal static void Set(BattleUnitModel unit, BattleUnitModel attacker)
        {
            if (attacker == null)
                return;

            GetHpAttackRate(attacker, out float additive, out float multiplier);

            var snapshot = new AttackSnapshot(
                GetHpAttackFixed(attacker),
                additive,
                multiplier
            );

            if (!AttackSnapshots.TryGetValue(unit, out Stack<AttackSnapshot> stack))
            {
                stack = new Stack<AttackSnapshot>();
                AttackSnapshots.Add(unit, stack);
            }

            stack.Push(snapshot);
        }

        internal static void Reset(BattleUnitModel unit)
        {
            if (!AttackSnapshots.TryGetValue(unit, out Stack<AttackSnapshot> stack))
                return;

            if (stack.Count > 0)
                stack.Pop();

            if (stack.Count == 0)
                AttackSnapshots.Remove(unit);
        }

        private static bool TryGetAttackSnapshot(BattleUnitModel unit, out AttackSnapshot snapshot)
        {
            if (AttackSnapshots.TryGetValue(unit, out Stack<AttackSnapshot> stack)
                && stack.Count > 0)
            {
                snapshot = stack.Peek();
                return true;
            }

            snapshot = default;
            return false;
        }

        internal static float GetFixed(BattleUnitModel unit)
        {
            return TryGetAttackSnapshot(unit, out AttackSnapshot snapshot)
                ? snapshot.Fixed
                : 0f;
        }

        internal static float GetRateAdditive(BattleUnitModel unit)
        {
            return TryGetAttackSnapshot(unit, out AttackSnapshot snapshot)
                ? snapshot.RateAdditive
                : 0f;
        }

        internal static float GetRateMultiplier(BattleUnitModel unit)
        {
            // 未登録時は「補正なし」= 1 を返す
            return TryGetAttackSnapshot(unit, out AttackSnapshot snapshot)
                ? snapshot.RateMultiplier
                : 1f;
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

        internal static float GetHpIncreaseFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpIncreaseModifier m in GetModifiers<HpModUtil.IHpIncreaseModifier>(unit))
                result += m.IncreaseFixed;
            return result;
        }

        internal static float GetHpIncreaseRate(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpIncreaseModifier m in GetModifiers<HpModUtil.IHpIncreaseModifier>(unit))
                result += m.IncreaseRate;
            return result;
        }

        #endregion

        #region ダメージ

        internal static float GetHpDecreaseFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpDecreaseModifier m in GetModifiers<HpModUtil.IHpDecreaseModifier>(unit))
                result += m.DecreaseFixed;
            return result;
        }

        /// <summary>
        /// Decrease + Change の率を、加算分と乗算分に分けて一度の走査で取得する。
        /// </summary>
        internal static void GetHpDecreaseRate(BattleUnitModel unit, out float additive, out float multiplier)
        {
            additive = 0f;
            multiplier = 1f;

            foreach (HpModUtil.IHpDecreaseModifier m in GetModifiers<HpModUtil.IHpDecreaseModifier>(unit))
                AddRate(m.DecreaseRate, ref additive, ref multiplier);

            foreach (HpModUtil.IHpChangeModifier m in GetModifiers<HpModUtil.IHpChangeModifier>(unit))
                AddRate(m.ChangeRate, ref additive, ref multiplier);
        }

        internal static float GetHpDecreaseRateAdditive(BattleUnitModel unit)
        {
            GetHpDecreaseRate(unit, out float additive, out _);
            return additive;
        }

        internal static float GetHpDecreaseRateMultiplier(BattleUnitModel unit)
        {
            GetHpDecreaseRate(unit, out _, out float multiplier);
            return multiplier;
        }

        #endregion

        #region 増減両方

        internal static float GetHpChangeFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpChangeModifier m in GetModifiers<HpModUtil.IHpChangeModifier>(unit))
                result += m.ChangeFixed;
            return result;
        }

        internal static float GetHpChangeRate(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpChangeModifier m in GetModifiers<HpModUtil.IHpChangeModifier>(unit))
                result += m.ChangeRate;
            return result;
        }

        #endregion

        #region 攻撃側

        internal static float GetHpAttackFixed(BattleUnitModel unit)
        {
            float result = 0f;
            foreach (HpModUtil.IHpAttackModifier m in GetModifiers<HpModUtil.IHpAttackModifier>(unit))
                result += m.AttackDecreaseFixed;
            return result;
        }

        internal static void GetHpAttackRate(BattleUnitModel unit, out float additive, out float multiplier)
        {
            additive = 0f;
            multiplier = 1f;

            foreach (HpModUtil.IHpAttackModifier m in GetModifiers<HpModUtil.IHpAttackModifier>(unit))
                AddRate(m.AttackDecreaseRate, ref additive, ref multiplier);
        }

        internal static float GetHpAttackRateAdditive(BattleUnitModel unit)
        {
            GetHpAttackRate(unit, out float additive, out _);
            return additive;
        }

        internal static float GetHpAttackRateMultiplier(BattleUnitModel unit)
        {
            GetHpAttackRate(unit, out _, out float multiplier);
            return multiplier;
        }

        #endregion
    }
}