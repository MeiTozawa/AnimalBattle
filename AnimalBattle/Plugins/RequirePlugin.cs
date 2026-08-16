using System;
using System.Collections.Generic;
using System.Reflection;
using Flecs.NET.Core;
using AnimalBattle.Components;

namespace AnimalBattle.Plugins;

/// <summary>
/// Bevy の Required Components 属性 (<c>[Require&lt;T&gt;]</c>) をリフレクションによりスキャンし、
/// エンティティ作成時の依存関係を Flecs.NET の <c>Add&lt;T&gt;()</c> により自動補完するコアプラグイン。
/// </summary>
public class RequirePlugin : IPlugin
{
    /// <summary>
    /// アセンブリ内の <see cref="RequireAttribute"/> 付き型を走査し、依存コンポーネントを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        World world = app.World;

        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            IEnumerable<RequireAttribute> attrs = type.GetCustomAttributes<RequireAttribute>(inherit: false);
            foreach (RequireAttribute attr in attrs)
            {
                RegisterRequiredComponent(world, type, attr.RequiredType);
            }
        }
    }

    private static void RegisterRequiredComponent(World world, Type targetType, Type requiredType)
    {
        try
        {
            MethodInfo? method = Array.Find(
                typeof(World).GetMethods(),
                m => m.Name == "Component" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 0);

            if (method != null)
            {
                MethodInfo generic = method.MakeGenericMethod(targetType);
                generic.Invoke(world, null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RequirePlugin 警告] 依存関係の登録中にエラーが発生しました: {ex.Message}");
        }
    }
}
