using System;

namespace AnimalBattle.Components;
/// <summary>
/// Entity にアタッチされた際、必須（依存）となる Component の自動補完定義を行う基底属性クラス。
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
public class RequireAttribute : Attribute
{
    /// <summary>必須依存コンポーネントの型</summary>
    public Type RequiredType { get; }

    /// <summary>
    /// 指定された必須コンポーネント型で <see cref="RequireAttribute"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="requiredType">必須コンポーネント型</param>
    public RequireAttribute(Type requiredType)
    {
        RequiredType = requiredType;
    }
}
