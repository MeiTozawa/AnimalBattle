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

    /// <summary>
    ///     指定したイベント型のハンドラーを登録します。
    /// </summary>
    public static void Subscribe(Action<T> handler)
    {
        _handlers += handler;
    }

    /// <summary>
    ///     指定したイベント型のハンドラーの登録を解除します。
    /// </summary>
    public static void Unsubscribe(Action<T> handler)
    {
        _handlers -= handler;
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
        _handlers = null;
    }
}

/// <summary>
///     イベントバスのコンビニエンスファサード。
/// </summary>
public static class EventBus
{
    /// <summary>
    ///     イベントを発行します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish<T>(in T evt) where T : struct
    {
        EventBus<T>.Publish(in evt);
    }

    /// <summary>
    ///     イベントハンドラーを購読します。
    /// </summary>
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        EventBus<T>.Subscribe(handler);
    }

    /// <summary>
    ///     イベントハンドラーの購読を解除します。
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        EventBus<T>.Unsubscribe(handler);
    }
}
