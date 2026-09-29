using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 lifecycle 内の component 発行済み key を事前確保領域で管理します。</summary>
public sealed class OneShotEventGuard {
    private readonly CommandOneShotKey[] _keys;
    private bool _started;

    /// <summary>one-shot key の保持領域を初期化します。</summary>
    public OneShotEventGuard(int capacity = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _keys = new CommandOneShotKey[capacity];
    }

    /// <summary>現在の攻撃 lifecycle で記憶している key 数を取得します。</summary>
    public int Count { get; private set; }

    /// <summary>key の最大容量を取得します。</summary>
    public int Capacity => _keys.Length;

    /// <summary>開始済みの技能を取得します。</summary>
    public BossSkill Skill { get; private set; }

    /// <summary>攻撃の開始 frame を取得します。</summary>
    public long StartFrame { get; private set; }

    /// <summary>新しい攻撃 lifecycle を開始し、以前の key を破棄します。</summary>
    public ErrorOr<Success> Begin(BossSkill skill, long frameUpdateNumber)
    {
        if (!skill.IsValid() || frameUpdateNumber < 0)
            return Error.Validation("OneShotEventGuard.Begin", "技能または開始フレーム番号が無効です。");

        Reset();
        Skill = skill;
        StartFrame = frameUpdateNumber;
        _started = true;
        return Result.Success;
    }


    /// <summary>指定 component の発行権を一度だけ予約します。</summary>
    /// <remarks>予約後の port 失敗も key を消費したままにし、同じ component を再発動しません。</remarks>
    public ErrorOr<Success> Reserve(int componentIndex)
    {
        if (!_started || componentIndex < 0)
            return Error.Validation("OneShotEventGuard.Reserve", "Guard が未開始か component index が不正です。");

        CommandOneShotKey key = new(CommandComponentKind.Attack, Skill, componentIndex, StartFrame);
        for (var index = 0; index < Count; index++)
            if (_keys[index] == key)
                return Error.Failure("OneShotEventGuard.DuplicateKey", "該当 component は既に発行予約済みです。");

        if (Count >= _keys.Length)
            return Error.Failure("OneShotEventGuard.CapacityExceeded", "One-shot key 保持容量を超過しました。");

        _keys[Count++] = key;
        return Result.Success;
    }

    /// <summary>指定 component が既に予約済みかどうかを取得します。</summary>
    public bool Contains(int componentIndex)
    {
        if (!_started || componentIndex < 0) return false;

        CommandOneShotKey key = new(CommandComponentKind.Attack, Skill, componentIndex, StartFrame);
        for (var index = 0; index < Count; index++)
            if (_keys[index] == key)
                return true;

        return false;
    }

    /// <summary>現在の攻撃 lifecycle を終了して key を破棄します。</summary>
    public void Reset()
    {
        Count = 0;
        Skill = default;
        StartFrame = -1;
        _started = false;
    }
}