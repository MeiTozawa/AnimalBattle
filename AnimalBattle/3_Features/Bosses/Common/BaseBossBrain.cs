using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Skills;

using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai;

/// <summary>
///     Boss 意思決定ブレインの共通基底抽象クラス。
///     フラット有限状態マシンによる戦闘状態管理、
///     GASによるフェーズ遷移・状態異常管理、
///     および死亡制御を一元管理します。
/// </summary>
public abstract class BaseBossBrain : IBossBrain
{
    protected static readonly StringId StateCombat = "Combat";

    protected readonly AbilitySystemComponent _asc = new();
    protected readonly StateMachine _stateMachine = new(StateCombat);

    protected PhaseTransitionAbility _phaseTransitionAbility = null!;

    protected float _stateTimer;
    protected bool _isDead;

    protected Vec2f _currentPosition;
    protected Vec2f _targetPosition;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; protected set; } = Phase.Phase1;

    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    public AbilitySystemComponent AbilitySystem => _asc;

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>AI 目標地点座標を取得します。</summary>
    public Vec2f TargetPosition => _targetPosition;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => _targetPosition.X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => _targetPosition.Y;

    /// <summary>現在発動中の技能識別タグを取得します。</summary>
    public virtual StringId CurrentSkillTag => _asc.CurrentAbility?.AbilityTag ?? StringId.Empty;

    /// <summary>現在 AI 状態の経過時間（秒）を取得します。</summary>
    public virtual float StateTimer => _stateTimer;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _isDead;

    /// <summary>Boss が右向きであるかを取得します（派生クラスでオーバーライド可能）。</summary>
    public virtual bool FacingRight => true;

    /// <summary>弾幕波次発射イベント。</summary>
    public event Action<StringId, Vec2f, Vec2f>? OnEmitBulletWave;

    /// <summary>全弾幕消去イベント。</summary>
    public event Action? OnClearBullets;

    /// <summary>現在の Boss 統一マクロ状態を取得します。</summary>
    public abstract BossState CurrentState { get; }

    /// <summary>
    ///     弾幕発射イベントを発火します。
    /// </summary>
    protected void RaiseEmitBulletWave(StringId skill, Vec2f origin, Vec2f target)
    {
        OnEmitBulletWave?.Invoke(skill, origin, target);
    }

    /// <summary>
    ///     弾幕消去イベントを発火します。
    /// </summary>
    protected void RaiseClearBullets()
    {
        OnClearBullets?.Invoke();
    }

    /// <summary>
    ///     共通ブレイン基盤（GAS フェーズ遷移能力登録・イベント購読）を初期化します。
    /// </summary>
    protected void InitializeBaseBrain(PhaseTransitionAbilityConfig phaseConfig)
    {
        _phaseTransitionAbility = new PhaseTransitionAbility(phaseConfig);
        _asc.GiveAbility(_phaseTransitionAbility);
        _asc.OnSkillCompleted += abilityTag =>
        {
            if (abilityTag == _phaseTransitionAbility.AbilityTag)
            {
                OnPhaseTransitionCompleted();
            }
        };
    }

    /// <summary>フェーズ遷移開始時の追加処理フック（派生クラスで拡張）。</summary>
    protected virtual void OnPhaseTransitionEntered(Phase newPhase) { }

    /// <summary>フェーズ遷移完了時の追加処理フック（派生クラスで拡張）。</summary>
    protected virtual void OnPhaseTransitionCompleted() { }

    /// <summary>死亡状態突入時の追加処理フック（派生クラスで拡張）。</summary>
    protected virtual void OnDeadEntered() { }

    /// <summary>
    ///     Boss を死亡状態へ移行させます。
    ///     すべての発動中アビリティを即座に中断し、全戦闘タグを安全に解除します。
    /// </summary>
    public virtual void Kill()
    {
        _isDead = true;
        _asc.InterruptCurrentAbility();
        _asc.ClearTags();
        RaiseClearBullets();
        OnDeadEntered();
    }

    /// <summary>
    ///     現在の HP 割合に基づいてフェーズ遷移を評価します。
    /// </summary>
    protected void EvaluatePhaseTransition(
        float currentHp,
        float maxHp,
        float p2Threshold,
        float p3Threshold,
        float p2Duration = 0.0f,
        float p3Duration = 0.0f)
    {
        if (maxHp <= 0.0f || _asc.HasTag(GameplayTag.Phasing) || _isDead) return;

        var hpRatio = currentHp / maxHp;

        if (Phase == Phase.Phase1 && hpRatio <= p2Threshold)
        {
            StartPhaseTransition(Phase.Phase2, p2Duration);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= p3Threshold)
        {
            StartPhaseTransition(Phase.Phase3, p3Duration);
        }
    }

    /// <summary>
    ///     指定フェーズへの遷移を開始し、GAS フェーズ遷移能力を起動します。
    /// </summary>
    protected void StartPhaseTransition(Phase newPhase, float duration = 0.0f)
    {
        Phase = newPhase;
        _asc.InterruptCurrentAbility();
        _phaseTransitionAbility.Configure(duration);
        _asc.TryActivateAbility(_phaseTransitionAbility.AbilityTag);
        OnPhaseTransitionEntered(newPhase);
    }

    /// <summary>
    ///     デバッグ用に指定フェーズへ即座に切り替えます。
    /// </summary>
    public virtual void DebugSetPhase(Phase targetPhase)
    {
        if (_isDead) return;

        Phase = targetPhase;
        _asc.InterruptCurrentAbility();
        _asc.RemoveTag(GameplayTag.Phasing);
        _asc.RemoveTag(GameplayTag.Vulnerable);
        _asc.RemoveTag(GameplayTag.SuperArmor);
        OnPhaseTransitionCompleted();
    }
}
