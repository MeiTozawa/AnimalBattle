namespace AnimalBattle.Abilities.Skills;

/// <summary>
///     技能実行の微視的フェーズ：予兆・実行・後隙を表す列挙体です。
/// </summary>
public enum SkillPhase : byte {
    /// <summary>未開始または待機状態。</summary>
    None = 0,

    /// <summary>前隙：予兆演出・蓄力・警告表示。</summary>
    Telegraph = 1,

    /// <summary>判定実行：弾幕発射・突進・近接攻撃判定。</summary>
    Active = 2,

    /// <summary>後隙：硬直・破綻・易傷状態。</summary>
    Recovery = 3,

    /// <summary>技能実行完了。</summary>
    Completed = 4
}