using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     単一の Phase（Phase1, Phase2, Phase3 等）の動作と攻撃一覧を宣言的に構築するビルダーです。
/// </summary>
/// <param name="parent">親の BossPipelineBuilder。</param>
/// <param name="phase">対象の Phase。</param>
public sealed class PhasePipelineBuilder(BossPipelineBuilder parent, Phase phase) {
    private readonly List<AttackPipelineBuilder> _attackBuilders = [];
    private readonly BossPipelineBuilder _parent = parent;

    /// <summary>対象の Phase を取得します。</summary>
    public Phase CurrentPhase { get; } = phase;

    /// <summary>
    ///     この Phase で定義された全攻撃ビルダーを取得します。
    /// </summary>
    internal IReadOnlyList<AttackPipelineBuilder> AttackBuilders => _attackBuilders;

    /// <summary>
    ///     待機時間を取得します。
    /// </summary>
    internal float IdleDuration { get; private set; } = 0.8f;

    /// <summary>
    ///     巡回時間を取得します。
    /// </summary>
    internal float PatrolDuration { get; private set; } = 2.0f;

    /// <summary>
    ///     移行演出時間を取得します。
    /// </summary>
    internal float TransitionDuration { get; private set; } = 1.0f;

    /// <summary>
    ///     機動パラメータ設定を取得します。
    /// </summary>
    internal PhaseLocomotionSettings LocomotionSettings { get; private set; } = new(580.0f);

    /// <summary>
    ///     Phase の基礎時間パラメータを設定します。
    /// </summary>
    /// <param name="idleDuration">待機時間（秒）。</param>
    /// <param name="patrolDuration">巡回時間（秒）。</param>
    /// <param name="transitionDuration">フェーズ移行演出時間（秒）。</param>
    /// <returns>現在の Phase ビルダー。</returns>
    public PhasePipelineBuilder Timing(float idleDuration = 0.8f, float patrolDuration = 2.0f,
        float transitionDuration = 1.0f)
    {
        IdleDuration = idleDuration;
        PatrolDuration = patrolDuration;
        TransitionDuration = transitionDuration;
        return this;
    }

    /// <summary>
    ///     Phase の足場間跳躍・落下・着地機動パラメータを設定します。
    /// </summary>
    /// <param name="jumpForce">跳躍初速（px/s）。</param>
    /// <param name="gravity">適用される重力加速度（px/s^2）。</param>
    /// <param name="maxFallSpeed">最大落下終端速度（px/s）。</param>
    /// <param name="maxAscentDuration">跳躍上昇の最大許容時間（秒）。</param>
    /// <param name="landingRecoveryDuration">着地後の硬直待機時間（秒）。</param>
    /// <param name="takeoffDistance">足場間跳躍への移行を開始する水平距離閾値（px）。</param>
    /// <returns>現在の Phase ビルダー。</returns>
    public PhasePipelineBuilder PlatformJump(
        float jumpForce,
        float gravity = 980.0f,
        float maxFallSpeed = 850.0f,
        float maxAscentDuration = 0.80f,
        float landingRecoveryDuration = 0.10f,
        float takeoffDistance = 50.0f)
    {
        LocomotionSettings = new PhaseLocomotionSettings(
            jumpForce,
            gravity,
            maxFallSpeed,
            maxAscentDuration,
            landingRecoveryDuration,
            takeoffDistance);
        return this;
    }

    /// <summary>
    ///     この Phase に新しい攻撃パターンを追加します。
    /// </summary>
    /// <param name="skill">技能識別子。</param>
    /// <returns>新しい攻撃パイプラインビルダー。</returns>
    public AttackPipelineBuilder Attack(AbilityId skill)
    {
        AttackPipelineBuilder builder = new(this, skill);
        _attackBuilders.Add(builder);
        return builder;
    }


    /// <summary>
    ///     次の Phase の定義に進みます。
    /// </summary>
    /// <param name="phase">次に定義する Phase。</param>
    /// <returns>新しい Phase ビルダー。</returns>
    public PhasePipelineBuilder Phase(Phase phase)
    {
        return _parent.Phase(phase);
    }

    /// <summary>
    ///     全体のパイプラインをビルドして終了します。
    /// </summary>
    /// <returns>構築された BossBehaviorPipeline。</returns>
    public BossBehaviorPipeline Build()
    {
        return _parent.Build();
    }
}