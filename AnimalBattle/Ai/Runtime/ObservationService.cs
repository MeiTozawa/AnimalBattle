using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using ErrorOr;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 重大度を考慮して観測を保持する bounded ring buffer です。
/// </summary>
public sealed class ObservationService : IObservationService
{
    private readonly ObservationEvent[] _events;
    private readonly Action<ObservationEvent>? _logger;
    private int _head;
    private int _count;
    private long _droppedInformationCount;
    private long _recordFailureCount;

    /// <summary>現在保持している観測数を取得します。</summary>
    public int Count => _count;

    /// <summary>観測容量を取得します。</summary>
    public int Capacity => _events.Length;

    /// <summary>容量不足で破棄した通常情報の件数を取得します。</summary>
    public long DroppedInformationCount => _droppedInformationCount;

    /// <summary>記録処理の失敗件数を取得します。</summary>
    public long RecordFailureCount => _recordFailureCount;

    /// <summary>観測サービスを生成します。</summary>
    /// <param name="capacity">保持する最大観測数。</param>
    /// <param name="logger">外部 logger。失敗しても lifecycle は例外を送出しません。</param>
    public ObservationService(int capacity = 256, Action<ObservationEvent>? logger = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        _events = new ObservationEvent[capacity];
        _logger = logger;
    }

    /// <summary>観測を ring buffer へ記録します。</summary>
    public ErrorOr<Success> Record(in ObservationEvent observation)
    {
        try
        {
            if (!Store(in observation))
            {
                return Result.Success;
            }

            if (_logger is not null)
            {
                _logger(observation);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _recordFailureCount++;
            return Error.Failure("Observation.RecordFailed", ex.Message);
        }
    }

    /// <summary>重大度付きの診断を構造化観測へ変換して記録します。</summary>
    public ErrorOr<Success> RecordDiagnostic(
        DiagnosticResult diagnostic,
        BossType bossType = BossType.Unknown,
        LifecycleOperation operation = LifecycleOperation.Update)
    {
        ObservationEvent observation = new(
            diagnostic.EntityId,
            bossType,
            diagnostic.State,
            diagnostic.Phase,
            operation,
            diagnostic.FrameUpdateNumber,
            0,
            diagnostic.Skill,
            AttackStage.None,

            default,
            diagnostic.Severity,
            diagnostic.ErrorCode,
            diagnostic.Message);
        return Record(in observation);
    }

    /// <summary>保存済み観測を古い順に読み取ります。</summary>
    public bool Get(int index, out ObservationEvent observation)
    {
        if ((uint)index >= (uint)_count)
        {
            observation = default;
            return false;
        }

        int actualIndex = (_head + index) % _events.Length;
        observation = _events[actualIndex];
        return true;
    }

    /// <summary>保存済み観測をすべて削除します。</summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;
    }

    private bool Store(in ObservationEvent observation)
    {
        if (_count < _events.Length)
        {
            int appendIndex = (_head + _count) % _events.Length;
            _events[appendIndex] = observation;
            _count++;
            return true;
        }

        if (observation.Severity == DiagnosticSeverity.Information)
        {
            _droppedInformationCount++;
            return false;
        }

        int informationIndex = FindOldestInformationIndex();
        if (informationIndex >= 0)
        {
            _events[informationIndex] = observation;
            _droppedInformationCount++;
            return true;
        }

        _events[_head] = observation;
        _head = (_head + 1) % _events.Length;
        return true;
    }

    private int FindOldestInformationIndex()
    {
        for (int offset = 0; offset < _count; offset++)
        {
            int index = (_head + offset) % _events.Length;
            if (_events[index].Severity == DiagnosticSeverity.Information)
            {
                return index;
            }
        }

        return -1;
    }
}
