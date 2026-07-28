using System;
using System.IO;
using AnimalBattle.Core;

namespace AnimalBattle.Animations;

/// <summary>
/// アニメーション・スプライトアセットのロード、クリップ初期化、更新処理、および描画を統括するマネージャークラス。
/// </summary>
public sealed class AnimationManager : IDisposable
{
    private readonly AnimationPlayer _p1AnimPlayer = new();
    private readonly AnimationPlayer _p2AnimPlayer = new();
    private readonly AnimationPlayer _p1EffectPlayer = new();
    private readonly AnimationPlayer _p2EffectPlayer = new();

    private int _catSpriteHandle = -1;
    private int _effectSpriteHandle = -1;
    private bool _isDisposed;

    /// <summary>
    /// 猫キャラクターのスプライトハンドルを取得します。
    /// </summary>
    public int CatSpriteHandle => _catSpriteHandle;

    /// <summary>
    /// 攻撃エフェクトのスプライトハンドルを取得します。
    /// </summary>
    public int EffectSpriteHandle => _effectSpriteHandle;

    /// <summary>
    /// 猫キャラクターのスプライト画像が正常にロードされているかを取得します。
    /// </summary>
    public bool HasCatSprite => _catSpriteHandle != -1;

    /// <summary>
    /// 攻撃エフェクトのスプライト画像が正常にロードされているかを取得します。
    /// </summary>
    public bool HasEffectSprite => _effectSpriteHandle != -1;

    /// <summary>
    /// デストラクタ。
    /// </summary>
    ~AnimationManager()
    {
        Dispose(false);
    }

    /// <summary>
    /// 画像アセットのロードおよび各プレイヤー・エフェクトの animation クリップ初期化を行います。
    /// </summary>
    public void Initialize()
    {
        // 子猫スプライトのロード
        string spritePath = @"resource\Cat Sprite Sheet.png";
        if (!File.Exists(spritePath))
        {
            string altPath = Path.Combine("..", "resource", "Cat Sprite Sheet.png");
            if (File.Exists(altPath))
            {
                spritePath = Path.GetFullPath(altPath);
            }
        }
        _catSpriteHandle = AssetManager.LoadGraph(spritePath);
        Console.WriteLine($"[子猫スプライトロード] パス: '{spritePath}' (存在: {File.Exists(spritePath)}) -> ハンドル: {_catSpriteHandle}");

        // 攻撃エフェクトスプライトのロード
        string effectPath = @"resource\sword & shield effect sprites.png";
        if (!File.Exists(effectPath))
        {
            string altPath = Path.Combine("..", "resource", "sword & shield effect sprites.png");
            if (File.Exists(altPath))
            {
                effectPath = Path.GetFullPath(altPath);
            }
        }
        _effectSpriteHandle = AssetManager.LoadGraph(effectPath);
        Console.WriteLine($"[攻撃エフェクトスプライト] パス: '{effectPath}' (存在: {File.Exists(effectPath)}) -> ハンドル: {_effectSpriteHandle}");

        if (HasCatSprite)
        {
            SetupCatClips(_p1AnimPlayer);
            SetupCatClips(_p2AnimPlayer);
        }

        if (HasEffectSprite)
        {
            SetupEffectClips(_p1EffectPlayer);
            SetupEffectClips(_p2EffectPlayer);
        }
    }

