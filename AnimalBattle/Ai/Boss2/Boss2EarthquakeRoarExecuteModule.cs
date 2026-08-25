namespace AnimalBattle.Ai.Boss2;

/// <summary>EarthquakeRoar の Execute State_Module です。</summary>
/// <remarks>Boss2 設定と共有実行値を注入します。</remarks>
public sealed class Boss2EarthquakeRoarExecuteModule(IBoss2Configuration configuration, Boss2BehaviorState state) : Boss2ExecuteModuleBase(Boss2StateIds.EarthquakeRoarExecute, configuration, state)
{
}
