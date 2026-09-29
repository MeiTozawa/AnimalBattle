using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 State_Module 間で共有する Entity 単位の実行値です。</summary>
public sealed class Boss2BehaviorState : IBossBehaviorState {
    private BossPhaseTransitionData _phaseTransitionData;
    private BossTerminalData _terminalData;

    /// <summary>Boss2 の位置を指定して共有値を初期化します。</summary>
    public Boss2BehaviorState(float startX = 900.0f, float startY = 480.0f)
    {
        CurrentPosition = new Vec2f(startX, startY);
        AttackStartPosition = CurrentPosition;
        AttackTargetPosition = CurrentPosition;
        CurrentSkill = BossSkill.None;
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
        IsPlayerAlive = true;
    }

    /// <summary>現在位置です。</summary>
    public Vec2f CurrentPosition { get; set; }

    /// <summary>現在速度です。</summary>
    public Vec2f CurrentVelocity { get; set; }

    /// <summary>現在状態の経過時間です。</summary>
    public float StateElapsed { get; set; }

    /// <summary>攻撃中のサブタイマーです。</summary>
    public float AttackSubTimer { get; set; }

    /// <summary>現在攻撃の順序 index です。</summary>
    public int CurrentAttackIndex { get; set; }

    /// <summary>攻撃開始位置です。</summary>
    public Vec2f AttackStartPosition { get; set; }

    /// <summary>攻撃開始時の対象位置です。</summary>
    public Vec2f AttackTargetPosition { get; set; }

    /// <summary>攻撃対象 Entity です。</summary>
    public ulong AttackTargetEntityId { get; set; }

    /// <summary>Boss が接地しているかどうかです。</summary>
    public bool IsGrounded { get; set; }

    /// <summary>プレイヤーが生存しているかどうかです。</summary>
    public bool IsPlayerAlive { get; set; }

    /// <summary>Phase 遷移中の無敵状態です。</summary>
    public bool IsInvulnerable { get; set; }

    /// <summary>Dead が確定しているかどうかです。</summary>
    public bool IsDead { get; set; }

    /// <summary>攻撃が開始済みかどうかです。</summary>
    public bool IsAttackActive { get; set; }

    /// <summary>PhaseTransition の項目別適用 data です。</summary>
    public ref BossPhaseTransitionData PhaseTransitionData => ref _phaseTransitionData;

    /// <summary>Dead 状態で保持する終端 data です。</summary>
    public ref BossTerminalData TerminalData => ref _terminalData;

    /// <summary>直近の PhaseTransition target を取得します。</summary>
    public Phase PhaseTransitionTargetPhase { get; set; }

    /// <summary>跳躍のクールダウンタイマーです。</summary>
    public float JumpCooldown { get; set; }

    /// <summary>技能ごとの残り冷却時間を管理する追跡器を取得します。</summary>
    public BossCooldownTracker CooldownTracker { get; } = new();

    /// <summary>次に要求する State_Id です。</summary>
    public BossState PendingBossState { get; private set; }

    /// <summary>次の状態へ渡す遷移理由です。</summary>
    public TransitionReason PendingTransitionReason { get; private set; }

    /// <summary>現在技能識別子です。</summary>
    public BossSkill CurrentSkill { get; set; }

    /// <summary>右向きかどうかです。</summary>
    public bool FacingRight { get; set; }


    /// <inheritdoc />
    public void SynchronizePosition(float x, float y)
    {
        CurrentPosition = new Vec2f(x, y);
    }

    /// <inheritdoc />
    public void SynchronizeState(float x, float y, float vx, float vy, bool isGrounded, bool isPlayerAlive)
    {
        CurrentPosition = new Vec2f(x, y);
        CurrentVelocity = new Vec2f(vx, vy);
        IsGrounded = isGrounded;
        IsPlayerAlive = isPlayerAlive;
    }

    /// <summary>保持済みの遷移要求を読み取り、要求を消費します。</summary>
    public bool ConsumeTransition(out BossState stateId, out TransitionReason reason)
    {
        stateId = PendingBossState;
        reason = PendingTransitionReason;
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
        return stateId.IsValid();
    }

    /// <summary>Boss2 の共有実行値を初期スポーン状態にリセットします。</summary>
    /// <param name="startX">初期 X 座標。</param>
    /// <param name="startY">初期 Y 座標。</param>
    public void Reset(float startX, float startY)
    {
        CurrentPosition = new Vec2f(startX, startY);
        CurrentVelocity = Vec2f.Zero;
        StateElapsed = 0.0f;
        AttackSubTimer = 0.0f;
        CurrentAttackIndex = 0;
        CurrentSkill = BossSkill.None;
        AttackStartPosition = CurrentPosition;
        AttackTargetPosition = CurrentPosition;
        AttackTargetEntityId = 0;
        IsGrounded = false;
        IsPlayerAlive = true;
        FacingRight = false;
        IsInvulnerable = false;
        IsDead = false;
        IsAttackActive = false;
        _phaseTransitionData = default;
        _terminalData.Reset();
        PhaseTransitionTargetPhase = Phase.Phase1;
        JumpCooldown = 0.0f;
        CooldownTracker.Reset();
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
    }

    /// <summary>次の遷移要求を一件だけ保持します。</summary>
    public void RequestTransition(BossState stateId, in TransitionReason reason)
    {
        if (!stateId.IsValid() || reason.Kind == ReasonKind.Unknown) return;

        PendingBossState = stateId;
        PendingTransitionReason = reason;
    }

    /// <summary>現在状態の経過時間を初期化します。</summary>
    public void ResetStateTimer()
    {
        StateElapsed = 0.0f;
    }
}