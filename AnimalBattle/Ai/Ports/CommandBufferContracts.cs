using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Ports;

/// <summary>command 容量不足時の採用優先度です。</summary>
public enum CommandPriority
{
    /// <summary>通常の演出・移動要求です。</summary>
    Normal = 0,
    /// <summary>通常の攻撃要求です。</summary>
    Attack = 1,
    /// <summary>安全値と攻撃判定です。</summary>
    Safety = 2,
    /// <summary>エラー診断です。</summary>
    Diagnostic = 3
}

/// <summary>command を識別する one-shot key の領域です。</summary>
public enum CommandComponentKind
{
    /// <summary>移動 component です。</summary>
    Movement = 1,
    /// <summary>攻撃 component です。</summary>
    Attack = 2,
    /// <summary>演出 component です。</summary>
    Effect = 3
}

/// <summary>同じ攻撃 component の二重発行を防ぐ値型 key です。</summary>
public readonly record struct CommandOneShotKey(
    CommandComponentKind Component,
    AttackId AttackId,
    int ComponentIndex,
    long FrameUpdateNumber)
{
    /// <summary>key が発行可能な値かどうかを取得します。</summary>
    public bool IsValid => Component != 0 && ComponentIndex >= 0 && FrameUpdateNumber >= 0;
}

/// <summary>command buffer の拒否理由です。</summary>
public enum CommandBufferFailureReason
{
    /// <summary>拒否理由がありません。</summary>
    None = 0,
    /// <summary>容量不足で低優先度だったことを示します。</summary>
    CapacityExceeded = 1,
    /// <summary>同じ one-shot key が既に存在することを示します。</summary>
    DuplicateOneShot = 2,
    /// <summary>one-shot key が不正であることを示します。</summary>
    InvalidOneShotKey = 3,
    /// <summary>診断記録に失敗したことを示します。</summary>
    DiagnosticFailed = 4
}

/// <summary>command buffer の overflow 診断を表す値型です。</summary>
public readonly record struct CommandBufferDiagnostic(
    CommandBufferFailureReason Reason,
    CommandPriority IncomingPriority,
    CommandPriority ExistingPriority,
    CommandOneShotKey OneShotKey,
    ErrorCode ErrorCode);

/// <summary>command 書き込みの結果と、受理された one-shot key を返します。</summary>
public readonly record struct CommandWriteResult(
    bool IsAccepted,
    CommandOneShotKey OneShotKey,
    CommandBufferFailureReason FailureReason,
    ErrorCode ErrorCode)
{
    /// <summary>command が受理されたかどうかを取得します。</summary>
    public bool IsSuccess => IsAccepted;
}

/// <summary>command buffer の診断を受け取る境界です。</summary>
public interface ICommandBufferDiagnosticSink
{
    /// <summary>overflow または重複を記録します。</summary>
    bool TryRecord(in CommandBufferDiagnostic diagnostic);
}