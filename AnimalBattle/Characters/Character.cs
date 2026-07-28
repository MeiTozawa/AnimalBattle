using System;
using AnimalBattle.Core;

namespace AnimalBattle.Characters;

/// <summary>
/// プレイヤーキャラクターの基本情報およびコンポーネント（物理、Hitbox、Hurtbox、ステートマシン）を保持するコンポジションクラス。
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
    /// キャラクターの喰らい判定領域（Hurtbox）を取得します。
    /// </summary>
    public Hurtbox Hurtbox { get; }

    /// <summary>
    /// キャラクターの攻撃判定領域（Hitbox）を取得します。
    /// </summary>
    public Hitbox Hitbox { get; }

    /// <summary>
    /// キャラクターが左を向いているかどうかの向き状態を取得または設定します。
    /// </summary>
    public bool FacingLeft { get; set; }

    /// <summary>
    /// キャラクターの状態遷移マシンを取得します。
    /// </summary>
    public CharacterStateMachine StateMachine { get; } = new();

    /// <summary>
    /// キャラクターの設定コンテナを取得または設定します。
    /// </summary>
    public ConfigBase<CharacterConfig> ConfigHolder { get; set; }

    /// <summary>
    /// キャラクターの物理・移動設定パラメータを取得します。
    /// </summary>
    public CharacterConfig Config => ConfigHolder.Config;

    /// <summary>
    /// 指定された初期値で新しい <see cref="Character"/> インスタンスを生成します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="name">キャラクター名</param>
    /// <param name="mainColor">メインカラー (ARGB)</param>
    /// <param name="outlineColor">枠線カラー (ARGB)</param>
    /// <param name="configPath">読み込む TOML 設定ファイルパス（省略時は "character.toml"）</param>
    public Character(int playerIndex, string name, uint mainColor, uint outlineColor, string configPath = "character.toml")
    {
        PlayerIndex = playerIndex;
        Name = name;
        MainColor = mainColor;
        OutlineColor = outlineColor;
        FacingLeft = (playerIndex == 2); // P2 はデフォルトで左向き

        var configResult = CharacterConfig.LoadFromFile(configPath);
        ConfigHolder = configResult.IsOk ? configResult.Value! : new ConfigBase<CharacterConfig>();

        // コンポジション：TOML設定パラメータに基づく Hurtbox と Hitbox の動的生成
        Hurtbox = new Hurtbox(
            ownerIndex: playerIndex,
            offsetX: Config.HurtboxOffsetX,
            offsetY: Config.HurtboxOffsetY,
            width: Config.HurtboxWidth,
            height: Config.HurtboxHeight);

        Hitbox = new Hitbox(
            ownerIndex: playerIndex,
            offsetX: Config.HitboxOffsetX,
            offsetY: Config.HitboxOffsetY,
            width: Config.HitboxWidth,
            height: Config.HitboxHeight,
            damage: Config.HitboxDamage,
            knockbackForce: Config.HitboxKnockbackForce,
            knockbackAngle: Config.HitboxKnockbackAngle);
    }
}
