using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// キャラクターの入力検知、物理運動、およびアニメーション状態遷移を一括構築するドメインプラグイン。
/// </summary>
public class CharacterPlugin : IPlugin
{
    /// <summary>
    /// キャラクターの入力検知、物理運動、およびアニメーション状態遷移システムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddSystem(CharacterSystems.InputSystem)
           .AddSystem(CharacterSystems.MovementSystem)
           .AddSystem(CharacterSystems.AnimationSystem);
    }
}
