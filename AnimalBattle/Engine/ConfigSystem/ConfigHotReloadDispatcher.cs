using AnimalBattle.Logging;
namespace AnimalBattle.Configs;

using System.Collections.Concurrent;
using System.Diagnostics;
using Flecs.NET.Core;

/// <summary>
/// バックグラウンドで検知された設定変更イベントをメインスレッドへ安全にディスパッチするパイプライン。
/// </summary>
public static class ConfigHotReloadDispatcher
{
    private static readonly ConcurrentQueue<Action<World>> _pendingActions = new();

    /// <summary>
    /// バックグラウンドスレッドからメインスレッドでの実行タスクをキューイングします。
    /// </summary>
    /// <param name="syncAction">メインスレッドで実行する同期処理</param>
    public static void Enqueue(Action<World> syncAction)
    {
        Debug.Assert(syncAction != null, "同期アクションは null にできません。");
        _pendingActions.Enqueue(syncAction);
    }

    /// <summary>
    /// メインスレッドのフレーム開始時に保留中の設定更新をすべて適用します。
    /// Release ビルド時は条件付きコンパイルにより呼び出しごと完全に除去されます。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    [Conditional("DEBUG")]
    public static void Dispatch(World world)
    {
        while (_pendingActions.TryDequeue(out var action))
        {
            try
            {
                action(world);
            }
            catch (Exception ex)
            {
                Log.Error("ConfigHotReload", $"設定ホットリロードの適用中に例外が発生しました: {ex.Message}", ex);
                Debug.Fail($"[C# 設定警告] 設定ホットリロード適用中に例外が発生しました: {ex.Message}");
            }
        }
    }
}
