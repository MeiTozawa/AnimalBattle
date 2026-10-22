using Flecs.NET.Core;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の対話状態およびアニメーションタイマーを保持するコンポーネント。
/// </summary>
public struct StartUiState {
    public Entity CatPreviewEntity;
    public Entity BunnyPreviewEntity;
    public float CatStepTimer;
    public float BunnyStepTimer;
    public float Boss1FlyTimer;
    public float Boss2SlamTimer;
    public bool IsBoss1Hovered;
    public bool IsBoss2Hovered;
    public bool IsConfirmHovered;
    public int Boss1TextureHandle;
    public int Boss2TextureHandle;
}
