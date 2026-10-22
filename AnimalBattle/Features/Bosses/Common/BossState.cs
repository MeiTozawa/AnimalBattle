using System.Runtime.InteropServices;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;

namespace AnimalBattle.Ai;

/// <summary>
///     Boss AI の状態を表す型安全な判別構造体です（BossType + 各 Boss 固有状態値）。
/// </summary>
/// <remarks>
///     メモリレイアウトは Blittable（2 bytes）であり、各 Boss の独立した状態列挙体（<see cref="Boss1State" />, <see cref="Boss2State" />）との
///     相互変換および暗黙変換を提供します。
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BossState(BossType BossType, byte RawValue) : IEquatable<BossState> {
    /// <summary>安全な未指定状態です。</summary>
    public static readonly BossState Unknown = default;

    #region Boss1 定義

    /// <summary>Boss1 の浮遊移動状態です。</summary>
    public static readonly BossState Boss1_Hover = From(Boss1State.Hover);

    /// <summary>Boss1 の攻撃選択状態です。</summary>
    public static readonly BossState Boss1_AttackSelection = From(Boss1State.AttackSelection);

    /// <summary>Boss1 の攻撃予兆（前揺れ）状態です（全技能共通）。</summary>
    public static readonly BossState Boss1_Telegraph = From(Boss1State.Telegraph);

    /// <summary>Boss1 の攻撃判定実行（弾幕・突進）状態です（全技能共通）。</summary>
    public static readonly BossState Boss1_Execute = From(Boss1State.Execute);

    /// <summary>Boss1 の攻撃後隙状態です（全技能共通）。</summary>
    public static readonly BossState Boss1_Recovery = From(Boss1State.Recovery);

    /// <summary>Boss1 の Phase 遷移状態です。</summary>
    public static readonly BossState Boss1_PhaseTransition = From(Boss1State.PhaseTransition);

    /// <summary>Boss1 の Dead 状態です。</summary>
    public static readonly BossState Boss1_Dead = From(Boss1State.Dead);

    #endregion

    #region Boss2 定義

    /// <summary>Boss2 の待機状態です。</summary>
    public static readonly BossState Boss2_Idle = From(Boss2State.Idle);

    /// <summary>Boss2 の足場追跡状態です。</summary>
    public static readonly BossState Boss2_PatrolChase = From(Boss2State.PatrolChase);

    /// <summary>Boss2 の跳躍開始位置への移動状態です。</summary>
    public static readonly BossState Boss2_MoveToTakeoff = From(Boss2State.MoveToTakeoff);

    /// <summary>Boss2 の足場間機動（跳躍・落下・着地）状態です。</summary>
    public static readonly BossState Boss2_PlatformTraverse = From(Boss2State.PlatformTraverse);

    /// <summary>Boss2 の攻撃予兆（前揺れ）状態です（全技能共通）。</summary>
    public static readonly BossState Boss2_Telegraph = From(Boss2State.Telegraph);

    /// <summary>Boss2 の攻撃判定実行状態です（全技能共通）。</summary>
    public static readonly BossState Boss2_Execute = From(Boss2State.Execute);

    /// <summary>Boss2 の気絶（スタン）状態です。</summary>
    public static readonly BossState Boss2_Stunned = From(Boss2State.Stunned);

    /// <summary>Boss2 の攻撃後隙状態です（全技能共通）。</summary>
    public static readonly BossState Boss2_Recovery = From(Boss2State.Recovery);

    /// <summary>Boss2 の Phase 遷移状態です。</summary>
    public static readonly BossState Boss2_PhaseTransition = From(Boss2State.PhaseTransition);

    /// <summary>Boss2 の Dead 状態です。</summary>
    public static readonly BossState Boss2_Dead = From(Boss2State.Dead);

    #endregion

    /// <summary>状態が有効（Unknown でなく値が 0 以外）かどうかを取得します。</summary>
    public bool IsValid => BossType != BossType.Unknown && RawValue != 0;

    /// <summary>この状態が予兆（Telegraph）状態かどうかを取得します。</summary>
    public bool IsTelegraph => (BossType == BossType.Boss1 && (Boss1State)RawValue == Boss1State.Telegraph)
     || (BossType == BossType.Boss2 && (Boss2State)RawValue == Boss2State.Telegraph);

    /// <summary>この状態が攻撃実行（Execute）状態かどうかを取得します。</summary>
    public bool IsExecute => (BossType == BossType.Boss1 && (Boss1State)RawValue == Boss1State.Execute)
     || (BossType == BossType.Boss2 && (Boss2State)RawValue == Boss2State.Execute);

    /// <summary>この状態が攻撃後隙（Recovery）状態かどうかを取得します。</summary>
    public bool IsRecovery => (BossType == BossType.Boss1 && (Boss1State)RawValue == Boss1State.Recovery)
     || (BossType == BossType.Boss2 && (Boss2State)RawValue == Boss2State.Recovery);

    /// <summary>この状態が Phase 遷移状態かどうかを取得します。</summary>
    public bool IsPhaseTransition => (BossType == BossType.Boss1 && (Boss1State)RawValue == Boss1State.PhaseTransition)
     || (BossType == BossType.Boss2 && (Boss2State)RawValue == Boss2State.PhaseTransition);

    /// <summary>この状態が死亡（Dead）状態かどうかを取得します。</summary>
    public bool IsDead => (BossType == BossType.Boss1 && (Boss1State)RawValue == Boss1State.Dead)
     || (BossType == BossType.Boss2 && (Boss2State)RawValue == Boss2State.Dead);

    /// <summary>Boss1 の固有状態から判別状態を生成します。</summary>
    public static BossState From(Boss1State state)
    {
        return new BossState(BossType.Boss1, (byte)state);
    }

    /// <summary>Boss2 の固有状態から判別状態を生成します。</summary>
    public static BossState From(Boss2State state)
    {
        return new BossState(BossType.Boss2, (byte)state);
    }

    /// <summary>Boss1State からの暗黙変換を定義します。</summary>
    public static implicit operator BossState(Boss1State state)
    {
        return From(state);
    }

    /// <summary>Boss2State からの暗黙変換を定義します。</summary>
    public static implicit operator BossState(Boss2State state)
    {
        return From(state);
    }

    /// <summary>Boss1State として解釈します。</summary>
    public Boss1State AsBoss1()
    {
        return BossType == BossType.Boss1 ? (Boss1State)RawValue : Boss1State.None;
    }

    /// <summary>Boss2State として解釈します。</summary>
    public Boss2State AsBoss2()
    {
        return BossType == BossType.Boss2 ? (Boss2State)RawValue : Boss2State.None;
    }

    /// <summary>32ビット符号なし整数（上位バイトに BossType、下位バイトに RawValue）へ明示的に変換します。</summary>
    public static explicit operator uint(BossState state)
    {
        return (uint)(((byte)state.BossType << 8) | state.RawValue);
    }

    /// <summary>64ビット符号付き整数へ明示的に変換します。</summary>
    public static explicit operator long(BossState state)
    {
        return (uint)state;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (!IsValid) return "Unknown";

        return BossType switch {
            BossType.Boss1 => $"Boss1_{AsBoss1()}",
            BossType.Boss2 => $"Boss2_{AsBoss2()}",
            _ => $"Unknown({BossType},{RawValue})"
        };
    }
}

/// <summary>固定 BossState の判定拡張を提供します。</summary>
public static class BossStateExtensions {
    /// <summary>Unknown ではない固定状態かどうかを取得します。</summary>
    public static bool IsValid(this BossState state)
    {
        return state.IsValid;
    }
}