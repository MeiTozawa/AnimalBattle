using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
///     <see cref="Color" /> の JSON シリアライズ/デシリアライズをサポートするコンバーター。
///     数値（uint）または文字列（16進数コード "#RRGGBB" 等）を透過的に解釈します。
/// </summary>
public sealed class JsonColorConverter : JsonConverter<Color> {
    /// <inheritdoc />
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetUInt32(out var uval)) return uval.ToColor();

            var lval = reader.GetInt64();
            return ((uint)lval).ToColor();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str)) return str.ToColor();
        }

        return Color.Empty;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((uint)value.ToArgb());
    }
}

/// <summary>
///     Release ビルド時に単一のバイナリファイルとしてパッケージングされる全設定バンドル。
/// </summary>
public sealed class BakedConfigBundle {
    /// <summary>プレイヤー設定 (後方互換)</summary>
    public PlayerConfig Player { get; set; } = new();

    /// <summary>Cat (猫) プレイヤー設定</summary>
    public CatConfig Cat { get; set; } = new();

    /// <summary>Bunny (ウサギ) プレイヤー設定</summary>
    public BunnyConfig Bunny { get; set; } = new();

    /// <summary>Boss 1 設定</summary>
    public Boss1Config Boss1 { get; set; } = new();

    /// <summary>Boss 2 設定</summary>
    public Boss2Config Boss2 { get; set; } = new();

    /// <summary>Boss 1 弾幕・技能パターン設定</summary>
    public Boss1PatternConfig Boss1Pattern { get; set; } = new();

    /// <summary>Boss 2 近接・技能パターン設定</summary>
    public Boss2PatternConfig Boss2Pattern { get; set; } = new();

    /// <summary>特殊弾幕変調設定</summary>
    public SpecialBulletConfig SpecialBullet { get; set; } = new();

    /// <summary>アリーナ足場設定</summary>
    public ArenaConfig Arena { get; set; } = new();

    /// <summary>描画・カメラ・UI 設定</summary>
    public RenderConfig Render { get; set; } = new();

    /// <summary>カメラ・画面振動設定</summary>
    public CameraConfig Camera { get; set; } = new();

    /// <summary>タイルセット設定</summary>
    public TileSetConfig TileSet { get; set; } = new();

    /// <summary>戦闘原型設定 (combat.prefab.toml)</summary>
    public CombatPrefabConfig CombatPrefab { get; set; } = new();

    /// <summary>Cat プレイヤー原型設定 (cat.prefab.toml)</summary>
    public CatPrefabConfig CatPrefab { get; set; } = new();

    /// <summary>Bunny プレイヤー原型設定 (bunny.prefab.toml)</summary>
    public BunnyPrefabConfig BunnyPrefab { get; set; } = new();

    /// <summary>Boss 1 原型設定 (boss1.prefab.toml)</summary>
    public Boss1PrefabConfig Boss1Prefab { get; set; } = new();

    /// <summary>Boss 2 原型設定 (boss2.prefab.toml)</summary>
    public Boss2PrefabConfig Boss2Prefab { get; set; } = new();

    /// <summary>スタート・選人シーン設定 (start_scene.toml)</summary>
    public StartSceneConfig StartScene { get; set; } = new();
}

/// <summary>
///     Native AOT 互換の JSON シリアライザーコンテキスト。
///     リフレクションを使用せず、コンパイル時にコード生成された高速なパーサーを提供します。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(JsonColorConverter), typeof(JsonStringEnumConverter<EaseType>)])]
[JsonSerializable(typeof(BakedConfigBundle))]
[JsonSerializable(typeof(PlayerConfig))]
[JsonSerializable(typeof(CatConfig))]
[JsonSerializable(typeof(BunnyConfig))]
[JsonSerializable(typeof(Boss1Config))]
[JsonSerializable(typeof(Boss1PhaseFsmConfig))]
[JsonSerializable(typeof(AttackRuleConfig))]
[JsonSerializable(typeof(Boss2Config))]
[JsonSerializable(typeof(Boss2PhaseConfig))]
[JsonSerializable(typeof(PhaseTransitionAbilityConfig))]
[JsonSerializable(typeof(Boss1PatternConfig))]
[JsonSerializable(typeof(Boss2PatternConfig))]
[JsonSerializable(typeof(DiveRamConfig))]
[JsonSerializable(typeof(Aimed3ShotConfig))]
[JsonSerializable(typeof(RingBurstConfig))]
[JsonSerializable(typeof(DoubleSpiralConfig))]
[JsonSerializable(typeof(RainDropConfig))]
[JsonSerializable(typeof(CrossChaosConfig))]
[JsonSerializable(typeof(FastSniperConfig))]
[JsonSerializable(typeof(HeavySlashConfig))]
[JsonSerializable(typeof(GroundStompConfig))]
[JsonSerializable(typeof(LeapSmashConfig))]
[JsonSerializable(typeof(DoubleCleaveConfig))]
[JsonSerializable(typeof(FrenzyChargeConfig))]
[JsonSerializable(typeof(EarthquakeRoarConfig))]
[JsonSerializable(typeof(SkillEventConfig))]
[JsonSerializable(typeof(SpecialBulletConfig))]
[JsonSerializable(typeof(ArenaConfig))]
[JsonSerializable(typeof(CombatPrefabConfig))]
[JsonSerializable(typeof(PlayerPrefabConfig))]
[JsonSerializable(typeof(CatPrefabConfig))]
[JsonSerializable(typeof(BunnyPrefabConfig))]
[JsonSerializable(typeof(BossPrefabConfig))]
[JsonSerializable(typeof(Boss1PrefabConfig))]
[JsonSerializable(typeof(Boss2PrefabConfig))]

[JsonSerializable(typeof(TileMapConfig))]
[JsonSerializable(typeof(RenderConfig))]
[JsonSerializable(typeof(PlayerRenderConfig))]
[JsonSerializable(typeof(CameraConfig))]
[JsonSerializable(typeof(TileSetConfig))]
[JsonSerializable(typeof(AnimationClipConfig))]
[JsonSerializable(typeof(AnimationConfig))]
[JsonSerializable(typeof(StartSceneConfig))]
[JsonSerializable(typeof(StartSceneLayoutConfig))]
[JsonSerializable(typeof(CardLayoutConfig))]
[JsonSerializable(typeof(StartSceneColorsConfig))]
[JsonSerializable(typeof(StartSceneTextsConfig))]
[JsonSerializable(typeof(StartSceneCardTextConfig))]
[JsonSerializable(typeof(StartSceneMotionConfig))]
[JsonSerializable(typeof(EaseType))]
[JsonSerializable(typeof(Vec2f))]
[JsonSerializable(typeof(Vec2i))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext {
}
