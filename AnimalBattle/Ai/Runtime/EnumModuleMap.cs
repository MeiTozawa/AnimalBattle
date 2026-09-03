using System.Collections.Frozen;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 初期化時に検証された固定 BossState と State_Module の対応です。
/// </summary>
/// <remarks>
/// コンストラクターで候補集合を一度だけ検査します。検査成功後は frozen map を参照し、
/// 実行中に module の追加、削除、差し替え、生成、世代更新を行いません。
/// 検査に失敗した場合は候補集合を破棄して空の map を公開します。
/// </remarks>
public sealed class EnumModuleMap : IEnumModuleMap
{
    private readonly FrozenDictionary<BossState, IBossStateModule> _modules;
    private readonly EnumCompletenessResult _validation;

    /// <summary>固定 map が実行に使用できるかどうかを取得します。</summary>
    public bool RuntimeReady => _validation.IsSuccess;

    /// <summary>検査成功後に公開される固定 module 数を取得します。</summary>
    public int Count => _modules.Count;

    /// <summary>
    /// 固定 module 候補を初期化して完全性を検査します。
    /// </summary>
    /// <param name="modules">固定 enum に対応する具体的な module 集合。</param>
    /// <param name="availableDependencies">初期化時に利用可能な依存関係名。</param>
    /// <param name="observation">検査結果を記録する任意の観測サービス。</param>
    public EnumModuleMap(
        IEnumerable<IBossStateModule> modules,
        IEnumerable<string>? availableDependencies = null,
        IObservationService? observation = null)
    {
        ArgumentNullException.ThrowIfNull(modules);

        Dictionary<BossState, IBossStateModule> candidate = new();
        EnumCompletenessResult constructionResult = EnumCompletenessResult.Success();
        foreach (IBossStateModule? module in modules)
        {
            if (module is null)
            {
                constructionResult = EnumCompletenessResult.Failure(
                    ErrorCode.MissingDependency,
                    "State_Module が指定されていません。");
                continue;
            }

            if (!module.State.IsValid())
            {
                constructionResult = EnumCompletenessResult.Failure(
                    ErrorCode.InvalidStateId,
                    "Unknown は State_Module に対応付けられません。",
                    module.State);
                continue;
            }

            if (!candidate.TryAdd(module.State, module))
            {
                constructionResult = EnumCompletenessResult.Failure(
                    ErrorCode.InvalidStateId,
                    "固定 BossState が重複しています。",
                    module.State);
            }
        }

        HashSet<string> dependencies = availableDependencies is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(availableDependencies, StringComparer.Ordinal);

        EnumCompletenessResult validation = constructionResult.IsFailure
            ? constructionResult
            : EnumCompletenessCheck.Validate(candidate, dependencies);
        validation = RecordFailureIfNeeded(validation, observation);

        _validation = validation;
        _modules = validation.IsSuccess
            ? candidate.ToFrozenDictionary()
            : new Dictionary<BossState, IBossStateModule>().ToFrozenDictionary();
    }

    /// <summary>固定 map を検査済みの空集合として生成します。</summary>
    /// <remarks>診断やテストで不完全 map を明示的に扱う場合に使用します。</remarks>
    public EnumModuleMap(IObservationService observation)
        : this(Array.Empty<IBossStateModule>(), null, observation)
    {
    }

    /// <inheritdoc />
    public EnumCompletenessResult ValidateComplete() => _validation;

    /// <inheritdoc />
    public bool Get(BossState state, out IBossStateModule module)
    {
        if (!_validation.IsSuccess)
        {
            module = null!;
            return false;
        }

        return _modules.TryGetValue(state, out module!);
    }

    private static EnumCompletenessResult RecordFailureIfNeeded(
        EnumCompletenessResult validation,
        IObservationService? observation)
    {
        if (validation.IsSuccess || observation is null)
        {
            return validation;
        }

        ObservationEvent diagnostic = new(
            0,
            BossType.Unknown,
            validation.State,
            Phase.Unknown,
            LifecycleOperation.Enter,
            0,
            0,
            severity: DiagnosticSeverity.ExecutionError,
            errorCode: validation.ErrorCode,
            message: validation.Message);
        try
        {
            if (!observation.Record(in diagnostic).IsError)
            {
                return validation;
            }
        }
        catch
        {
            // 診断記録の例外は map の公開を成功扱いにしません。
        }

        return EnumCompletenessResult.Failure(
            ErrorCode.ObservationFailed,
            "固定 module 検査の診断記録に失敗しました。",
            validation.State);
    }
}

/// <summary>固定 enum/module 対応を初期化時に検査する検査器です。</summary>
public static class EnumCompletenessCheck
{
    /// <summary>
    /// 固定 BossState 全値、module 型、依存関係、攻撃 mapping の完全性を検査します。
    /// </summary>
    /// <param name="modules">初期化中だけ使用する候補 module 集合。</param>
    /// <param name="availableDependencies">初期化時に利用可能な依存関係名。</param>
    public static EnumCompletenessResult Validate(
        IReadOnlyDictionary<BossState, IBossStateModule> modules,
        IReadOnlySet<string> availableDependencies)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(availableDependencies);

        if (!FixedBossStateModuleCatalog.IsComplete(out string catalogDiagnostic))
        {
            return EnumCompletenessResult.Failure(ErrorCode.InvalidStateId, catalogDiagnostic);
        }

        if (!AttackStateModuleMap.IsComplete(out string attackDiagnostic))
        {
            return EnumCompletenessResult.Failure(ErrorCode.InvalidBossSkill, attackDiagnostic);
        }


        if (modules.Count != FixedBossStateModuleCatalog.Entries.Count)
        {
            return EnumCompletenessResult.Failure(
                ErrorCode.UnregisteredState,
                "固定 BossState の module が不足または余分です。");
        }

        foreach (FixedBossStateModule fixedModule in FixedBossStateModuleCatalog.Entries)
        {
            if (!modules.TryGetValue(fixedModule.State, out IBossStateModule? module))
            {
                return EnumCompletenessResult.Failure(
                    ErrorCode.UnregisteredState,
                    "固定 BossState の module が未対応です。",
                    fixedModule.State);
            }

            if (module.State != fixedModule.State
                || !fixedModule.ModuleType.IsAssignableFrom(module.GetType())
                || !string.Equals(
                    fixedModule.ModuleFileName,
                    $"{module.GetType().Name}.cs",
                    StringComparison.Ordinal))
            {
                return EnumCompletenessResult.Failure(
                    ErrorCode.InvalidStateId,
                    "固定 BossState と一状態一ファイル module の対応が不正です。",
                    fixedModule.State);
            }

            if (module is IStateModuleDependencyProvider dependencyProvider)
            {
                foreach (string dependency in dependencyProvider.RequiredDependencies)
                {
                    if (string.IsNullOrWhiteSpace(dependency)
                        || !availableDependencies.Contains(dependency))
                    {
                        return EnumCompletenessResult.Failure(
                            ErrorCode.MissingDependency,
                            $"必須依存関係が不足しています: {dependency}",
                            fixedModule.State);
                    }
                }
            }
        }

        return EnumCompletenessResult.Success();
    }
}
