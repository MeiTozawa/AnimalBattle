using System;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// 各 Boss 固有状態と汎用 DSL 攻撃生命周期（Telegraph, Recovery）の橋渡しを行うアダプターインターフェースです。
/// </summary>
public interface IBossAttackAdapter
{
    /// <summary>現在 Boss の Telegraph 状態を取得します。</summary>
    BossState TelegraphState { get; }

    /// <summary>現在 Boss の Execute 状态を取得します。</summary>
    BossState ExecuteState { get; }

    /// <summary>現在 Boss の Recovery 状態を取得します。</summary>
    BossState RecoveryState { get; }

    /// <summary>現在 Boss の待機・巡航デフォルト状態（Boss1 の Hover、Boss2 の Idle）を取得します。</summary>
    BossState DefaultState { get; }

    /// <summary>現在選択されている攻撃技能を取得します。</summary>
    BossSkill CurrentSkill { get; }

    /// <summary>
    /// 指定された技能の攻撃定義（時序、攻撃範囲、後隙持続時間など）を取得します。
    /// </summary>
    /// <param name="skill">対象の攻撃技能。</param>
    /// <returns>攻撃定義、または技能が無効・未設定の場合のエラー。</returns>
    ErrorOr<AttackDefinition> GetAttackDefinition(BossSkill skill);

    /// <summary>
    /// Telegraph 状態進入時の目標固定および初期化処理を実行します。
    /// </summary>
    /// <param name="context">状態遷移および外部ポートのコンテキスト。</param>
    /// <param name="invocation">フレーム呼び出し情報。</param>
    void OnTelegraphEnter(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>
    /// Recovery 状態進入時の初期化処理を実行します。
    /// </summary>
    /// <param name="context">状態遷移および外部ポートのコンテキスト。</param>
    /// <param name="invocation">フレーム呼び出し情報。</param>
    void OnRecoveryEnter(ref StateContext context, in LifecycleInvocation invocation);

    /// <summary>
    /// Recovery 状態更新時のフレームごとの追加処理を実行します。
    /// </summary>
    /// <param name="context">状態遷移および外部ポートのコンテキスト。</param>
    /// <param name="invocation">フレーム呼び出し情報。</param>
    /// <param name="elapsed">Recovery 開始からの経過時間（秒）。</param>
    void OnRecoveryUpdate(ref StateContext context, in LifecycleInvocation invocation, float elapsed);

    /// <summary>
    /// Recovery 状態が正常完了した際の事後処理を実行します。
    /// </summary>
    void OnRecoveryCompleted();

    /// <summary>
    /// 状態遷移を要求します。
    /// </summary>
    /// <param name="targetState">遷移先の状態。</param>
    /// <param name="reason">遷移理由。</param>
    void RequestTransition(BossState targetState, TransitionReason reason);

    /// <summary>
    /// 現在状態の経過時間を内部状態に同期します。
    /// </summary>
    /// <param name="elapsed">経過時間（秒）。</param>
    void SetStateElapsed(float elapsed);
}
