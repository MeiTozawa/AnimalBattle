using System.Runtime.InteropServices;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>Boss の攻撃・技能状態を表すデータ構造体です。</summary>
/// <remarks>攻撃・技能データを生成します。</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct BossAttackData(BossSkill skill, AttackStage stage = AttackStage.None) {
    /// <summary>現在の技能識別子です。</summary>
    public BossSkill Skill = skill;

    /// <summary>攻撃の段階です。</summary>
    public AttackStage Stage = stage;

    /// <summary>攻撃内の component index です。</summary>
    public int AttackIndex = 0;

    /// <summary>攻撃段階の sub timer です。</summary>
    public float SubTimer = 0.0f;

    /// <summary>攻撃角度の累積 offset です。</summary>
    public float AngleOffset = 0.0f;

    /// <summary>攻撃開始時に固定した対象位置です。</summary>
    public Vec2f TargetPosition = Vec2f.Zero;

    /// <summary>対象 Entity の識別子です。</summary>
    public ulong TargetEntityId = 0;

    /// <summary>同じ攻撃 component の one-shot 済みビットです。</summary>
    public ulong OneShotEmissionMask = 0;

    /// <summary>設定上の攻撃継続時間です。</summary>
    public float ConfiguredDuration = 0.0f;

    /// <summary>実際に経過した攻撃時間です。</summary>
    public float ElapsedDuration = 0.0f;
}