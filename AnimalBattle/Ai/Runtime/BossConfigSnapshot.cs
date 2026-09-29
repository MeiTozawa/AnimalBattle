using System.Collections.Frozen;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     初期化時または設定更新時に検証済みとして公開する Boss AI 設定 snapshot です。
/// </summary>
public readonly struct BossConfigSnapshot {
    private static readonly FrozenDictionary<BossPhaseKey, BossPhaseSnapshot> _emptyPhases =
        new Dictionary<BossPhaseKey, BossPhaseSnapshot>().ToFrozenDictionary();

    private static readonly FrozenDictionary<BossSkill, AttackDefinition> _emptyAttacks =
        new Dictionary<BossSkill, AttackDefinition>().ToFrozenDictionary();

    private readonly FrozenDictionary<BossPhaseKey, BossPhaseSnapshot>? _phases;
    private readonly FrozenDictionary<BossSkill, AttackDefinition>? _attacks;

    /// <summary>未初期化の設定 snapshot を表す値です。</summary>
    public static BossConfigSnapshot Empty => default;

    /// <summary>設定 snapshot の世代を取得します。</summary>
    public long Version { get; }

    /// <summary>設定が検証済みで実行に使用できるかどうかを取得します。</summary>
    public bool IsValid { get; }

    /// <summary>設定の入力元を識別する不変の名前を取得します。</summary>
    public string? SourceName { get; }

    /// <summary>検証済み Phase 設定を取得します。</summary>
    public IReadOnlyDictionary<BossPhaseKey, BossPhaseSnapshot> Phases => _phases ?? _emptyPhases;

    /// <summary>BossSkill ごとの検証済み攻撃定義を取得します。</summary>
    public IReadOnlyDictionary<BossSkill, AttackDefinition> Attacks => _attacks ?? _emptyAttacks;

    /// <summary>
    ///     互換用の空の攻撃定義を持つ snapshot を生成します。
    /// </summary>
    /// <param name="version">設定 version。0 以上である必要があります。</param>
    /// <param name="sourceName">設定を識別する不変の名前。</param>
    public BossConfigSnapshot(long version, string? sourceName = null)
        : this(
            version,
            sourceName,
            true,
            _emptyPhases,
            _emptyAttacks)
    {
    }

    private BossConfigSnapshot(
        long version,
        string? sourceName,
        bool isValid,
        FrozenDictionary<BossPhaseKey, BossPhaseSnapshot> phases,
        FrozenDictionary<BossSkill, AttackDefinition> attacks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        Version = version;
        IsValid = isValid;
        SourceName = sourceName;
        _phases = phases;
        _attacks = attacks;
    }

    /// <summary>指定した Boss 種別と Phase の設定を取得します。</summary>
    /// <param name="bossType">対象 Boss 種別。</param>
    /// <param name="phase">取得する Phase。</param>
    /// <param name="snapshot">取得した Phase 設定。</param>
    /// <returns>設定が存在する場合は true。</returns>
    public bool GetPhase(BossType bossType, Phase phase, out BossPhaseSnapshot snapshot)
    {
        return Phases.TryGetValue(new BossPhaseKey(bossType, phase), out snapshot);
    }

    /// <summary>指定した Phase の設定を取得します。</summary>
    /// <param name="phase">取得する Phase。</param>
    /// <param name="snapshot">取得した Phase 設定。</param>
    /// <returns>Boss 種別を限定せず一意に取得できる場合は true。</returns>
    public bool GetPhase(Phase phase, out BossPhaseSnapshot snapshot)
    {
        BossPhaseSnapshot? found = null;
        foreach (var item in Phases)
        {
            if (item.Key.Phase != phase) continue;

            if (found.HasValue)
            {
                snapshot = default;
                return false;
            }

            found = item.Value;
        }

        snapshot = found.GetValueOrDefault();
        return found.HasValue;
    }

    /// <summary>指定した BossSkill の攻撃定義を取得します。</summary>
    /// <param name="skill">取得する BossSkill。</param>
    /// <param name="definition">取得した攻撃定義。</param>
    /// <returns>設定が存在する場合は true。</returns>
    public bool GetAttack(BossSkill skill, out AttackDefinition definition)
    {
        return Attacks.TryGetValue(skill, out definition!);
    }

    /// <summary>
    ///     検証済みの設定値を凍結した snapshot を生成します。
    /// </summary>
    /// <param name="version">設定 version。</param>
    /// <param name="sourceName">設定の入力元。</param>
    /// <param name="phases">検証済み Phase 設定。</param>
    /// <param name="attacks">検証済み攻撃定義。</param>
    /// <returns>不変 snapshot。</returns>
    internal static BossConfigSnapshot CreateValidated(
        long version,
        string? sourceName,
        IEnumerable<BossPhaseSnapshot> phases,
        IEnumerable<AttackDefinition> attacks)
    {
        ArgumentNullException.ThrowIfNull(phases);
        ArgumentNullException.ThrowIfNull(attacks);

        Dictionary<BossPhaseKey, BossPhaseSnapshot> phaseMap = new();
        foreach (var phase in phases) phaseMap.Add(new BossPhaseKey(phase.BossType, phase.Phase), phase);

        Dictionary<BossSkill, AttackDefinition> attackMap = new();
        foreach (var attack in attacks) attackMap.Add(attack.Skill, attack);

        return new BossConfigSnapshot(
            version,
            sourceName,
            true,
            phaseMap.ToFrozenDictionary(),
            attackMap.ToFrozenDictionary());
    }
}

/// <summary>
///     State_Module が参照する検証済み設定 snapshot の提供境界です。
/// </summary>
public interface IConfigProvider {
    /// <summary>現在有効な設定 snapshot を取得します。</summary>
    BossConfigSnapshot Current { get; }
}