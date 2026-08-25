using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 lifecycle 内の component 発行済み key を事前確保領域で管理します。</summary>
public sealed class OneShotEventGuard
{
    private readonly CommandOneShotKey[] _keys;
    private int _count;
    private AttackId _attackId;
    private long _startFrame;
    private bool _started;

    /// <summary>現在の攻撃 lifecycle で記憶している key 数を取得します。</summary>
    public int Count => _count;
    /// <summary>key の最大容量を取得します。</summary>
    public int Capacity => _keys.Length;
    /// <summary>開始済みの攻撃 ID を取得します。</summary>
    public AttackId AttackId => _attackId;
    /// <summary>攻撃の開始 frame を取得します。</summary>
    public long StartFrame => _startFrame;

    /// <summary>one-shot key の保持領域を初期化します。</summary>
    public OneShotEventGuard(int capacity = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _keys = new CommandOneShotKey[capacity];
    }

    /// <summary>新しい攻撃 lifecycle を開始し、以前の key を破棄します。</summary>
    public bool Begin(AttackId attackId, long frameUpdateNumber)
    {
        if (!attackId.IsValid || frameUpdateNumber < 0)
        {
            return false;
        }

        Reset();
        _attackId = attackId;
        _startFrame = frameUpdateNumber;
        _started = true;
        return true;
    }

    /// <summary>指定 component の発行権を一度だけ予約します。</summary>
    /// <remarks>予約後の port 失敗も key を消費したままにし、同じ component を再発動しません。</remarks>
    public bool TryReserve(int componentIndex)
    {
        if (!_started || componentIndex < 0)
        {
            return false;
        }

        CommandOneShotKey key = new(CommandComponentKind.Attack, _attackId, componentIndex, _startFrame);
        for (int index = 0; index < _count; index++)
        {
            if (_keys[index] == key)
            {
                return false;
            }
        }

        if (_count >= _keys.Length)
        {
            return false;
        }

        _keys[_count++] = key;
        return true;
    }

    /// <summary>指定 component が既に予約済みかどうかを取得します。</summary>
    public bool Contains(int componentIndex)
    {
        if (!_started || componentIndex < 0)
        {
            return false;
        }

        CommandOneShotKey key = new(CommandComponentKind.Attack, _attackId, componentIndex, _startFrame);
        for (int index = 0; index < _count; index++)
        {
            if (_keys[index] == key)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>現在の攻撃 lifecycle を終了して key を破棄します。</summary>
    public void Reset()
    {
        _count = 0;
        _attackId = default;
        _startFrame = -1;
        _started = false;
    }
}
