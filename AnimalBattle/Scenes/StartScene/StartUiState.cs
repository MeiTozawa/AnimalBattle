namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の対話状態およびアニメーションタイマーを保持するコンポーネント。
/// </summary>
public struct StartUiState
{
    /// <summary>Cat（遠距離）プレビューエンティティ。</summary>
    public Entity CatPreviewEntity;

    /// <summary>Bunny（近接）プレビューエンティティ。</summary>
    public Entity BunnyPreviewEntity;

    /// <summary>Cat ホバー演出用タイマー。</summary>
    public float CatStepTimer;

    /// <summary>Bunny ホバー演出用タイマー。</summary>
    public float BunnyStepTimer;

    /// <summary>「チュートリアル開始」ボタンのホバー状態。</summary>
    public bool IsStartTutorialHovered;

    /// <summary>「チュートリアルをスキップ」ボタンのホバー状態。</summary>
    public bool IsSkipTutorialHovered;

    /// <summary>互換用旧確定ボタンホバー状態。</summary>
    public bool IsConfirmHovered;
}
