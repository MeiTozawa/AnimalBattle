using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 State_Module 間で共有する Entity 単位の実行値です。</summary>
public sealed class Boss1BehaviorState : IBossBehaviorState {
    private BossPhaseTransitionData _phaseTransitionData;
    private BossTerminalData _terminalData;

    /// <summary>Boss1 の基準位置を指定して実行値を初期化します。</summary>
    public Boss1BehaviorState(float baseX = 640.0f, float baseY = 200.0f)
    {
        BaseX = baseX;
        CurrentPosition = new Vec2f(baseX, baseY);
        AttackStartPosition = CurrentPosition;
        AttackTargetPosition = CurrentPosition;
        CurrentSkill = BossSkill.None;
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
    }

    /// <summary>浮遊基準 X 座標です。</summary>
    public float BaseX { get; set; } = 640.0f;

    /// <summary>現在の Boss 位置です。runtime adapter が frame 境界で同期します。</summary>
    public Vec2f CurrentPosition { get; set; }

    /// <summary>現在の Boss 速度です。</summary>
    public Vec2f CurrentVelocity { get; set; }

    /// <summary>浮遊の累積時間です。</summary>
    public float HoverTimer { get; set; }

    /// <summary>現在状態の経過時間です。</summary>
    public float StateElapsed { get; set; }

    /// <summary>現在攻撃の sub timer です。</summary>
    public float AttackSubTimer { get; set; }

    /// <summary>弾幕回転角です。</summary>
    public float AngleOffset { get; set; }

    /// <summary>現在攻撃の順序 index です。</summary>
    public int CurrentAttackIndex { get; set; }

    /// <summary>攻撃開始位置です。</summary>
    public Vec2f AttackStartPosition { get; set; }

    /// <summary>攻撃開始時に固定した対象位置です。</summary>
    public Vec2f AttackTargetPosition { get; set; }

    /// <summary>攻撃対象 Entity です。</summary>
    public ulong AttackTargetEntityId { get; set; }

    /// <summary>次に要求する State_Id です。</summary>
    public BossState PendingBossState { get; private set; }

    /// <summary>次の状態へ渡す遷移理由です。</summary>
    public TransitionReason PendingTransitionReason { get; private set; }

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

    /// <inheritdoc />
    public bool FacingRight { get; set; }

    /// <summary>現在選択されている技能です。</summary>
    public BossSkill CurrentSkill { get; set; }


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
    }

    /// <summary>保持済みの遷移要求を読み取り、次 frame 用に消費します。</summary>
    public bool ConsumeTransition(out BossState stateId, out TransitionReason reason)
    {
        stateId = PendingBossState;
        reason = PendingTransitionReason;
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
        return stateId.IsValid();
    }

    /// <summary>Boss1 の共有実行値を初期スポーン状態にリセットします。</summary>
    /// <param name="startX">初期 X 座標。</param>
    /// <param name="startY">初期 Y 座標。</param>
    public void Reset(float startX, float startY)
    {
        BaseX = startX;
        CurrentPosition = new Vec2f(startX, startY);
        CurrentVelocity = Vec2f.Zero;
        FacingRight = false;
        HoverTimer = 0.0f;
        StateElapsed = 0.0f;
        AttackSubTimer = 0.0f;
        AngleOffset = 0.0f;
        CurrentSkill = BossSkill.None;
        CurrentAttackIndex = 0;
        AttackStartPosition = CurrentPosition;
        AttackTargetPosition = CurrentPosition;
        AttackTargetEntityId = 0;
        PendingBossState = BossState.Unknown;
        PendingTransitionReason = TransitionReason.None;
        IsInvulnerable = false;
        IsDead = false;
        IsAttackActive = false;
        _phaseTransitionData = default;
        _terminalData.Reset();
    }

    /// <summary>次の遷移要求を一件だけ保持します。</summary>
    public void RequestTransition(BossState stateId, in TransitionReason reason)
    {
        if (!stateId.IsValid() || reason.Kind == ReasonKind.Unknown) return;

        PendingBossState = stateId;
        PendingTransitionReason = reason;
    }

    /// <summary>現在状態の経過値を初期化します。</summary>
    public void ResetStateTimer()
    {
        StateElapsed = 0.0f;
    }
}