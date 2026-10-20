using System.Runtime.CompilerServices;

namespace AnimalBattle.Events;

/// <summary>
///     型ごとにディスパッチを直接静的バインドするゼロオーバーヘッド・型安全ゲーム内イベントバス。
///     ヒープ割り当ておよびディクショナリルックアップを排除し、高速なイベント通知を提供します。
/// </summary>
/// <typeparam name="T">イベント構造体型</typeparam>
public static class EventBus<T> where T : struct
{
    private static Action<T>? _handlers;

    static EventBus()
    {
        EventBus.RegisterCleaner(Clear);
    }

    /// <summary>
    ///     指定したイベント型のハンドラーを登録し、スコープ解除用トークンを返します。
    ///     スレッドセーフなアトミック操作により登録されます。
    /// </summary>
    public static IDisposable Subscribe(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var current = _handlers;
        while (true)
        {
            var updated = (Action<T>?)Delegate.Combine(current, handler);
            var original = Interlocked.CompareExchange(ref _handlers, updated, current);
            if (ReferenceEquals(original, current))
            {
                break;
            }
            current = original;
        }

        return new SubscriptionToken(handler);
    }

    /// <summary>
    ///     指定したイベント型のハンドラーの登録を解除します。
    ///     スレッドセーフなアトミック操作により解除されます。
    /// </summary>
    public static void Unsubscribe(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var current = _handlers;
        while (true)
        {
            var updated = (Action<T>?)Delegate.Remove(current, handler);
            var original = Interlocked.CompareExchange(ref _handlers, updated, current);
            if (ReferenceEquals(original, current))
            {
                break;
            }
            current = original;
        }
    }

    /// <summary>
    ///     イベントを発行し、登録された全リスナーへ同期通知します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish(in T evt)
    {
        _handlers?.Invoke(evt);
    }

    /// <summary>
    ///     登録された全ハンドラーを消去します。
    /// </summary>
    public static void Clear()
    {
        Interlocked.Exchange(ref _handlers, null);
    }

    private sealed class SubscriptionToken(Action<T> handler) : IDisposable {
        private Action<T>? _handler = handler;

        public void Dispose()
        {
            var handler = Interlocked.Exchange(ref _handler, null);
            if (handler != null)
            {
                Unsubscribe(handler);
            }
        }
    }
}

/// <summary>
///     イベントバスのコンビニエンスファサード。
/// </summary>
public static class EventBus
{
    private static readonly List<Action> _cleanerActions = new();
    private static readonly Lock _registryLock = new();

    internal static void RegisterCleaner(Action cleaner)
    {
        lock (_registryLock)
        {
            if (!_cleanerActions.Contains(cleaner))
            {
                _cleanerActions.Add(cleaner);
            }
        }
    }

    /// <summary>
    ///     すべてのイベント型に登録された全リスナー・ハンドラーを一括消去します。
    ///     シーン遷移時、ゲーム終了時、あるいはテスト実行時の静的委托リークを完全に防止します。
    /// </summary>
    public static void ClearAll()
    {
        lock (_registryLock)
        {
            foreach (var cleaner in _cleanerActions)
            {
                cleaner();
            }
        }
    }

    /// <summary>
    ///     指定したイベント型に登録された全ハンドラーを消去します。
    /// </summary>
    public static void Clear<T>() where T : struct
    {
        EventBus<T>.Clear();
    }

    /// <summary>
    ///     イベントを発行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish<T>(in T evt) where T : struct
    {
        EventBus<T>.Publish(in evt);
    }

    /// <summary>
    ///     イベントハンドラーを購読し、破棄時に自動 Unsubscribe するトークンを返します。
    /// </summary>
    public static IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        return EventBus<T>.Subscribe(handler);
    }

    /// <summary>
    ///     イベントハンドラーの購読を解除します。
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        EventBus<T>.Unsubscribe(handler);
    }
}
