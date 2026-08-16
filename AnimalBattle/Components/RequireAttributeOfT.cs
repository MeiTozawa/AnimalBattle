using System;

namespace AnimalBattle.Components;
/// <summary>
/// Bevy スタイルの必須コンポーネント依存関係を型安全に指定する属性クラス。
/// コード上では <c>[Require&lt;T&gt;]</c> として使用します。
/// </summary>
/// <typeparam name="T">必須依存コンポーネント型</typeparam>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequireAttribute<T> : RequireAttribute where T : struct
{
    /// <summary>
    /// <see cref="RequireAttribute{T}"/> の新しいインスタンスを初期化します。
    /// </summary>
    public RequireAttribute() : base(typeof(T))
    {
    }
}
