using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
/// Release ビルド時に Resource/baked_configs.bin から圧縮された設定データを読み込み、
/// AOT 互換の JSON ソースジェネレーター経由で復元するバイナリ設定ローダー。
/// </summary>
public static class BinaryConfigLoader
{
    private const uint ExpectedMagic = 0x46434241; // "ABCF" in little-endian
    private const uint ExpectedVersion = 1;

    /// <summary>
    /// バイナリ設定ファイルから <see cref="BakedConfigBundle"/> を復元します。
    /// 外部ファイルが存在する場合はそれを優先し、存在しない場合は埋め込みリソースから読み込みます。
    /// </summary>
    /// <param name="overridePath">明示的なバイナリファイルパス（省略時は自動探索）。</param>
    /// <param name="resolver">探索に使用するパスリゾルバー（省略時は標準リゾルバー）。</param>
    /// <returns>読み込み結果を含む <see cref="ErrorOr{BakedConfigBundle}"/></returns>
    public static ErrorOr<BakedConfigBundle> Load(string? overridePath = null, IConfigPathResolver? resolver = null)
    {
        try
        {
            resolver ??= ConfigPathResolver.Instance;
            Stream? stream = resolver.OpenBinaryStream(overridePath);
            if (stream == null)
            {
                return Error.NotFound("Config.BinaryNotFound", "バイナリ設定ファイル (baked_configs.bin) が見つかりません。");
            }

            using (stream)
            {
                uint magic;
                uint version;
                uint uncompressedSize;
                uint compressedSize;

                using (BinaryReader reader = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    magic = reader.ReadUInt32();
                    if (magic != ExpectedMagic)
                    {
                        return Error.Validation("Config.InvalidMagic", $"バイナリ設定のマジックナンバーが不正です (0x{magic:X8})。");
                    }

                    version = reader.ReadUInt32();
                    if (version != ExpectedVersion)
                    {
                        return Error.Validation("Config.UnsupportedVersion", $"サポートされていないバイナリ設定バージョンです: {version}");
                    }

                    uncompressedSize = reader.ReadUInt32();
                    compressedSize = reader.ReadUInt32();
                }

                using ZLibStream decompressor = new(stream, CompressionMode.Decompress);
                BakedConfigBundle? bundle = JsonSerializer.Deserialize(
                    decompressor,
                    ConfigJsonContext.Default.BakedConfigBundle);

                if (bundle == null)
                {
                    return Error.Failure("Config.DeserializationFailed", "バイナリ設定データのデシリアライズ結果が null です。");
                }

                return bundle;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Config.Exception", $"バイナリ設定の読み込み中に例外が発生しました: {ex.Message}");
        }
    }
}
