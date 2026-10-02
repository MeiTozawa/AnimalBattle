using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;

namespace AnimalBattle.Maps.Data;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(LevelData))]
[JsonSerializable(typeof(TileLayerData))]
internal sealed partial class LevelJsonContext : JsonSerializerContext
{
}

/// <summary>
///     外部 JSON 形式のレベルデータをパースし、型安全な <see cref="LevelData" /> インスタンスを構築するローダー。
/// </summary>
public static class LevelDataLoader
{
    /// <summary>
    ///     JSON 文字列からレベルデータを読み込み、検証を実行します。
    /// </summary>
    /// <param name="json">JSON 文字列</param>
    /// <returns>読み込まれた <see cref="LevelData" /> または検証エラー</returns>
    public static ErrorOr<LevelData> LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Error.Validation("LevelData.EmptyJson", "レベルデータ JSON 文字列が空です。");
        }

        try
        {
            var data = JsonSerializer.Deserialize(json, LevelJsonContext.Default.LevelData);
            if (data == null || data.Width <= 0 || data.Height <= 0 || data.TileWidth <= 0 || data.TileHeight <= 0)
            {
                return Error.Validation("LevelData.InvalidDimensions", "レベルサイズまたはタイルサイズが不正です。正数を指定してください。");
            }

            return data;
        }
        catch (Exception ex)
        {
            return Error.Failure("LevelData.ParseFailure", $"レベルデータ JSON のパースに失敗しました: {ex.Message}");
        }
    }
}
