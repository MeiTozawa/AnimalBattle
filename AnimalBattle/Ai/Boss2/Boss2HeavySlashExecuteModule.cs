namespace AnimalBattle.Ai.Boss2;

/// <summary>HeavySlash の Execute State_Module です。</summary>
public sealed class Boss2HeavySlashExecuteModule : Boss2ExecuteModuleBase
{
    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2HeavySlashExecuteModule(IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(Boss2StateIds.HeavySlashExecute, configuration, state)
    {
    }
}
