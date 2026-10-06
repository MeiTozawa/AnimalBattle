using System.Collections.Frozen;
using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     初期化時に検証された固定 BossState と State_Module の対応です。
/// </summary>
/// <remarks>
///     コンストラクターで候補集合を一度だけ検査します。検査成功後は frozen map を参照し、
///     実行中に module の追加、削除、差し替え、生成、世代更新を行いません。
///     検査に失敗した場合は候補集合を破棄して空の map を公開します。
/// </remarks>
public sealed class EnumModuleMap : IEnumModuleMap {
    private readonly FrozenDictionary<BossState, IBossStateModule> _modules;
    private readonly EnumCompletenessResult _validation;

    /// <summary>
    ///     固定 module 候補を初期化して完全性を検査します。
    /// </summary>
    /// <param name="modules">固定 enum に対応する具体的な module 集合。</param>
    /// <param name="availableDependencies">初期化時に利用可能な依存関係名。</param>
    /// <param name="observation">検査結果を記録する任意の観測サービス。</param>
    public EnumModuleMap(
        IEnumerable<IBossStateModule> modules,
        IEnumerable<string>? availableDependencies = null)
    {
        ArgumentNullException.ThrowIfNull(modules);

        Dictionary<BossState, IBossStateModule> candidate = new();
        var constructionResult = EnumCompletenessResult.Success();
        foreach (var module in modules)
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
                constructionResult = EnumCompletenessResult.Failure(
                    ErrorCode.InvalidStateId,
                    "固定 BossState が重複しています。",
                    module.State);
        }

        var dependencies = availableDependencies is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(availableDependencies, StringComparer.Ordinal);

        var validation = constructionResult.IsFailure
            ? constructionResult
            : EnumCompletenessCheck.Validate(candidate, dependencies);

        _validation = validation;
        _modules = validation.IsSuccess
            ? candidate.ToFrozenDictionary()
            : new Dictionary<BossState, IBossStateModule>().ToFrozenDictionary();
    }

    /// <summary>固定 map を検査済みの空集合として生成します。</summary>
    public EnumModuleMap()
        : this(Array.Empty<IBossStateModule>(), null)
    {
    }

    /// <summary>固定 map が実行に使用できるかどうかを取得します。</summary>
    public bool RuntimeReady => _validation.IsSuccess;

    /// <summary>検査成功後に公開される固定 module 数を取得します。</summary>
    public int Count => _modules.Count;

    /// <inheritdoc />
    public EnumCompletenessResult ValidateComplete()
    {
        return _validation;
    }

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
}

/// <summary>固定 enum/module 対応を初期化時に検査する検査器です。</summary>
public static class EnumCompletenessCheck {
    /// <summary>
    ///     固定 BossState 全値、module 型、依存関係、攻撃 mapping の完全性を検査します。
    /// </summary>
    /// <param name="modules">初期化中だけ使用する候補 module 集合。</param>
    /// <param name="availableDependencies">初期化時に利用可能な依存関係名。</param>
    public static EnumCompletenessResult Validate(
        IReadOnlyDictionary<BossState, IBossStateModule> modules,
        IReadOnlySet<string> availableDependencies)
    {
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(availableDependencies);

        if (!FixedBossStateModuleCatalog.IsComplete(out var catalogDiagnostic))
            return EnumCompletenessResult.Failure(ErrorCode.InvalidStateId, catalogDiagnostic);

        if (!AttackStateModuleMap.IsComplete(out var attackDiagnostic))
            return EnumCompletenessResult.Failure(ErrorCode.InvalidBossSkill, attackDiagnostic);


        HashSet<BossType> targetedBossTypes = [];
        foreach (var state in modules.Keys)
            if (state.BossType != BossType.Unknown)
                targetedBossTypes.Add(state.BossType);

        List<FixedBossStateModule> expectedModules = [];
        foreach (var entry in FixedBossStateModuleCatalog.Entries)
            if (targetedBossTypes.Count == 0 || targetedBossTypes.Contains(entry.BossType))
                expectedModules.Add(entry);

        if (modules.Count != expectedModules.Count)
            return EnumCompletenessResult.Failure(
                ErrorCode.UnregisteredState,
                "固定 BossState の module が不足または余分です。");

        foreach (var fixedModule in expectedModules)
        {
            if (!modules.TryGetValue(fixedModule.State, out var module))
                return EnumCompletenessResult.Failure(
                    ErrorCode.UnregisteredState,
                    "固定 BossState の module が未対応です。",
                    fixedModule.State);

            if (module.State != fixedModule.State
             || !fixedModule.ModuleType.IsAssignableFrom(module.GetType()))
                return EnumCompletenessResult.Failure(
                    ErrorCode.InvalidStateId,
                    "固定 BossState と一状態一ファイル module の対応が不正です。",
                    fixedModule.State);

            if (module is IStateModuleDependencyProvider dependencyProvider)
                foreach (var dependency in dependencyProvider.RequiredDependencies)
                    if (string.IsNullOrWhiteSpace(dependency)
                     || !availableDependencies.Contains(dependency))
                        return EnumCompletenessResult.Failure(
                            ErrorCode.MissingDependency,
                            $"必須依存関係が不足しています: {dependency}",
                            fixedModule.State);
        }

        return EnumCompletenessResult.Success();
    }
}