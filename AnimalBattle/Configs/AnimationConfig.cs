using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// スプライトシート単位のアニメーション設定クラス。
/// セル寸法およびクリップ一覧を保持します。
/// </summary>
public sealed class AnimationConfig
{
    private Dictionary<StringId, AnimationClipConfig>? _clipLookup;

    /// <summary>キャラクター/エフェクトの表示名</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>デフォルトのセル幅（ピクセル）</summary>
    public int CellWidth { get; set; }

    /// <summary>デフォルトのセル高さ（ピクセル）</summary>
    public int CellHeight { get; set; }

    /// <summary>クリップ一覧</summary>
    public List<AnimationClipConfig> Clips { get; set; } = [];

    /// <summary>エフェクト専用クリップ一覧（後方互換プロパティ）</summary>
    public List<AnimationClipConfig> EffectClips { get; set; } = [];

    /// <summary>
    /// <see cref="StringId"/> をキーとしてキャッシュされたクリップ設定を O(1) で検索します。
    /// </summary>
    /// <param name="clipId">検索対象のクリップ識別子。</param>
    /// <returns>合致するクリップ設定、未登録時は null。</returns>
    public AnimationClipConfig? FindClip(StringId clipId)
    {
        _clipLookup ??= BuildClipLookup();
        return _clipLookup.GetValueOrDefault(clipId);
    }

    private Dictionary<StringId, AnimationClipConfig> BuildClipLookup()
    {
        Dictionary<StringId, AnimationClipConfig> lookup = new(Clips.Count);
        for (int i = 0; i < Clips.Count; i++)
        {
            AnimationClipConfig clip = Clips[i];
            if (!string.IsNullOrEmpty(clip.Name))
            {
                lookup[clip.Name] = clip;
            }
        }
        return lookup;
    }
}
