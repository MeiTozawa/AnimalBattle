using System;
using AnimalBattle.Core;

namespace AnimalBattle.Characters;
/// <summary>
/// プレイヤーキャラクターの基本情報およびコンポーネント（物理、Hitbox、Hurtbox、ステートマシン）を保持するクラス。
/// </summary>
public class Character
{
    private int _teamId;
    private bool _facingLeft;

    /// <summary>
    /// プレイヤーのインデックス（1～4）を取得します。
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
    /// キャラクターの所属チーム ID を取得または設定します。
    /// 設定変更時、内部の Hurtbox および Hitbox の TeamId フィルタも動的に同期更新されます。
    /// </summary>
    public int TeamId
    {
        get => _teamId;
        set
        {
            _teamId = value;
            if (Hurtbox != null)
            {
                Hurtbox.Filter = new CollisionFilter(Hurtbox.Filter.Category, Hurtbox.Filter.Mask, Hurtbox.Filter.OwnerId, _teamId);
            }
            if (Hitbox != null)
            {
                Hitbox.Filter = new CollisionFilter(Hitbox.Filter.Category, Hitbox.Filter.Mask, Hitbox.Filter.OwnerId, _teamId);
            }
        }
    }

    /// <summary>
    /// プレイヤーの操作種別（Human / Cpu）を取得または設定します。
    /// </summary>
    public PlayerType PlayerType { get; set; }

    /// <summary>
    /// キャラクターの残りストック（残機）数を取得または設定します。
    /// </summary>
    public int StockCount { get; set; }

    /// <summary>
    /// キャラクターのスコア（撃墜数）を取得または設定します。
    /// </summary>
    public int Score { get; set; }

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
    public bool FacingLeft
    {
        get => _facingLeft;
        set => _facingLeft = value;
    }

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
    /// <param name="playerIndex">プレイヤーインデックス (1～4)</param>
    /// <param name="name">キャラクター名</param>
    /// <param name="mainColor">メインカラー (ARGB)</param>
    /// <param name="outlineColor">枠線カラー (ARGB)</param>
    /// <param name="teamId">チーム ID（負の値の場合は playerIndex と同値に設定されます）</param>
    /// <param name="playerType">操作種別（Human / Cpu）</param>
    /// <param name="stockCount">初期ストック（残機）数</param>
    /// <param name="facingLeft">初期の向き（null の場合は偶数プレイヤーが左向き）</param>
    /// <param name="configPath">読み込む TOML 設定ファイルパス（省略時は "character.toml"）</param>
    public Character(
        int playerIndex,
        string name,
        uint mainColor,
        uint outlineColor,
        int teamId = -1,
        PlayerType playerType = PlayerType.Human,
        int stockCount = 3,
        bool? facingLeft = null,
        string configPath = "character.toml")
    {
        PlayerIndex = playerIndex;
        Name = name;
        MainColor = mainColor;
        OutlineColor = outlineColor;
        _teamId = teamId >= 0 ? teamId : playerIndex;
        PlayerType = playerType;
        StockCount = stockCount;
        Score = 0;

        _facingLeft = facingLeft ?? (playerIndex % 2 == 0);

        Result<ConfigBase<CharacterConfig>> configResult = CharacterConfig.LoadFromFile(configPath);
        ConfigHolder = configResult.IsOk ? configResult.Value! : new ConfigBase<CharacterConfig>();


        Hurtbox = new Hurtbox(
            ownerIndex: playerIndex,
            offsetX: Config.HurtboxOffsetX,
            offsetY: Config.HurtboxOffsetY,
            width: Config.HurtboxWidth,
            height: Config.HurtboxHeight,
            teamId: _teamId);

        Hitbox = new Hitbox(
            ownerIndex: playerIndex,
            offsetX: Config.HitboxOffsetX,
            offsetY: Config.HitboxOffsetY,
            width: Config.HitboxWidth,
            height: Config.HitboxHeight,
            damage: Config.HitboxDamage,
            knockbackForce: Config.HitboxKnockbackForce,
            knockbackAngle: Config.HitboxKnockbackAngle,
            teamId: _teamId);
    }
}
