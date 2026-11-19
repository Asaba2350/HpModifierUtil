# HpModifierUtil

Library of Ruina向けのHP変更処理を拡張するMod開発者向けユーティリティです。

## 概要

HpModifierUtilは、他のModから利用できるHP変更補正機能を提供します。

HPの増減量や最大HP、回復可能HPに対する補正を統一的に管理することを目的としています。

## 主な機能

- HP回復量の補正
- HP減少量の補正
- HP変更量の補正
- 攻撃者側からのHP減少量補正
- 最大HPの減少
- 回復可能HP上限の減少
- HPの直接設定

## 導入方法

1. HpModifierUtil.dllをLibrary of RuinaのMod環境に導入してください。
2. 他のModから利用する場合は、HpModifierUtil.dllを参照に追加してください。

## API

### HpModUtil

HP変更に関する機能を提供するクラスです。

| メソッド | 説明 |
|---|---|
| `DecreaseMaxHp(unit, amount)` | 最大HPを減少させる |
| `DecreaseRecoverableHp(unit, amount)` | 回復可能HP上限を減少させる |
| `ForceSetHp(unit, newHp)` | HPを直接設定する |

### 補正インターフェース

各インターフェースを実装することで、PassiveやBattleUnitBufからHP変更補正を提供できます。

| インターフェース | 用途 |
|---|---|
| `IMinMaxHp` | 最小最大HPの設定 |
| `IHpIncreaseModifier` | HP回復量の補正 |
| `IHpDecreaseModifier` | HP減少量の補正 |
| `IHpChangeModifier` | HP増減量の補正 |
| `IHpAttackModifier` | 攻撃者側からのHP減少量補正 |

## 注意事項

- `DecreaseMaxHp`は最大HPそのものを減少させます。
- `DecreaseRecoverableHp`は最大HPを基準として回復可能HP上限を減少させます。
- `ForceSetHp`は通常のHP変更処理やHarmonyパッチを経由せず、HPを直接設定します。
- 他ModによるHP変更・死亡処理への介入との競合が発生する可能性があります。

## ライセンス

ライセンスについては別途定めます。