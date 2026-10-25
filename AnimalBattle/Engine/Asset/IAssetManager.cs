namespace AnimalBattle.Asset;

using AnimalBattle.Configs;
using AnimalBattle.Core;

/// <summary>
/// キャラクタースプライトや各種テクスチャのロード・保持・解放を統括するアセットサービス。
/// </summary>
public interface IAssetManager : IDisposable
{
    /// <summary>キャラクタースプライトがロード済みかどうかを取得します。</summary>
    bool HasCharacterSprite { get; }

    /// <summary>スプライトシート画像ロードおよび初期化を実行します。</summary>
    void Initialize();

    /// <summary>ロード済みの全テクスチャ・スプライトを破棄し、状態をリセットします。</summary>
    void Shutdown();

    /// <summary>テクスチャをロードし、ハンドルをキャッシュして返します。</summary>
    int LoadTexture(string relativePath);

    /// <summary>キャッシュ済みのテクスチャハンドルの取得を試行します。</summary>
    bool TryGetTexture(string relativePath, out int handle);

    /// <summary>画像サイズを手動キャッシュ登録します。</summary>
    void CacheGraphSize(int handle, Vec2i size);

    /// <summary>キャッシュ済みのテクスチャ画像サイズの取得を試行します。</summary>
    bool TryGetGraphSize(int handle, out Vec2i size);

    /// <summary>キャッシュ済みのテクスチャ画像サイズを取得します。</summary>
    Vec2i GetGraphSize(int handle);

    /// <summary>キャラクタースプライトハンドルの取得を試行します。</summary>
    bool TryGetCharacterSpriteHandle(CharacterType type, StringId clipId, out int handle);

    /// <summary>キャラクターアニメーション設定の取得を試行します。</summary>
    bool TryGetCharacterConfig(CharacterType type, out AnimationConfig config);

    /// <summary>アニメーションクリップ識別子と再生経過時間からスプライトシート上の切り出し矩形 UV SrcRect を計算します。</summary>
    Rect CalculateSrcRect(AnimationConfig config, StringId clipId, float animTimer, out bool isFinished);
}
