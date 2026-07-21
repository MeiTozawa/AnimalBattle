using System;

namespace AnimalBattle;

/// <summary>
/// プレイヤーキャラクターの基本情報およびコンポーネントを保持するクラス。
/// </summary>
public class Character
{
    /// <summary>
    /// プレイヤーのインデックス（1 または 2）を取得します。
    /// </summary>
    public int PlayerIndex { get; }

    /// <summary>
    /// キャラクターの表示名を取得または設定します。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// メイン描画カラー（ARGB）を取得または設定します。
    /// </summary>
    public uint MainColor { get; set; }

    /// <summary>
    /// 枠線描画カラー（ARGB）を取得または設定します。
    /// </summary>
    public uint OutlineColor { get; set; }

    /// <summary>
    /// Native C++ 物理エンジン上のオブジェクトIDを取得または設定します。
    /// </summary>
    public int PhysicsObjectId { get; set; } = -1;

    /// <summary>
    /// キャラクターの状態遷移マシンを取得します。
    /// </summary>
    public CharacterStateMachine StateMachine { get; } = new();

    /// <summary>
    /// キャラクターの物理・移動設定パラメータを取得または設定します。
    /// </summary>
    public CharacterConfig Config { get; set; } = new();

    /// <summary>
    /// 指定された初期値で新しい <see cref="Character"/> インスタンスを初期化します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="name">キャラクター名</param>
    /// <param name="mainColor">メインカラー (ARGB)</param>
    /// <param name="outlineColor">枠線カラー (ARGB)</param>
    public Character(int playerIndex, string name, uint mainColor, uint outlineColor)
    {
        PlayerIndex = playerIndex;
        Name = name;
        MainColor = mainColor;
        OutlineColor = outlineColor;
        Config.SyncFromConfig();
    }
}
