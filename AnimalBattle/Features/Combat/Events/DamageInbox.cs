namespace AnimalBattle.Combat.Events;

/// <summary>
///     ワールド常駐型被弾イベントバッファ。
///     幾何衝突判定とダメージ確定裁定を完全疎結合化し、ECS 瞬態コンポーネントによる Archetype 顛簸を根絶します。
/// </summary>
public sealed class DamageInbox
{
    public const int DefaultCapacity = 512;

    public readonly HitEvent[] Buffer = new HitEvent[DefaultCapacity];
    private int _count;

    /// <summary>本フレームで蓄積されたイベント総数。</summary>
    public int Count => _count;

    /// <summary>
    ///     バッファへ被弾イベントを無ロックで追記します。
    /// </summary>
    public void Enqueue(in HitEvent evt)
    {
        var idx = Interlocked.Increment(ref _count) - 1;
        if ((uint)idx < (uint)Buffer.Length)
        {
            Buffer[idx] = evt;
        }
    }

    /// <summary>
    ///     有効な被弾イベントの読み取り専用スパンを取得します。
    /// </summary>
    public ReadOnlySpan<HitEvent> AsSpan()
    {
        var validCount = Math.Min(_count, Buffer.Length);
        return new ReadOnlySpan<HitEvent>(Buffer, 0, validCount);
    }

    /// <summary>
    ///     毎フレームの裁定終了時にイベントカウントをゼロリセットします。
    /// </summary>
    public void Clear()
    {
        _count = 0;
    }
}
