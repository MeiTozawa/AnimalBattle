namespace AnimalBattle.Abilities.Skills;

/// <summary>
///     TOML 設定に基づいて動的にタグを付与・回収するフェーズ遷移専用能力。
///     実行中は設定された ActivationOwnedTags：Invincible / Phasingを自動保持し、
///     終了時に GAS によって対称的に安全解除されます。
/// </summary>
public sealed class PhaseTransitionAbility : GameplayAbility {
    private float _timer;
    private float _targetDuration;
    private Action? _onCompleted;

    /// <summary>
    ///     TOML 設定オブジェクトを注入してフェーズ遷移能力を初期化します。
    /// </summary>
    public PhaseTransitionAbility(PhaseTransitionAbilityConfig config)
    {
        AbilityTag = new StringId("PhaseTransition");
        DisplayName = "Phase Transition";
        ActivationOwnedTags = config.ParsedOwnedTags;
        BlockedTags = config.ParsedBlockedTags;
        _targetDuration = config.Duration;
    }

    /// <summary>
    ///     個別フェーズのタイムライン時間および完了コールバックを構成します。
    /// </summary>
    public void Configure(float duration, Action? onCompleted = null)
    {
        _targetDuration = duration > 0.0f ? duration : _targetDuration;
        _onCompleted = onCompleted;
    }

    /// <inheritdoc />
    public override void Activate(AbilitySystemComponent asc, in AbilityActivationContext ctx)
    {
        _timer = 0.0f;
    }

    /// <inheritdoc />
    public override void Update(float dt, AbilitySystemComponent asc, bool isGrounded = false)
    {
        _timer += dt;
        if (_timer >= _targetDuration)
        {
            asc.EndCurrentAbility();
        }
    }

    /// <inheritdoc />
    public override void EndAbility(AbilitySystemComponent asc)
    {
        _timer = 0.0f;
        _onCompleted?.Invoke();
    }

    /// <inheritdoc />
    public override void CancelAbility(AbilitySystemComponent asc)
    {
        _timer = 0.0f;
    }
}
