using AnimalBattle.Abilities.Attack;

namespace AnimalBattle.Abilities.Skills;

/// <summary>
///     単一の技能実行タイムライン（前隙・判定・後隙）を一元管理する軽量ランナーです。
///     ヒープ割り当てを行わず、高精度な時系列コンポーネント発火と状態問い合わせを提供します。
/// </summary>
public sealed class SkillRunner {
    private float _facingDirection = 1.0f;
    private bool _hasLeftGround;
    private ulong _triggeredMask;

    /// <summary>現在実行中の攻撃定義を取得します。</summary>
    public AttackDefinition? Definition { get; private set; }

    /// <summary>技能実行がアクティブかどうかを取得します。</summary>
    public bool IsActive => CurrentPhase is SkillPhase.Telegraph or SkillPhase.Active or SkillPhase.Recovery;

    /// <summary>技能実行が完了したかどうかを取得します。</summary>
    public bool IsComplete => CurrentPhase == SkillPhase.Completed;

    /// <summary>現在の技能フェーズを取得します。</summary>
    public SkillPhase CurrentPhase { get; private set; } = SkillPhase.None;

    /// <summary>技能開始からの総経過時間（秒）を取得します。</summary>
    public float Elapsed { get; private set; }

    /// <summary>現在フェーズ内の経過時間（秒）を取得します。</summary>
    public float PhaseElapsed { get; private set; }

    /// <summary>現在フェーズの目標持続時間（秒）を取得します。</summary>
    public float PhaseDuration {
        get
        {
            if (Definition is null) return 0.0f;
            return CurrentPhase switch {
                SkillPhase.Telegraph => Definition.TelegraphDuration,
                SkillPhase.Active => Definition.ExecuteDuration,
                SkillPhase.Recovery => Definition.RecoveryDuration,
                _ => 0.0f
            };
        }
    }

    /// <summary>現在有効なアーマータグを取得します。</summary>
    public GameplayTag CurrentArmorTag =>
        CurrentPhase == SkillPhase.Active && Definition is not null ? Definition.ArmorTag : GameplayTag.None;

    /// <summary>現在有効な靭性閾値を取得します。</summary>
    public float PoiseThreshold =>
        CurrentPhase == SkillPhase.Active && Definition is not null ? Definition.PoiseThreshold : 0.0f;

    /// <summary>
    ///     指定した攻撃定義で技能実行を開始します。
    /// </summary>
    /// <param name="definition">攻撃定義。</param>
    /// <param name="facingDirection">キャラの向き（正: 右向き、負: 左向き）。</param>
    public void Start(AttackDefinition definition, float facingDirection = 1.0f)
    {
        Debug.Assert(definition is not null, "[SkillRunner 警告] null の攻撃定義で Start が呼び出されました。");
        Debug.Assert(definition.IsValid, $"[SkillRunner 警告] 攻撃定義が不正です: {definition.Skill}");

        Definition = definition;
        _facingDirection = facingDirection >= 0.0f ? 1.0f : -1.0f;
        _triggeredMask = 0UL;
        _hasLeftGround = false;
        Elapsed = 0.0f;
        PhaseElapsed = 0.0f;

        // Telegraph が 0 秒以下の場合は即座に Active へ移行
        CurrentPhase = definition.TelegraphDuration > 0.0f ? SkillPhase.Telegraph : SkillPhase.Active;
    }

    /// <summary>
    ///     技能実行状態を初期化します。
    /// </summary>
    public void Reset()
    {
        Definition = null;
        CurrentPhase = SkillPhase.None;
        Elapsed = 0.0f;
        PhaseElapsed = 0.0f;
        _triggeredMask = 0UL;
        _hasLeftGround = false;
    }

    /// <summary>
    ///     毎フレームの技能タイムラインを更新し、到達したアクションステップをコールバックへ通知します。
    /// </summary>
    /// <param name="dt">デルタ時間（秒）。</param>
    /// <param name="isGrounded">接地状態フラグ。</param>
    /// <param name="onTrigger">アクション発火時の通知コールバック（action, facingDir）。</param>
    public void Update(float dt, bool isGrounded = false, Action<AttackActionStep, float>? onTrigger = null)
    {
        if (Definition is null || CurrentPhase is SkillPhase.None or SkillPhase.Completed) return;

        Elapsed += dt;
        PhaseElapsed += dt;

        if (!isGrounded) _hasLeftGround = true;

        var def = Definition;

        // フェーズ遷移評価
        switch (CurrentPhase)
        {
            case SkillPhase.Telegraph:
                if (PhaseElapsed >= def.TelegraphDuration)
                {
                    CurrentPhase = SkillPhase.Active;
                    PhaseElapsed -= def.TelegraphDuration;
                }

            break;

            case SkillPhase.Active:
                // Active 中のアクション発火判定
                ProcessActiveActions(def, isGrounded, onTrigger);

                if (PhaseElapsed >= def.ExecuteDuration)
                {
                    CurrentPhase = SkillPhase.Recovery;
                    PhaseElapsed -= def.ExecuteDuration;
                }

            break;

            case SkillPhase.Recovery:
                if (PhaseElapsed >= def.RecoveryDuration)
                {
                    CurrentPhase = SkillPhase.Completed;
                    PhaseElapsed = 0.0f;
                }

            break;
        }
    }


    private void ProcessActiveActions(AttackDefinition def, bool isGrounded,
        Action<AttackActionStep, float>? onTrigger)
    {
        var count = Math.Min(def.ActionCount, 64);
        for (var i = 0; i < count; i++)
        {
            var mask = 1UL << i;
            if ((_triggeredMask & mask) != 0) continue;

            if (!def.TryGetAction(i, out var step)) continue;

            // 接地イベント駆動判定
            if (step.Kind == AttackActionKind.OnGroundedHitbox)
            {
                var minTime = MathF.Max(step.TriggerTime, 0.05f);
                if (_hasLeftGround && isGrounded && PhaseElapsed >= minTime)
                {
                    _triggeredMask |= mask;
                    onTrigger?.Invoke(step, _facingDirection);

                    // 接地判定成功時は即座に Active を終了して Recovery へ
                    CurrentPhase = SkillPhase.Recovery;
                    PhaseElapsed = 0.0f;
                    return;
                }

                continue;
            }

            // 時系列駆動判定（浮動小数点微小誤差を許容）
            if (PhaseElapsed + 0.0001f >= step.TriggerTime)
            {
                _triggeredMask |= mask;
                onTrigger?.Invoke(step, _facingDirection);
            }
        }
    }
}