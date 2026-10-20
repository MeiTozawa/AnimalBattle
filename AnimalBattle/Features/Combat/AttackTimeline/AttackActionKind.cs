namespace AnimalBattle.Abilities.Attack;

/// <summary>攻撃の各実行アクション（タイムラインステップ）の種類を定義します。</summary>
public enum AttackActionKind {
    /// <summary>弾を発射するアクションです。</summary>
    Bullet = 1,

    /// <summary>攻撃判定を生成するアクションです。</summary>
    Hitbox, // = 2

    /// <summary>攻撃に伴う基本速度を設定するアクションです。</summary>
    Movement, // = 3

    /// <summary>対象位置へのイージング平滑移動および滞空を行う汎用運動アクションです。</summary>
    EasingMovement, // = 4

    /// <summary>高速急降下・突進を行う汎用等速運動アクションです。</summary>
    VelocityDive, // = 5

    /// <summary>接地時に判定を生成するイベント駆動アクションです。</summary>
    OnGroundedHitbox, // = 6

    /// <summary>画面振動をトリガーする表現層アクションです。</summary>
    ScreenShake // = 7
}
