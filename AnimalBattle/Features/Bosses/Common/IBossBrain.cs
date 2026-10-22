namespace AnimalBattle.Ai;

/// <summary>
///     Boss 意思決定ブレインの共通インターフェースです。
/// </summary>
public interface IBossBrain
{
    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    AbilitySystemComponent AbilitySystem { get; }

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    Phase Phase { get; }

    /// <summary>現在発動中の技能識別タグを取得します。</summary>
    StringId CurrentSkillTag { get; }

    /// <summary>現在 AI 状態の経過時間秒単位を取得します。</summary>
    float StateTimer { get; }

    /// <summary>AI 目標地点座標を取得します。</summary>
    Vec2f TargetPosition { get; }

    /// <summary>現在の Boss 座標を取得します。</summary>
    Vec2f Position { get; }

    /// <summary>Boss が右向きであるかを取得します。</summary>
    bool FacingRight => true;

    /// <summary>死亡状態かどうかを取得します。</summary>
    bool IsDead { get; }

    /// <summary>弾幕波次発射イベント。</summary>
    event Action<StringId, Vec2f, Vec2f>? OnEmitBulletWave;

    /// <summary>全弾幕消去イベント。</summary>
    event Action? OnClearBullets;

    /// <summary>画面振動要求イベント。</summary>
    event Action<float, float>? OnScreenShake;

    /// <summary>死亡処理を実行します。</summary>
    void Kill();

    /// <summary>状態を初期位置にリセットします。</summary>
    void Reset(Vec2f pos);

    /// <summary>デバッグ用に指定フェーズへ即座に切り替えます。</summary>
    void DebugSetPhase(Phase targetPhase);
}
