using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Components;

namespace AnimalBattle.Ai.Runtime;

/// <summary>エラー復帰時に適用する安全値の項目を表す bit mask です。</summary>
[Flags]
public enum SafetyParameterApplyMask
{
    /// <summary>適用項目がありません。</summary>
    None = 0,
    /// <summary>移動速度を安全値へ変更します。</summary>
    MovementSpeed = 1,
    /// <summary>攻撃判定と攻撃生成を停止します。</summary>
    AttackHitbox = 2,
    /// <summary>無敵状態を安全値へ変更します。</summary>
    Invulnerability = 4,
    /// <summary>状態および攻撃タイマーを安全値へ変更します。</summary>
    Timer = 8
}

/// <summary>エラー復帰時に適用する項目別の安全値です。</summary>
public readonly record struct SafetyParameter(
    float MovementSpeedX,
    float MovementSpeedY,
    bool DisableAttackHitbox,
    bool IsInvulnerable,
    float StateTimer,
    float StateDuration)
{
    /// <summary>攻撃を停止し、移動とタイマーをゼロにする既定の安全値です。</summary>
    public static SafetyParameter Default => new(0.0f, 0.0f, true, true, 0.0f, 0.0f);

    /// <summary>安全値の数値が有限値かどうかを取得します。</summary>
    public bool IsValid => float.IsFinite(MovementSpeedX)
        && float.IsFinite(MovementSpeedY)
        && float.IsFinite(StateTimer)
        && float.IsFinite(StateDuration)
        && StateTimer >= 0.0f
        && StateDuration >= 0.0f;
}

/// <summary>安全値の項目別適用結果です。</summary>
public readonly record struct SafetyParameterApplicationResult(
    SafetyParameterApplyMask AppliedMask,
    SafetyParameterApplyMask FailedMask)
{
    /// <summary>失敗した項目があるかどうかを取得します。</summary>
    public bool HasFailure => FailedMask != SafetyParameterApplyMask.None;

    /// <summary>全項目を適用できたかどうかを取得します。</summary>
    public bool IsComplete => !HasFailure;
}

/// <summary>runtime slot に安全値を適用する処理をまとめた helper です。</summary>
internal static class SafetyParameterApplier
{
    /// <summary>指定した安全値を成功項目を保持しながら適用します。</summary>
    public static SafetyParameterApplicationResult Apply(
        BossRuntimeSlot runtime,
        in SafetyParameter parameter,
        SafetyParameterApplyMask applied,
        SafetyParameterApplyMask failed)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!parameter.IsValid)
        {
            return new SafetyParameterApplicationResult(applied, failed | SafetyParameterApplyMask.Timer);
        }

        runtime.ClearCommands();
        if (!applied.HasFlag(SafetyParameterApplyMask.MovementSpeed))
        {
            bool success = parameter.MovementSpeedX == 0.0f && parameter.MovementSpeedY == 0.0f
                ? runtime.Context.Movement.Stop()
                : runtime.Context.Movement.SetVelocity(parameter.MovementSpeedX, parameter.MovementSpeedY);
            if (success)
            {
                applied |= SafetyParameterApplyMask.MovementSpeed;
                failed &= ~SafetyParameterApplyMask.MovementSpeed;
            }
            else failed |= SafetyParameterApplyMask.MovementSpeed;
        }

        if (!applied.HasFlag(SafetyParameterApplyMask.AttackHitbox))
        {
            // ECS 側の判定停止を先に確定し、弾整理が失敗しても新規生成は止めます。
            runtime.AiData.AttackGenerationEnabled = !parameter.DisableAttackHitbox;
            runtime.AttackData.Stage = AttackStage.None;
            runtime.AttackData.OneShotEmissionMask = 0;
            bool success = !parameter.DisableAttackHitbox || runtime.Context.Attack.ClearBossBullets();
            if (success)
            {
                applied |= SafetyParameterApplyMask.AttackHitbox;
                failed &= ~SafetyParameterApplyMask.AttackHitbox;
            }
            else failed |= SafetyParameterApplyMask.AttackHitbox;
        }

        if (!applied.HasFlag(SafetyParameterApplyMask.Invulnerability))
        {
            runtime.AiData.IsInvulnerable = parameter.IsInvulnerable;
            applied |= SafetyParameterApplyMask.Invulnerability;
            failed &= ~SafetyParameterApplyMask.Invulnerability;
        }

        if (!applied.HasFlag(SafetyParameterApplyMask.Timer))
        {
            runtime.AiData.StateTimer = parameter.StateTimer;
            runtime.AiData.StateDuration = parameter.StateDuration;
            runtime.AttackData.SubTimer = 0.0f;
            runtime.AttackData.ElapsedDuration = 0.0f;
            applied |= SafetyParameterApplyMask.Timer;
            failed &= ~SafetyParameterApplyMask.Timer;
        }

        return new SafetyParameterApplicationResult(applied, failed);
    }
}
