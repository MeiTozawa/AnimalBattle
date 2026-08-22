using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// 移動要求を表す値型 command です。
/// </summary>
public readonly struct MovementCommand
{
    /// <summary>移動要求の種別を取得します。</summary>
    public MovementCommandKind Kind { get; }

    /// <summary>要求する X 値を取得します。</summary>
    public float X { get; }

    /// <summary>要求する Y 値を取得します。</summary>
    public float Y { get; }

    /// <summary>移動要求を生成します。</summary>
    public MovementCommand(MovementCommandKind kind, float x, float y)
    {
        Kind = kind;
        X = x;
        Y = y;
    }
}

/// <summary>移動 command の種別を定義します。</summary>
public enum MovementCommandKind
{
    /// <summary>速度を設定します。</summary>
    SetVelocity = 1,
    /// <summary>指定位置へ移動します。</summary>
    MoveTo = 2,
    /// <summary>重力を適用します。</summary>
    ApplyGravity = 3,
    /// <summary>移動を停止します。</summary>
    Stop = 4
}

/// <summary>攻撃要求を表す値型 command です。</summary>
public readonly struct AttackCommand
{
    /// <summary>攻撃要求の種別を取得します。</summary>
    public AttackCommandKind Kind { get; }
    /// <summary>攻撃識別子を取得します。</summary>
    public AttackId AttackId { get; }
    /// <summary>command の X 値を取得します。</summary>
    public float X { get; }
    /// <summary>command の Y 値を取得します。</summary>
    public float Y { get; }
    /// <summary>攻撃 command を生成します。</summary>
    public AttackCommand(AttackCommandKind kind, AttackId attackId = default, float x = 0, float y = 0)
    {
        Kind = kind;
        AttackId = attackId;
        X = x;
        Y = y;
    }
}

/// <summary>攻撃 command の種別を定義します。</summary>
public enum AttackCommandKind
{
    /// <summary>弾を発射します。</summary>
    EmitBullet = 1,
    /// <summary>攻撃判定を生成します。</summary>
    CreateHitbox = 2,
    /// <summary>Boss の弾を整理します。</summary>
    ClearBossBullets = 3
}

/// <summary>演出要求を表す値型 command です。</summary>
public readonly struct EffectCommand
{
    /// <summary>演出要求の種別を取得します。</summary>
    public EffectCommandKind Kind { get; }
    /// <summary>演出の X 値を取得します。</summary>
    public float X { get; }
    /// <summary>演出の Y 値を取得します。</summary>
    public float Y { get; }
    /// <summary>演出要求を生成します。</summary>
    public EffectCommand(EffectCommandKind kind, float x = 0, float y = 0)
    {
        Kind = kind;
        X = x;
        Y = y;
    }
}

/// <summary>演出 command の種別を定義します。</summary>
public enum EffectCommandKind
{
    /// <summary>攻撃予告を表示します。</summary>
    RequestTelegraph = 1
}

/// <summary>事前確保済み command buffer の共通 read/write 境界です。</summary>
public interface ICommandBuffer<T> where T : struct
{
    /// <summary>現在の command 数を取得します。</summary>
    int Count { get; }
    /// <summary>command の最大格納数を取得します。</summary>
    int Capacity { get; }
    /// <summary>command を追加します。容量超過時は false を返します。</summary>
    bool TryAdd(in T command);
    /// <summary>指定優先度で command を追加します。</summary>
    bool TryAdd(in T command, CommandPriority priority);
    /// <summary>one-shot key を重複なく command とともに追加します。</summary>
    CommandWriteResult TryAddOneShot(in T command, in CommandOneShotKey key, CommandPriority priority = CommandPriority.Attack);
    /// <summary>格納済み command の one-shot key を取得します。</summary>
    bool TryGetOneShotKey(int index, out CommandOneShotKey key);
    /// <summary>次の Frame_Update に向けて command 数だけを 0 に戻します。</summary>
    void Clear();
    /// <summary>格納済み command を index で読み取ります。</summary>
    T this[int index] { get; }
}

/// <summary>移動処理への command 境界です。</summary>
public interface IMovementPort
{
    /// <summary>速度設定 command を積みます。</summary>
    bool SetVelocity(float x, float y);
    /// <summary>移動先 command を積みます。</summary>
    bool MoveTo(float x, float y);
    /// <summary>重力適用 command を積みます。</summary>
    bool ApplyGravity(float gravity, float maxFallSpeed);
    /// <summary>移動停止 command を積みます。</summary>
    bool Stop();
}

/// <summary>攻撃処理への command 境界です。</summary>
public interface IAttackPort
{
    /// <summary>弾生成 command を積みます。</summary>
    bool EmitBullet(AttackId attackId, float x, float y);
    /// <summary>攻撃判定生成 command を積みます。</summary>
    bool CreateHitbox(AttackId attackId, float x, float y);
    /// <summary>Boss の弾整理 command を積みます。</summary>
    bool ClearBossBullets();
}

/// <summary>演出処理への command 境界です。</summary>
public interface IEffectPort
{
    /// <summary>攻撃予告 command を積みます。</summary>
    bool RequestTelegraph(float x, float y);
}

/// <summary>状態モジュールが読む観測・診断記録の境界です。</summary>
public interface IObservationService
{
    /// <summary>構造化された観測イベントを事前確保領域へ記録します。</summary>
    bool TryRecord(in ObservationEvent observation);
}

/// <summary>明示的に注入される決定的な乱数源です。</summary>
public interface IRandomSource
{
    /// <summary>指定範囲の整数乱数を取得します。</summary>
    int NextInt(int exclusiveMax);
    /// <summary>0 以上 1 未満の単精度乱数を取得します。</summary>
    float NextSingle();
}
