using System.Drawing;
using AnimalBattle.Features.Tutorial.Actions;
using AnimalBattle.Features.Tutorial.Conditions;
using AnimalBattle.Features.Tutorial.Factories;
using ErrorOr;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Features.Tutorial.Configs;

/// <summary>
///     個別ボス扉設定。
/// </summary>
public sealed class TutorialDoorItemConfig
{
    public Vec2f Pos { get; set; }
    public string BossType { get; set; } = string.Empty;
    public string TexturePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string RuneSymbols { get; set; } = string.Empty;

    private Color _runeColorIdle;
    [ConfigurationKeyName("RuneColorIdle")]
    public string RuneColorIdleHex { get => string.Empty; set => _runeColorIdle = value.ToColor(); }
    public Color RuneColorIdle => _runeColorIdle;

    private Color _runeColorActive;
    [ConfigurationKeyName("RuneColorActive")]
    public string RuneColorActiveHex { get => string.Empty; set => _runeColorActive = value.ToColor(); }
    public Color RuneColorActive => _runeColorActive;

    private Color _auraColor;
    [ConfigurationKeyName("AuraColor")]
    public string AuraColorHex { get => string.Empty; set => _auraColor = value.ToColor(); }
    public Color AuraColor => _auraColor;

    public float MotionSpeed { get; set; }
    public float MotionAmplitude { get; set; }
}

/// <summary>
///     タイミングおよび物理パラメータ設定。
/// </summary>
public sealed class TutorialTimingConfig
{
    public float TargetCollectDelay { get; set; }
    public float MinionDefeatDelay { get; set; }
    public float RetryDuration { get; set; }
    public float TargetFloatSpeed { get; set; }
    public float TargetFloatAmplitude { get; set; }
    public float ReachMargin { get; set; }
}

/// <summary>
///     チュートリアル全テキスト設定。
/// </summary>
public sealed class TutorialTextsConfig
{
    public string RetryPrompt { get; set; } = string.Empty;
    public string DoorInteractPrompt { get; set; } = string.Empty;
    public string ControlBoardTitle { get; set; } = string.Empty;
    public string ControlBoardMove { get; set; } = string.Empty;
    public string ControlBoardJump { get; set; } = string.Empty;
    public string ControlBoardDash { get; set; } = string.Empty;
    public string ControlBoardAttack { get; set; } = string.Empty;
    public string ControlBoardSpecial { get; set; } = string.Empty;
}

/// <summary>
///     ステップ開始時に実行されるアクション設定。
/// </summary>
public sealed class TutorialActionConfig
{
    public string Type { get; set; } = string.Empty;
    public Vec2f Pos { get; set; }
    public float Radius { get; set; }
    public float Amount { get; set; }
    public string MinionType { get; set; } = string.Empty;
    public int Count { get; set; }
    public string BossType { get; set; } = string.Empty;
    public string TexturePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int Segments { get; set; }
}

/// <summary>
///     ステップ達成条件設定。
/// </summary>
public sealed class TutorialConditionConfig
{
    public string Type { get; set; } = string.Empty;
    public float Duration { get; set; }
}

/// <summary>
///     チュートリアル内の単一ステップ設定。
/// </summary>
public sealed class TutorialStepConfig
{
    public string Subtitle { get; set; } = string.Empty;
    public string SubtitleCat { get; set; } = string.Empty;
    public string SubtitleBunny { get; set; } = string.Empty;
    public TutorialActionConfig[] Actions { get; set; } = [];
    public TutorialConditionConfig Condition { get; set; } = new();

    /// <summary>小怪を含む戦闘ステップかどうか判定します。</summary>
    public bool HasMinions => Array.Exists(Actions, a => a.Type.Equals("SpawnMinion", StringComparison.OrdinalIgnoreCase)) ||
                              Condition.Type.Equals("DefeatMinions", StringComparison.OrdinalIgnoreCase);

    private ITutorialAction[] _runtimeActions = [];
    private ITutorialCondition? _runtimeCondition;

    /// <summary>ホットパス実行用に事前生成されたアクション一覧を取得します（ゼロアロケーション）。</summary>
    public ITutorialAction[] GetRuntimeActions() => _runtimeActions;

    /// <summary>ホットパス実行用に事前生成された条件判定インスタンスを取得します（ゼロアロケーション）。</summary>
    public ITutorialCondition GetRuntimeCondition() => _runtimeCondition!;

    internal void InitializeRuntime(ITutorialAction[] actions, ITutorialCondition condition)
    {
        _runtimeActions = actions;
        _runtimeCondition = condition;
    }
}

/// <summary>
///     チュートリアルフェーズ設定。複数のステップを内包する。
/// </summary>
public sealed class TutorialPhaseConfig
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public TutorialStepConfig[] Steps { get; set; } = [];

    /// <summary>小怪を含む戦闘フェーズかどうか判定します。</summary>
    public bool HasMinions => Array.Exists(Steps, s => s.HasMinions);

    /// <summary>ボス扉を含むフェーズかどうか判定します。</summary>
    public bool HasBossDoor => Array.Exists(Steps, s => Array.Exists(s.Actions, a => a.Type.Equals("SpawnDoor", StringComparison.OrdinalIgnoreCase)));
}

/// <summary>
///     チュートリアル全体設定クラス（ゲームプレイ・フェーズ進行ロジック）。
/// </summary>
public sealed class TutorialConfig : IConfigLifecycle
{
    public TutorialTimingConfig Timing { get; set; } = new();
    public TutorialTextsConfig Texts { get; set; } = new();

    /// <summary>
    ///     データ駆動フェーズシーケンス一覧。
    /// </summary>
    public TutorialPhaseConfig[] Phases { get; set; } = [];

    /// <inheritdoc />
    public ErrorOr<Success> Validate()
    {
        if (Phases == null || Phases.Length == 0)
        {
            return Error.Validation("TutorialConfig.Phases", "Phases が設定されていません。");
        }

        for (var i = 0; i < Phases.Length; i++)
        {
            var phase = Phases[i];
            if (phase.Steps == null || phase.Steps.Length == 0)
            {
                return Error.Validation("TutorialConfig.Steps", $"フェーズ[{i}] ({phase.Title}) にステップが設定されていません。");
            }

            for (var j = 0; j < phase.Steps.Length; j++)
            {
                var step = phase.Steps[j];
                if (step.Actions == null || step.Actions.Length == 0)
                {
                    return Error.Validation("TutorialConfig.Actions", $"フェーズ[{i}] ステップ[{j}] にアクションが設定されていません。");
                }

                if (string.IsNullOrWhiteSpace(step.Condition?.Type))
                {
                    return Error.Validation("TutorialConfig.Condition", $"フェーズ[{i}] ステップ[{j}] の条件 Type が空です。");
                }
            }
        }

        return Result.Success;
    }

    /// <inheritdoc />
    public void PostProcess()
    {
        for (var i = 0; i < Phases.Length; i++)
        {
            var phase = Phases[i];
            for (var j = 0; j < phase.Steps.Length; j++)
            {
                var step = phase.Steps[j];
                var runtimeActions = new ITutorialAction[step.Actions.Length];
                for (var a = 0; a < step.Actions.Length; a++)
                {
                    runtimeActions[a] = TutorialStepFactory.CreateAction(step.Actions[a]);
                }
                var runtimeCondition = TutorialStepFactory.CreateCondition(step.Condition);
                step.InitializeRuntime(runtimeActions, runtimeCondition);
            }
        }
    }
}
