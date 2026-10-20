using System;
using Flecs.NET.Core;

namespace AnimalBattle.Core;

/// <summary>
///     Flecs の遅延実行境界（DeferBegin / DeferEnd）を管理する RAII スコープ構造体。
/// </summary>
public readonly ref struct DeferScope : IDisposable
{
    private readonly World _world;

    public DeferScope(World world)
    {
        _world = world;
        _world.DeferBegin();
    }

    public void Dispose()
    {
        _world.DeferEnd();
    }
}
