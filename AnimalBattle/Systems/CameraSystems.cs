using Flecs.NET.Core;
using AnimalBattle.Components;

namespace AnimalBattle.Systems;

/// <summary>
/// カメラドメインに関する純粋システム関数（カメラトランスフォーム計算）を提供する静的クラス。
/// </summary>
public static class CameraSystems
{
    /// <summary>
    /// ターゲットエンティティのトランスフォームを追従する <see cref="Camera"/> の位置計算を担当するシステム関数。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    public static void CameraSystem(World world)
    {
        world.System<Transform, Camera>("CameraSystem")
             .Each((ref Transform trans, ref Camera camera) =>
             {
                 camera.X = trans.X;
                 camera.Y = trans.Y - 50.0f;
             });
    }
}
