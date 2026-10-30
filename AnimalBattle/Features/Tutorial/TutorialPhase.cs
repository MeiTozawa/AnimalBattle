namespace AnimalBattle.Features.Tutorial;

/// <summary>
///     チュートリアルの進行段階を表すフェーズ列挙体。
/// </summary>
public enum TutorialPhase
{
    /// <summary>段階1：移動練習（左右移動）</summary>
    Movement = 0,

    /// <summary>段階2：単段ジャンプ練習</summary>
    Jump = 1,

    /// <summary>段階3：二段ジャンプ練習</summary>
    DoubleJump = 2,

    /// <summary>段階4：ダッシュ練習</summary>
    Dash = 3,

    /// <summary>段階5：飛行小怪対戦練習</summary>
    FlyingMinion = 4,

    /// <summary>段階6：地上小怪対戦練習</summary>
    GroundMinion = 5,

    /// <summary>段階7：ボス選択扉（本戦移行）</summary>
    BossSelect = 6
}
