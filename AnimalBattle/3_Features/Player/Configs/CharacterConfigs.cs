namespace AnimalBattle.Configs;

/// <summary>
/// Cat (猫) 固有プレイヤー設定。
/// Flecs.NET の World 単例コンポーネントとして一意に登録・取得されます。
/// </summary>
public sealed class CatConfig : PlayerConfig
{
}

/// <summary>
/// Bunny (ウサギ) 固有プレイヤー設定。
/// Flecs.NET の World 単例コンポーネントとして一意に登録・取得されます。
/// </summary>
public sealed class BunnyConfig : PlayerConfig
{
}

/// <summary>
/// Cat (猫) 固有プレイヤー原型設定 (cat.prefab.toml)。
/// </summary>
public sealed class CatPrefabConfig : PlayerPrefabConfig
{
}

/// <summary>
/// Bunny (ウサギ) 固有プレイヤー原型設定 (bunny.prefab.toml)。
/// </summary>
public sealed class BunnyPrefabConfig : PlayerPrefabConfig
{
}

/// <summary>
/// Boss 1 固有原型設定 (boss1.prefab.toml)。
/// </summary>
public sealed class Boss1PrefabConfig : BossPrefabConfig
{
}

/// <summary>
/// Boss 2 固有原型設定 (boss2.prefab.toml)。
/// </summary>
public sealed class Boss2PrefabConfig : BossPrefabConfig
{
}
