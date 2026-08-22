using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>旧 Boss component へ投影する値だけを保持する純粋な互換 ECS データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossCompatibilityData
{
    /// <summary>旧 state enum の整数値です。</summary>
    public int LegacyStateValue;
    /// <summary>旧攻撃識別子の整数値です。</summary>
    public int LegacyAttackValue;
    /// <summary>旧 state timer です。</summary>
    public float LegacyStateTimer;
    /// <summary>旧 state duration です。</summary>
    public float LegacyStateDuration;
    /// <summary>旧無敵表現です。</summary>
    public bool LegacyIsInvulnerable;
    /// <summary>互換値の更新元です。</summary>
    public UpdateSource UpdateSource;
    /// <summary>互換値を作成した Frame_Update 番号です。</summary>
    public long FrameUpdateNumber;
}