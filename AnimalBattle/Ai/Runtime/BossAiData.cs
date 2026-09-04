using System.Runtime.InteropServices;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>Boss AI の共通実行状態を保持するデータ構造体です。</summary>
/// <remarks>共通 Boss AI データを生成します。</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct BossAiData(BossType bossType, BossState stateId, Phase phase)
{
    /// <summary>Boss 種別です。</summary>
    public BossType BossType = bossType;

    /// <summary>現在の安定した State_Id です。</summary>
    public BossState BossState = stateId;

    /// <summary>現在の親 Phase です。</summary>
    public Phase Phase = phase;

    /// <summary>現在状態の経過時間です。</summary>
    public float StateTimer = 0.0f;

    /// <summary>現在状態の設定上の継続時間です。</summary>
    public float StateDuration = 0.0f;

    /// <summary>最後に処理した Frame_Update 番号です。</summary>
    public long FrameUpdateNumber = 0;

    /// <summary>Phase 遷移などで無敵かどうかです。</summary>
    public bool IsInvulnerable = false;

    /// <summary>Boss が死亡終端状態かどうかです。</summary>
    public bool IsDead = false;

    /// <summary>新しい攻撃生成と攻撃判定を許可するかどうかです。</summary>
    public bool AttackGenerationEnabled = true;

    /// <summary>値を書き込んだ実行基盤です。</summary>
    public UpdateSource UpdateSource = UpdateSource.NewRuntime;
}
