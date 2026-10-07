namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃の各実行 component の種類を定義します。</summary>
public enum AttackComponentKind {
    /// <summary>弾を発射する component です。</summary>
    Bullet = 1,

    /// <summary>攻撃判定を生成する component です。</summary>
    Hitbox, // = 2

    /// <summary>攻撃に伴う基本速度を設定する component です。</summary>
    Movement, // = 3

    /// <summary>対象位置へのイージング平滑移動および滞空を行う汎用運動 component です。</summary>
    EasingMovement, // = 4

    /// <summary>高速急降下・突進を行う汎用等速運動 component です。</summary>
    VelocityDive, // = 5

    /// <summary>接地時に判定を生成するイベント駆動 component です。</summary>
    OnGroundedHitbox, // = 6

    /// <summary>音効 Cue を発行する表現層 component です。</summary>
    AudioCue, // = 7

    /// <summary>画面振動をトリガーする表現層 component です。</summary>
    ScreenShake // = 8
}