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

    /// <summary>チュートリアルを有効にするかどうかのチェック状態。</summary>
    public bool IsTutorialEnabled;

    /// <summary>チェックボックス領域のホバー状態。</summary>
    public bool IsCheckboxHovered;

    /// <summary>「ゲーム開始」ボタンのホバー状態。</summary>
    public bool IsStartGameHovered;
}
