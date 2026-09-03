using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>旧 Boss component へ投影する値だけを保持する純粋な互換 ECS データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossCompatibilityData
{
    /// <summary>旧 state enum の整数値です。</summary>
    public int LegacyStateValue;
    /// <summary>旧攻撃識別子の整数値です。</summary>
    public int LegacyAttackValue;
    /// <summary>旧 state timer です。</summary>
    public float LegacyStateTimer;
    /// <summary>旧 state duration です。</summary>
    public float LegacyStateDuration;
    /// <summary>旧無敵表現です。</summary>
    public bool LegacyIsInvulnerable;
    /// <summary>旧 state machine の技能 index です。</summary>
    public int LegacySkillIndex;
    /// <summary>旧 state machine の sub timer です。</summary>
    public float LegacySubTimer;
    /// <summary>旧 state machine の角度 offset です。</summary>
    public float LegacyAngleOffset;
    /// <summary>旧 state machine の移動開始位置 X です。</summary>
    public float LegacyStartX;
    /// <summary>旧 state machine の移動開始位置 Y です。</summary>
    public float LegacyStartY;
    /// <summary>旧 state machine の移動目標位置 X です。</summary>
    public float LegacyTargetX;
    /// <summary>旧 state machine の移動目標位置 Y です。</summary>
    public float LegacyTargetY;
    /// <summary>旧 Boss の基準または出現位置 X です。</summary>
    public float LegacyBaseOrSpawnX;
    /// <summary>旧 Boss の基準または出現位置 Y です。</summary>
    public float LegacyBaseOrSpawnY;
    /// <summary>旧 Boss の移動補助 timer です。</summary>
    public float LegacyAuxiliaryTimer;
    /// <summary>旧 Boss の被弾演出 timer です。</summary>
    public float LegacyHitFlashTimer;
    /// <summary>旧 Boss1 stats の弾幕回転角度 offset です。</summary>
    public float LegacyBoss1StatsAngleOffset;
    /// <summary>旧 Boss2 stats の跳躍 cooldown です。</summary>
    public float LegacyJumpCooldown;
    /// <summary>旧 Boss2 の攻撃速度です。</summary>
    public float LegacyActionSpeed;
    /// <summary>旧 Boss2 の向きです。</summary>
    public bool LegacyFacingRight;
    /// <summary>旧 Boss2 の combo index です。</summary>
    public int LegacyComboIndex;
    /// <summary>互換値の更新元です。</summary>
    public UpdateSource UpdateSource;
    /// <summary>互換 view が公開する固定 BossState です。</summary>
    public BossState BossState;
    /// <summary>互換 view が公開する親 Phase です。</summary>
    public Phase Phase;
    /// <summary>互換 view が公開する技能識別子です。</summary>
    public BossSkill Skill;
    /// <summary>互換 view が公開する直近の遷移理由です。</summary>

    public TransitionReason TransitionReason;
    /// <summary>互換 view が公開する無敵状態です。</summary>
    public bool IsInvulnerable;
    /// <summary>互換 view が公開する攻撃段階です。</summary>
    public AttackStage AttackStage;
    /// <summary>Debug_Data が現在利用可能かどうかです。</summary>
    public bool HasDebugData;
    /// <summary>互換値を作成した Frame_Update 番号です。</summary>
    public long FrameUpdateNumber;
}
