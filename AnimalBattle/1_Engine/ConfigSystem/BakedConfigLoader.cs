namespace AnimalBattle.Configs;

using System.Diagnostics;
using ErrorOr;

/// <summary>
/// Release ビルド向けにベイク済みバイナリバンドルから設定を抽出するローダー。
/// </summary>
/// <typeparam name="T">対象の設定クラスの型</typeparam>
public sealed class BakedConfigLoader<T> : IConfigLoader<T> where T : class
{
    private readonly Func<BakedConfigBundle, T> _selector;

    /// <summary>
    /// <see cref="BakedConfigLoader{T}" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="selector">バンドルから該当設定を抽出するセレクター関数</param>
    public BakedConfigLoader(Func<BakedConfigBundle, T> selector)
    {
        Debug.Assert(selector != null, "セレクター関数は null にできません。");
        _selector = selector;
    }

    /// <inheritdoc />
    public ErrorOr<T> LoadInitial()
    {
        var bundleResult = BinaryConfigLoader.Load();
        if (bundleResult.IsError)
        {
            return bundleResult.FirstError;
        }

        var selected = _selector(bundleResult.Value);
        Debug.Assert(selected != null, $"{typeof(T).Name} のベイク設定が null です。");

        if (selected is IConfigLifecycle lifecycle)
        {
            var valid = lifecycle.Validate();
            if (valid.IsError) return valid.FirstError;
            lifecycle.PostProcess();
        }

        return selected;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
