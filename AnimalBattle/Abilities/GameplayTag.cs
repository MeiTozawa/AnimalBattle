namespace AnimalBattle.Abilities;

/// <summary>
///     戦闘状態および能力の制約を表す 64 ビット型安全タグフラグです。
///     ヒープ割り当てを生じさせない零 GC ビットマスクとして動作します。
/// </summary>
[Flags]
public enum GameplayTag : ulong {
    /// <summary>制約なし・空タグ。</summary>
    None = 0,

    /// <summary>技能発動中。</summary>
    Casting = 1UL << 0,

    /// <summary>攻撃予兆前隙中。</summary>
    Telegraph = 1UL << 1,

    /// <summary>攻撃判定持続中。</summary>
    Active = 1UL << 2,

    /// <summary>攻撃後隙硬直中。</summary>
    Recovery = 1UL << 3,

    /// <summary>剛体（スーパーアーマー）。</summary>
    SuperArmor = 1UL << 4,

    /// <summary>完全無敵。</summary>
    Invincible = 1UL << 5,

    /// <summary>破綻・易傷状態。</summary>
    Vulnerable = 1UL << 6,

    /// <summary>滞空状態。</summary>
    Airborne = 1UL << 7,

    /// <summary>接地状態。</summary>
    Grounded = 1UL << 8,

    /// <summary>気絶・失衡硬直中。</summary>
    Stunned = 1UL << 9,

    /// <summary>打断中断状態。</summary>
    Interrupted = 1UL << 10,

    // ===== UE 风格 Cooldown 标签体系 =====
    /// <summary>全局公共冷却（Global Cooldown，防止同一帧或短时间内瞬间连发）。</summary>
    CooldownGlobal = 1UL << 11,

    /// <summary>近戦技能組冷却（近接技全体の連続使用を抑制）。</summary>
    CooldownMelee = 1UL << 12,

    /// <summary>遠隔・弾幕技能組冷却（弾幕密度と射撃リズムを統制）。</summary>
    CooldownRanged = 1UL << 13,

    /// <summary>大技・高脅威技能組冷却（必殺・位移大技の頻度を抑制）。</summary>
    CooldownHeavy = 1UL << 14
}
