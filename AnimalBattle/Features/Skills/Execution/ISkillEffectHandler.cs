namespace AnimalBattle.Skills;

/// <summary>
/// 各種技能アクションエフェクトを実行するハンドラーの共通インターフェース。
/// </summary>
public interface ISkillEffectHandler
{
    /// <summary>対応する効果種別</summary>
    SkillEffectKind Kind { get; }

    /// <summary>
    /// エフェクトを実行します。
    /// </summary>
    /// <param name="ctx">実行コンテキスト</param>
    /// <param name="effect">エフェクト設定</param>
    /// <param name="angleOffset">累積回転角オフセット</param>
    void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset);
}
