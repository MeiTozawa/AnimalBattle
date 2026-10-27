namespace AnimalBattle.Skills;

/// <summary>
/// 技能エフェクト実行時に必要なランタイム環境コンテキスト構造体。
/// </summary>
public readonly ref struct SkillExecutionContext
{
    /// <summary>対象の Flecs ワールド</summary>
    public required World World { get; init; }

    /// <summary>発動中の技能識別タグ</summary>
    public required StringId SkillTag { get; init; }

    /// <summary>発動元エンティティの現在座標</summary>
    public required Vec2f Origin { get; init; }

    /// <summary>攻撃の目標地点座標</summary>
    public required Vec2f TargetPos { get; init; }

    /// <summary>水平前方向き（正: 右向き、負: 左向き）</summary>
    public required float ForwardDir { get; init; }

    /// <summary>現在の施術者移動速度</summary>
    public required Vec2f CasterVelocity { get; init; }

    /// <summary>施術者の衝突判定高さ（px）</summary>
    public required float BodyHeight { get; init; }

    /// <summary>移動速度設定コールバック</summary>
    public Action<Vec2f>? SetVelocity { get; init; }
}