    /// <summary>
    /// 全プレイヤーおよびエフェクトのアニメーション状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(float deltaTime)
    {
        _p1AnimPlayer.Update(deltaTime);
        _p2AnimPlayer.Update(deltaTime);
        _p1EffectPlayer.Update(deltaTime);
        _p2EffectPlayer.Update(deltaTime);
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターのアニメーション再生を開始します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="animName">再生するアニメーション名</param>
    /// <param name="restartIfSame">同一アニメーション再生中にも最初から再開するか</param>
    /// <returns>再生開始に成功した場合は true</returns>
    public bool PlayCharacterAnimation(int playerIndex, string animName, bool restartIfSame = false)
    {
        AnimationPlayer player = GetCharacterAnimPlayer(playerIndex);
        return player.Play(animName, restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーのエフェクトアニメーション再生を開始します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="effectName">再生するエフェクト名</param>
    /// <param name="restartIfSame">同一エフェクト再生中にも最初から再開するか</param>
    /// <returns>再生開始に成功した場合は true</returns>
    public bool PlayEffectAnimation(int playerIndex, string effectName, bool restartIfSame = false)
    {
        AnimationPlayer player = GetEffectAnimPlayer(playerIndex);
        return player.Play(effectName, restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターのスプライトを描画します。
    /// </summary>
    /// <param name="render">レンダリングサービス</param>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="position">描画中心位置</param>
    /// <param name="facingLeft">左向きの場合は true</param>
    /// <param name="scale">表示倍率</param>
    public void DrawCharacter(RenderService render, int playerIndex, Point<int> position, bool facingLeft, float scale = 2.5f)
    {
        if (!HasCatSprite)
        {
            return;
        }

        AnimationPlayer player = GetCharacterAnimPlayer(playerIndex);
        player.Draw(render, position, _catSpriteHandle, flipX: facingLeft, scale: scale);
    }

    /// <summary>
    /// 指定されたプレイヤーの攻撃エフェクトスプライトを描画します。
    /// </summary>
    /// <param name="render">レンダリングサービス</param>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <param name="position">描画中心位置</param>
    /// <param name="facingLeft">左向きの場合は true</param>
    /// <param name="scale">表示倍率</param>
    public void DrawEffect(RenderService render, int playerIndex, Point<int> position, bool facingLeft, float scale = 2.0f)
    {
        if (!HasEffectSprite)
        {
            return;
        }

        AnimationPlayer player = GetEffectAnimPlayer(playerIndex);
        player.Draw(render, position, _effectSpriteHandle, flipX: facingLeft, scale: scale);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void SetupCatClips(AnimationPlayer player)
    {
        player.AddGridClip("Idle", row: 0, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 6.0f, isLooping: true);
        player.AddGridClip("Walk", row: 1, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 8.0f, isLooping: true);
        player.AddGridClip("Run", row: 2, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 10.0f, isLooping: true);
        player.AddGridClip("Rest", row: 3, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 5.0f, isLooping: true);
        player.AddGridClip("Jump", row: 4, frameWidth: 32, frameHeight: 32, frameCount: 8, fps: 10.0f, isLooping: true);
        player.AddGridClip("Attack", row: 5, frameWidth: 32, frameHeight: 32, frameCount: 8, fps: 20.0f, isLooping: false);
        player.AddGridClip("Hurt", row: 6, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 8.0f, isLooping: false);
        player.AddGridClip("Die", row: 7, frameWidth: 32, frameHeight: 32, frameCount: 6, fps: 6.0f, isLooping: false);
        player.AddGridClip("Meow", row: 8, frameWidth: 32, frameHeight: 32, frameCount: 7, fps: 8.0f, isLooping: false);
        player.AddGridClip("Spin", row: 9, frameWidth: 32, frameHeight: 32, frameCount: 8, fps: 12.0f, isLooping: true);
        player.Play("Idle");
    }

    private void SetupEffectClips(AnimationPlayer player)
    {
        player.AddGridClip("Slash", row: 0, frameWidth: 32, frameHeight: 32, frameCount: 5, fps: 20.0f, isLooping: false);
        player.AddGridClip("Hit", row: 1, frameWidth: 32, frameHeight: 32, frameCount: 4, fps: 16.0f, isLooping: false);
    }

    private AnimationPlayer GetCharacterAnimPlayer(int playerIndex)
    {
        return playerIndex switch
        {
            1 => _p1AnimPlayer,
            2 => _p2AnimPlayer,
            _ => throw new ArgumentOutOfRangeException(nameof(playerIndex), $"無効なプレイヤーインデックス: {playerIndex}")
        };
    }

    private AnimationPlayer GetEffectAnimPlayer(int playerIndex)
    {
        return playerIndex switch
        {
            1 => _p1EffectPlayer,
            2 => _p2EffectPlayer,
            _ => throw new ArgumentOutOfRangeException(nameof(playerIndex), $"無効なプレイヤーインデックス: {playerIndex}")
        };
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _p1AnimPlayer.Dispose();
                _p2AnimPlayer.Dispose();
                _p1EffectPlayer.Dispose();
                _p2EffectPlayer.Dispose();
            }

            _isDisposed = true;
        }
    }
}
