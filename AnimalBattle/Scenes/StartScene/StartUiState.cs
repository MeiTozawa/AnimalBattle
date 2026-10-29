namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の対話状態およびアニメーションタイマーを保持するコンポーネント。
/// </summary>
public struct StartUiState {
    public Entity CatPreviewEntity;
    public Entity BunnyPreviewEntity;
    public float CatStepTimer;
    public float BunnyStepTimer;
    public float BossSnakeFlyTimer;
    public float BossLionSlamTimer;
    public bool IsBossSnakeHovered;
    public bool IsBossLionHovered;
    public bool IsConfirmHovered;
    public int BossSnakeTextureHandle;
    public int BossLionTextureHandle;
}
