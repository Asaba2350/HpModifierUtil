# HpModifierUtil

Library of Ruina Modding向けの体力変更処理ライブラリです。

体力の増減補正、最大体力の減少、回復可能体力上限の設定など、体力に関する機能を提供します。

主に他のModから利用されることを想定しています。

## Distribution

GitHub → [Repository URL](https://github.com/Asaba2350/HpModifierUtil)

Steam Workshop → [Workshop URL](https://steamcommunity.com/sharedfiles/filedetails/?id=3842620046)

## Includes Your MOD

HpModifierUtilを利用するModを作成する場合、以下の準備が必要です。

1. Modフォルダ内の `Assemblies/HermesAssemblies/` フォルダに `HpModifierUtil.dll` を配置してください。
   `HermesAssemblies` フォルダが存在しない場合は作成してください。
2. 配置した `HpModifierUtil.dll` を参照に追加してください。
3. Modを公開する際、前提Modとして `Device Of Hermes` を指定してください。

詳しくは [Device Of Hermes - HermesPreloader](https://tundraclimate.github.io/DeviceOfHermes/docs/hermes-preloader.html) のドキュメントを参照してください。

## Features

- 体力回復量の補正
- 体力減少量の補正
- 体力増減量の補正
- 攻撃者側からの体力減少量補正
- 最大体力の減少
- 回復可能体力上限の減少
- 体力の直接設定

## API Reference

以降では、HpModifierUtilが提供するクラス・インターフェースについて説明します。

# `using HpModifierUtil;`

名前空間 `HpModifierUtil` に存在するクラスです。

この記事はこれを前提に書かれています。

# `HpModUtil`

体力の増減や最大体力に関する処理を提供するクラスです。

```csharp
// 最大体力を20減少させる
HpModUtil.DecreaseMaxHp(unit, 20);

// 回復可能体力上限を20減少させる
HpModUtil.DecreaseRecoverableHp(unit, 20);

// 体力を50に設定する
HpModUtil.ForceSetHp(unit, 50);
```

## Specified usage

### `DecreaseMaxHp(BattleUnitModel unit, int amount)`

```csharp
HpModUtil.DecreaseMaxHp(unit, amount);
```

対象の最大体力を減少させます。

複数回適用した場合、それぞれ加算されます。

`IMinMaxHp`によって設定された最大体力の最低値を下回ることはありません。

* `unit`: 対象のユニットです。
* `amount`: 最大体力の減少量です。0以下の場合は何も行いません。

### `DecreaseRecoverableHp(BattleUnitModel unit, int amount)`

```csharp
HpModUtil.DecreaseRecoverableHp(unit, amount);
```

対象の回復可能体力上限を減少させます。

最大体力そのものは変更せず、最大体力を基準として回復可能体力上限を設定します。

回復可能体力上限は以下のように計算されます。

```csharp
Math.Max(unit.Book.DeadLine, unit.MaxHp - reduction)
```

複数回適用した場合、それぞれ加算されます。

* `unit`: 対象のユニットです。
* `amount`: 回復可能体力上限の減少量です。0以下の場合は何も行いません。

### `ForceSetHp(BattleUnitModel target, int newHp)`

```csharp
HpModUtil.ForceSetHp(target, newHp);
```

対象の体力を直接設定します。

通常の体力変更処理を経由せず、内部の体力フィールドを直接書き換えます。

そのため、通常の体力変更に適用される補正や、体力変更に伴う処理は実行されません。

* `target`: 対象のユニットです。
* `newHp`: 設定する体力です。

`target`が`null`の場合は何も行いません。

# Interfaces

各インターフェースを`PassiveAbilityBase`または`BattleUnitBuf`に実装することで、体力変更に関する補正を提供できます。

補正値は有効なパッシブと、有効なバフから取得されます。

## `IMinMaxHp`

最大体力の最低値を設定するためのインターフェースです。

```csharp
public class SamplePassive : PassiveAbilityBase, HpModUtil.IMinMaxHp
{
    public int? MinMaxHp => 150;
}
```

### `MinMaxHp`

```csharp
public int? MinMaxHp { get; }
```

最大体力の最低値です。

複数のパッシブやバフで設定されている場合、最も大きい値が適用されます。

`null`を返した場合、このインターフェースからは最低値を設定しません。

初期値は1です。

## `IHpIncreaseModifier`

体力回復量を補正するためのインターフェースです。

```csharp
public class SamplePassive : PassiveAbilityBase, HpModUtil.IHpIncreaseModifier
{
    // 回復量に固定値を加算
    public float IncreaseFixed => 5f;

    // 回復量を20%増加
    public float IncreaseRate => 20f;
}
```

### `IncreaseFixed`

```csharp
public float IncreaseFixed { get; }
```

体力回復量に加算する固定値です。

複数回適用した場合、それぞれ加算されます。

### `IncreaseRate`

```csharp
public float IncreaseRate { get; }
```

体力回復量に適用する割合補正です。

複数回適用した場合、それぞれ加算されます。

## `IHpDecreaseModifier`

体力減少量を補正するためのインターフェースです。

```csharp
public class SamplePassive : PassiveAbilityBase, HpModUtil.IHpDecreaseModifier
{
    // 体力減少量に固定値を加算
    public float DecreaseFixed => 5f;

    // 体力減少量を20%増加
    public float DecreaseRate => 20f;
}
```

### `DecreaseFixed`

```csharp
public float DecreaseFixed { get; }
```

体力減少量に加算する固定値です。

複数回適用した場合、それぞれ加算されます。

正の値を設定すると体力減少量が増加し、負の値を設定すると体力減少量が減少します。

### `DecreaseRate`

```csharp
public float DecreaseRate { get; }
```

体力減少量に適用する割合補正です。

0以上の値は加算されます。

負の値は乗算として適用されます。

## `IHpChangeModifier`

体力の増減両方を補正するためのインターフェースです。

```csharp
public class SamplePassive : PassiveAbilityBase, HpModUtil.IHpChangeModifier
{
    // 体力の増減量に固定値を加算
    public float ChangeFixed => 5f;

    // 体力の増減量を20%増加
    public float ChangeRate => 20f;
}
```

### `ChangeFixed`

```csharp
public float ChangeFixed { get; }
```

体力の増減量に加算する固定値です。

複数回適用した場合、それぞれ加算されます。

回復時には回復量に、減少時には減少量に適用されます。

### `ChangeRate`

```csharp
public float ChangeRate { get; }
```

体力の増減量に適用する割合補正です。

回復時には`IHpIncreaseModifier.IncreaseRate`と加算されます。

減少時には`IHpDecreaseModifier.DecreaseRate`と同様に、0以上の値は加算、負の値は乗算として適用されます。

## `IHpAttackModifier`

攻撃者側から対象の体力減少量を補正するためのインターフェースです。

```csharp
public class SamplePassive : PassiveAbilityBase, HpModUtil.IHpAttackModifier
{
    // 対象の体力減少量に固定値を加算
    public float AttackDecreaseFixed => 5f;

    // 対象の体力減少量を20%増加
    public float AttackDecreaseRate => 20f;
}
```

### `AttackDecreaseFixed`

```csharp
public float AttackDecreaseFixed { get; }
```

攻撃対象の体力減少量に加算する固定値です。

複数回適用した場合、それぞれ加算されます。

### `AttackDecreaseRate`

```csharp
public float AttackDecreaseRate { get; }
```

攻撃対象の体力減少量に適用する割合補正です。

0以上の値は加算されます。

負の値は乗算として適用されます。

このインターフェースは`TakeDamage`の攻撃者引数から取得されます。

攻撃者が`null`の場合、この補正は適用されません。

# Modifier calculation

体力の増減処理では、対象のユニットに適用されている補正値を集計して計算します。

攻撃者側の補正については、`IHpAttackModifier`から取得します。

## Recovery

体力回復時の計算式です。

```text
回復量 =
    (元の回復量 + IncreaseFixed + ChangeFixed)
    × (1 + (IncreaseRate + ChangeRate) / 100)
```

各補正値は、該当するインターフェースを実装した有効なパッシブとバフから取得されます。

計算結果は整数に変換されます。

補正によって回復量が負の値になる場合、回復量は0になります。

## Decrease

体力減少時の計算式です。

```text
体力減少量 =
    (元の体力減少量 + DecreaseFixed + ChangeFixed + AttackDecreaseFixed)
    × (1 + 加算率 / 100)
    × 乗算率
```

加算率と乗算率は以下のように計算されます。

### 加算率

`DecreaseRate`、`ChangeRate`、`AttackDecreaseRate`のうち、0以上の値を合計します。

### 乗算率

負の値はそれぞれ乗算として適用されます。

```text
乗算率 =
    (1 + rate1 / 100)
    × (1 + rate2 / 100)
    × ...
```

各乗算結果は0未満になりません。

例えば、-20%と-30%を適用した場合、乗算率は0.8 × 0.7 = 0.56になります。

計算結果は整数に変換されます。

補正によって体力減少量が負の値になる場合、体力減少量は0になります。

# Notes

- 補正値は、対象ユニットの有効なパッシブと有効なバフから取得されます。
- 体力の増減量は整数として計算されます。小数部分は四捨五入されません。
- `DecreaseMaxHp`は最大体力そのものを減少させます。
- `DecreaseRecoverableHp`は最大体力を基準に回復可能体力上限を減少させます。最大体力の取得には`BattleUnitModel.MaxHp`を使用します。
- 回復可能体力上限が`unit.Book.DeadLine`に達した場合、通常の`Die()`による死亡処理を試みます。同じユニットへの死亡処理の試行は1幕につき1回です。
- 他Modによって死亡処理が阻止された場合、その幕の間は再試行しません。
- `ForceSetHp`は通常の体力変更処理を経由しないため、使用時には注意してください。

# License

本プロジェクトは[MIT License](LICENSE.txt)の下で公開されています。