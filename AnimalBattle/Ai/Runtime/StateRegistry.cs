using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// State_Module の対応表を原子的に検証・登録・差し替えする registry です。
/// </summary>
public sealed class StateRegistry : IStateRegistry
{
    private sealed class RegistrySnapshot
    {
        internal RegistrySnapshot(Dictionary<StateId, IBossStateModule> modules, long generation)
        {
            Modules = modules;
            Generation = generation;
        }

        internal Dictionary<StateId, IBossStateModule> Modules { get; }
        internal long Generation { get; }
    }

    private readonly object _sync = new();
    private readonly HashSet<string> _availableDependencies;
    private readonly IObservationService? _observation;
    private RegistrySnapshot _snapshot = new(new Dictionary<StateId, IBossStateModule>(), 0);

    /// <summary>現在の登録世代を取得します。</summary>
    public long Generation => Volatile.Read(ref _snapshot).Generation;

    /// <summary>登録済み State_Module 数を取得します。</summary>
    public int Count => Volatile.Read(ref _snapshot).Modules.Count;

    /// <summary>依存関係、診断サービスを指定して registry を生成します。</summary>
    public StateRegistry(IEnumerable<string>? availableDependencies = null, IObservationService? observation = null)
    {
        _availableDependencies = availableDependencies is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(availableDependencies, StringComparer.Ordinal);
        _observation = observation;
    }

    /// <summary>診断サービスだけを指定して registry を生成します。</summary>
    public StateRegistry(IObservationService observation)
        : this(null, observation)
    {
    }

    /// <summary>登録済み module を取得します。</summary>
    public bool TryGet(StateId stateId, out IBossStateModule module)
    {
        RegistrySnapshot snapshot = Volatile.Read(ref _snapshot);
        return snapshot.Modules.TryGetValue(stateId, out module!);
    }

    /// <summary>現在の全登録内容を検証します。</summary>
    public RegistryValidationResult Validate()
    {
        lock (_sync)
        {
            RegistrySnapshot snapshot = _snapshot;
            foreach (KeyValuePair<StateId, IBossStateModule> entry in snapshot.Modules)
            {
                RegistryValidationResult result = ValidateModule(entry.Key, entry.Value, snapshot.Modules, snapshot.Generation);
                if (result.IsFailure)
                {
                    return result;
                }
            }

            return RegistryValidationResult.Success(snapshot.Generation);
        }
    }

    /// <summary>module を新規登録し、登録成功時だけ世代を進めます。</summary>
    public RegistryValidationResult Register(IBossStateModule module)
    {
        return Register(module, false);
    }

    /// <summary>指定 State_Id の module を差し替え、登録成功時だけ世代を進めます。</summary>
    public RegistryValidationResult Replace(IBossStateModule module)
    {
        return Register(module, true);
    }

    /// <summary>module の登録を試み、成功可否を返します。</summary>
    public bool TryRegister(IBossStateModule module, out RegistryValidationResult result)
    {
        result = Register(module);
        return result.IsSuccess;
    }

    /// <summary>module の差し替えを試み、成功可否を返します。</summary>
    public bool TryReplace(IBossStateModule module, out RegistryValidationResult result)
    {
        result = Replace(module);
        return result.IsSuccess;
    }

    /// <summary>module の登録または差し替えを実行します。</summary>
    public RegistryValidationResult Register(IBossStateModule module, bool replaceExisting)
    {
        if (module is null)
        {
            return Reject(ErrorCode.MissingDependency, "State_Module が指定されていません。", default, Generation);
        }

        lock (_sync)
        {
            RegistrySnapshot snapshot = _snapshot;
            Dictionary<StateId, IBossStateModule> candidate = new(snapshot.Modules);
            if (candidate.ContainsKey(module.Id) && !replaceExisting)
            {
                return Reject(ErrorCode.InvalidStateId, "State_Id が重複しています。", module.Id, snapshot.Generation);
            }

            if (!module.Id.IsValid)
            {
                return Reject(ErrorCode.InvalidStateId, "State_Id の形式が不正です。", module.Id, snapshot.Generation);
            }

            foreach (KeyValuePair<StateId, IBossStateModule> entry in candidate)
            {
                if (entry.Key != module.Id && ReferenceEquals(entry.Value, module))
                {
                    return Reject(ErrorCode.InvalidStateId, "一つの module に複数の State_Id は登録できません。", module.Id, snapshot.Generation);
                }
            }

            candidate[module.Id] = module;
            RegistryValidationResult validation = ValidateModule(module.Id, module, candidate, snapshot.Generation);
            if (validation.IsFailure)
            {
                if (!TryRecord(module.Id, DiagnosticSeverity.ExecutionError, validation.ErrorCode, validation.Message))
                {
                    return RegistryValidationResult.Failure(ErrorCode.ObservationFailed, "Registry の診断記録に失敗したため登録を拒否しました。", module.Id, snapshot.Generation);
                }

                return validation;
            }

            if (!TryRecord(module.Id, DiagnosticSeverity.Information, ErrorCode.None, "State_Module を登録しました。"))
            {
                return RegistryValidationResult.Failure(ErrorCode.ObservationFailed, "Registry の診断記録に失敗したため登録を拒否しました。", module.Id, snapshot.Generation);
            }

            RegistrySnapshot nextSnapshot = new(candidate, snapshot.Generation + 1);
            Volatile.Write(ref _snapshot, nextSnapshot);
            return RegistryValidationResult.Success(nextSnapshot.Generation, "State_Module を登録しました。");
        }
    }

    private RegistryValidationResult ValidateModule(
        StateId key,
        IBossStateModule module,
        IReadOnlyDictionary<StateId, IBossStateModule> modules,
        long generation)
    {
        if (!key.IsValid || module.Id != key)
        {
            return RegistryValidationResult.Failure(ErrorCode.InvalidStateId, "State_Id の形式または module の対応が不正です。", key, generation);
        }

        if (module is IStateModuleDependencyProvider dependencies)
        {
            foreach (string dependency in dependencies.RequiredDependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency) || !_availableDependencies.Contains(dependency))
                {
                    return RegistryValidationResult.Failure(ErrorCode.MissingDependency, $"必須依存関係が不足しています: {dependency}", key, generation);
                }
            }
        }

        foreach (KeyValuePair<StateId, IBossStateModule> entry in modules)
        {
            if (entry.Key != key && ReferenceEquals(entry.Value, module))
            {
                return RegistryValidationResult.Failure(ErrorCode.InvalidStateId, "一つの module に複数の State_Id は登録できません。", key, generation);
            }
        }

        return RegistryValidationResult.Success(generation);
    }

    private RegistryValidationResult Reject(ErrorCode errorCode, string message, StateId stateId, long generation)
    {
        bool recorded = TryRecord(stateId, DiagnosticSeverity.ExecutionError, errorCode, message);
        if (!recorded)
        {
            return RegistryValidationResult.Failure(ErrorCode.ObservationFailed, "Registry の診断記録に失敗しました。", stateId, generation);
        }

        return RegistryValidationResult.Failure(errorCode, message, stateId, generation);
    }

    private bool TryRecord(StateId stateId, DiagnosticSeverity severity, ErrorCode errorCode, string message)
    {
        if (_observation is null)
        {
            return true;
        }

        ObservationEvent observation = new(
            0,
            BossType.Unknown,
            stateId,
            Phase.Unknown,
            LifecycleOperation.Enter,
            0,
            0,
            default,
            default,
            default,
            severity,
            errorCode,
            message);
        try
        {
            return _observation.TryRecord(in observation);
        }
        catch
        {
            return false;
        }
    }
}
