namespace AnimalBattle.Configs;

using System.Diagnostics;
using ErrorOr;
using Flecs.NET.Core;

/// <summary>
/// 純粋なインメモリ設定コンテナ。読み取り専用アクセス、バージョン番号、および更新通知を管理します。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
public sealed class ConfigHolder<T> : IConfigHolder where T : class
{
    private volatile T _current;
    private int _version;

    /// <inheritdoc />
    public Type ConfigType => typeof(T);

    /// <inheritdoc />
    public object BoxedValue => Value;

    /// <inheritdoc />
    public void InjectToWorld(World world)
    {
        world.Set(Value);
    }

    /// <summary>
    /// 初期設定インスタンスで <see cref="ConfigHolder{T}" /> を初期化します。
    /// </summary>
    /// <param name="initial">初期設定インスタンス</param>
    public ConfigHolder(T initial)
    {
        Debug.Assert(initial != null, "初期設定インスタンスは null にできません。");
        _current = initial;
        if (_current is IConfigLifecycle lifecycle)
        {
            lifecycle.PostProcess();
        }
    }

    /// <summary>現在有効な設定スナップショットを取得します（ロックフリー・O(1)参照）。</summary>
    public T Value => _current;

    /// <summary>設定バージョン番号（リロード成功ごとにインクリメント）。</summary>
    public uint Version => (uint)Volatile.Read(ref _version);

    /// <summary>設定変更通知イベント。</summary>
    public event Action<T>? OnChanged;

    /// <summary>
    /// 設定インスタンスを検証し、妥当であればアトミックに更新します。
    /// </summary>
    /// <param name="newConfig">新しい設定インスタンス</param>
    /// <returns>更新成功時は Success、検証失敗時は Error</returns>
    public ErrorOr<Success> Update(T newConfig)
    {
        Debug.Assert(newConfig != null, "更新対象の設定インスタンスは null にできません。");

        if (newConfig is IConfigLifecycle lifecycle)
        {
            var validateResult = lifecycle.Validate();
            if (validateResult.IsError)
            {
                return validateResult.FirstError;
            }
            lifecycle.PostProcess();
        }

        // 検証完了後に参照をアトミックに差し替え
        _current = newConfig;
        Interlocked.Increment(ref _version);

        OnChanged?.Invoke(_current);
        return Result.Success;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        OnChanged = null;
    }
}
