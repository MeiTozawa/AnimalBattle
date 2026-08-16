namespace AnimalBattle.Plugins;

/// <summary>
/// 物理エンジンのサービス連携および重力・運動積分計算システムを一括構築するドメインプラグイン。
/// </summary>
public class PhysicsPlugin : IPlugin
{
    /// <summary>
    /// 物理シミュレーションシステムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddSystem(PhysicsSystems.PhysicsSystem);
    }
}
