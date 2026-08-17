namespace AnimalBattle.Components;

/// <summary>
/// プレイヤーの発射した銃弾エンティティを識別するためのタグ構造体。
/// </summary>
public struct PlayerBulletTag
{
}

/// <summary>
/// Boss から発射された敵弾エンティティを識別するためのタグ構造体。
/// </summary>
public struct BossBulletTag
{
}

/// <summary>
/// 弾幕 Boss エンティティを識別するためのタグ構造体。
/// </summary>
[Require<Transform>]
[Require<Velocity>]
[Require<CollisionBox>]
[Require<BossStats>]
[Require<BossStateMachine>]
public struct BossTag
{
}

/// <summary>
/// バトル全体のゲーム進行状態（ゲームオーバー、勝利判定）を保持するシングルトンコンポーネント。
/// </summary>
public struct GameState
{
    /// <summary>プレイヤーが死亡しゲームオーバー中かどうかのフラグ</summary>
    public bool IsGameOver;

    /// <summary>Boss を撃破し勝利したかどうかのフラグ</summary>
    public bool IsVictory;

    /// <summary>
    /// <see cref="GameState"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="isGameOver">ゲームオーバー状態</param>
    /// <param name="isVictory">勝利状態</param>
    public GameState(bool isGameOver = false, bool isVictory = false)
    {
        IsGameOver = isGameOver;
        IsVictory = isVictory;
    }
}
