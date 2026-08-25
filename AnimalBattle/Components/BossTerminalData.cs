using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>Dead 状態で HUD、衝突、勝利演出、再起動経路が参照する終端データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossTerminalData
{
    /// <summary>終端データが確定しているかどうかです。</summary>
    public bool IsTerminal;
    /// <summary>終端として確定した固定 BossState です。</summary>
    public BossState DeadState;
    /// <summary>死亡時の Phase です。</summary>
    public Phase Phase;
    /// <summary>死亡を発生させた完全な理由です。</summary>
    public TransitionReason DeathReason;
    /// <summary>死亡を確定した Frame_Update 番号です。</summary>
    public long FrameUpdateNumber;
    /// <summary>死亡時の攻撃識別子です。</summary>
    public AttackId AttackId;
    /// <summary>死亡時の攻撃段階です。</summary>
    public AttackStage AttackStage;
    /// <summary>死亡時の HP です。</summary>
    public float Hp;
    /// <summary>死亡 frame に移動停止が適用されたかどうかです。</summary>
    public bool MovementStopped;
    /// <summary>死亡 frame に攻撃生成停止が適用されたかどうかです。</summary>
    public bool AttackGenerationStopped;

    /// <summary>終端データを未確定値へ戻します。</summary>
    public void Reset()
    {
        this = default;
    }

    /// <summary>死亡理由と死亡 frame を一度だけ終端データへ保存します。</summary>
    public bool TryCapture(
        BossState deadState,
        Phase phase,
        in TransitionReason deathReason,
        long frameUpdateNumber,
        AttackId attackId,
        AttackStage attackStage,
        float hp)
    {
        if (IsTerminal)
        {
            return false;
        }

        IsTerminal = true;
        DeadState = deadState;
        Phase = phase;
        DeathReason = deathReason;
        FrameUpdateNumber = frameUpdateNumber;
        AttackId = attackId;
        AttackStage = attackStage;
        Hp = hp;
        MovementStopped = false;
        AttackGenerationStopped = true;
        return true;
    }
}
