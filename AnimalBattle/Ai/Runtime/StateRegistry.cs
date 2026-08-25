using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 初期化時に渡された固定 State_Module 集合を検査して保持する互換境界です。
/// </summary>
/// <remarks>実行時の登録、削除、差し替え、世代管理は提供しません。</remarks>
[Obsolete("EnumModuleMap を使用してください。")]
public sealed class StateRegistry : IStateRegistry
{
    private readonly Dictionary<BossState, IBossStateModule> _modules;
    private readonly HashSet<string> _availableDependencies;
    private readonly IObservationService? _observation;
    private readonly RegistryValidationResult? _constructionFailure;

    /// <summary>登録済み固定 module 数を取得します。</summary>
    public int Count => _modules.Count;

    /// <summary>固定 module 集合を生成します。</summary>
    public StateRegistry(
        IEnumerable<IBossStateModule>? modules = null,
        IEnumerable<string>? availableDependencies = null,
        IObservationService? observation = null)
    {
        _modules = new Dictionary<BossState, IBossStateModule>();
        _availableDependencies = availableDependencies is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(availableDependencies, StringComparer.Ordinal);
        _observation = observation;

        if (modules is null)
        {
            return;
        }

        foreach (IBossStateModule module in modules)
        {
            if (module is null)
            {
                _constructionFailure = RegistryValidationResult.Failure(ErrorCode.MissingDependency, "State_Module が指定されていません。");
                continue;
            }

            if (!_modules.TryAdd(module.State, module))
            {
                _constructionFailure = RegistryValidationResult.Failure(ErrorCode.InvalidStateId, "固定 BossState が重複しています。", module.State);
            }
        }
    }

    /// <summary>診断サービスだけを指定して空の固定集合を生成します。</summary>
    public StateRegistry(IObservationService observation)
        : this(null, null, observation)
    {
    }

    /// <summary>指定した固定状態に対応する module を取得します。</summary>
    public bool TryGet(BossState state, out IBossStateModule module)
        => _modules.TryGetValue(state, out module!);

    /// <summary>固定 enum/module 対応を検証します。</summary>
    public EnumCompletenessResult ValidateComplete()
    {
        RegistryValidationResult validation = Validate();
        return validation.IsFailure
            ? EnumCompletenessResult.Failure(validation.ErrorCode, validation.Message, validation.State)
            : EnumCompletenessResult.Success();
    }

    /// <summary>固定 module 集合の完全性、依存関係、一対一対応を検証します。</summary>
    public RegistryValidationResult Validate()
    {
        if (_constructionFailure.HasValue)
        {
            return Record(_constructionFailure.Value);
        }

        if (!FixedBossStateModuleCatalog.IsComplete(out string catalogDiagnostic))
        {
            return Record(RegistryValidationResult.Failure(ErrorCode.InvalidStateId, catalogDiagnostic));
        }

        if (_modules.Count != FixedBossStateModuleCatalog.Entries.Count)
        {
            return Record(RegistryValidationResult.Failure(ErrorCode.UnregisteredState, "固定 BossState の module が不足しています。"));
        }

        foreach (FixedBossStateModule fixedModule in FixedBossStateModuleCatalog.Entries)
        {
            if (!_modules.TryGetValue(fixedModule.State, out IBossStateModule? module))
            {
                return Record(RegistryValidationResult.Failure(ErrorCode.UnregisteredState, "固定 BossState の module が未登録です。", fixedModule.State));
            }

            if (module.State != fixedModule.State || !fixedModule.ModuleType.IsAssignableFrom(module.GetType()))
            {
                return Record(RegistryValidationResult.Failure(ErrorCode.InvalidStateId, "固定 BossState と State_Module の対応が不正です。", fixedModule.State));
            }

            if (module is IStateModuleDependencyProvider dependencies)
            {
                foreach (string dependency in dependencies.RequiredDependencies)
                {
                    if (string.IsNullOrWhiteSpace(dependency) || !_availableDependencies.Contains(dependency))
                    {
                        return Record(RegistryValidationResult.Failure(ErrorCode.MissingDependency, $"必須依存関係が不足しています: {dependency}", fixedModule.State));
                    }
                }
            }
        }

        return RegistryValidationResult.Success();
    }

    private RegistryValidationResult Record(RegistryValidationResult result)
    {
        if (_observation is null)
        {
            return result;
        }

        ObservationEvent observation = new(
            0,
            BossType.Unknown,
            result.State,
            Phase.Unknown,
            LifecycleOperation.Enter,
            0,
            0,
            severity: result.IsSuccess ? DiagnosticSeverity.Information : DiagnosticSeverity.ExecutionError,
            errorCode: result.ErrorCode,
            message: result.Message);
        try
        {
            if (!_observation.TryRecord(in observation))
            {
                return RegistryValidationResult.Failure(ErrorCode.ObservationFailed, "固定 module 検査の診断記録に失敗しました。", result.State);
            }
        }
        catch
        {
            return RegistryValidationResult.Failure(ErrorCode.ObservationFailed, "固定 module 検査の診断記録に失敗しました。", result.State);
        }

        return result;
    }
}
