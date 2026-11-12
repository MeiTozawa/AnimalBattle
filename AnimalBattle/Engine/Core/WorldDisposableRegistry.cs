namespace AnimalBattle.Core;

using System;
using System.Collections.Generic;
using Flecs.NET.Core;

/// <summary>
///     Flecs ワールドに紐づく <see cref="IDisposable"/> システムコンテキストおよびネイティブリソースを一括管理・解放するレジストリ。
///     各モジュール・システムが登録時に自身のコンテキストを登録することで、GameManager 側での具象型ハードコードを排除します。
/// </summary>
public sealed class WorldDisposableRegistry : IDisposable
{
    private readonly List<IDisposable> _disposables = new(32);
    private bool _isDisposed;

    /// <summary>
    ///     破棄対象のリソースを登録します。
    /// </summary>
    /// <typeparam name="T">破棄可能なコンテキスト型</typeparam>
    /// <param name="disposable">登録するインスタンス</param>
    /// <returns>登録されたインスタンス</returns>
    public T Register<T>(T disposable) where T : IDisposable
    {
        if (!_isDisposed && !_disposables.Contains(disposable))
        {
            _disposables.Add(disposable);
        }

        return disposable;
    }

    /// <summary>
    ///     登録された全リソースを登録の逆順で安全に破棄します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            _disposables[i].Dispose();
        }

        _disposables.Clear();
        _isDisposed = true;
    }
}

/// <summary>
///     Flecs <see cref="World"/> に対する破棄可能コンテキスト登録用の拡張メソッド群。
/// </summary>
public static class WorldDisposableExtensions
{
    /// <summary>
    ///     指定された <see cref="IDisposable"/> コンテキストをワールドに <c>Set</c> し、ワールド破棄時の一括解放リストへ自動登録します。
    /// </summary>
    public static World SetDisposable<T>(this World world, T context) where T : class, IDisposable
    {
        if (!world.Has<WorldDisposableRegistry>())
        {
            world.Set(new WorldDisposableRegistry());
        }

        world.Get<WorldDisposableRegistry>().Register(context);
        world.Set(context);
        return world;
    }

    /// <summary>
    ///     ワールドに登録されたすべての <see cref="IDisposable"/> コンテキストを一括破棄します。
    /// </summary>
    public static void DisposeAllRegisteredContexts(this World world)
    {
        if (world.Has<WorldDisposableRegistry>())
        {
            world.Get<WorldDisposableRegistry>().Dispose();
        }
    }
}
