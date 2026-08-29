using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Benchmark;

/// <summary>ベンチマーク計測の状態を表します。</summary>
public enum ReferenceBenchmarkStatus
{
    /// <summary>未計測状態です。</summary>
    NotMeasured = 0,
    /// <summary>正常に計測完了した状態です。</summary>
    Measured = 1
}

/// <summary>ビルドプロファイル情報を保持するレコードです。</summary>
public readonly record struct ReferenceBenchmarkBuildProfile(
    string BuildConfiguration,
    bool IsOptimized,
    string TargetFramework);

/// <summary>ベンチマーク実行環境の情報を保持するレコードです。</summary>
public readonly record struct ReferenceBenchmarkEnvironment(int ProcessorCount);

/// <summary>フェーズごとの計測メトリクスを保持するレコードです。</summary>
public readonly record struct ReferenceBenchmarkPhaseMetric(
    long ManagedAllocatedBytes,
    long ElapsedTimeTicks,
    long CpuTimeTicks,
    int GcCollections = 0);

/// <summary>ワークロードの計測結果を保持するレコードです。</summary>
public readonly record struct ReferenceBenchmarkWorkloadResult(
    bool IsMeasured,
    ReferenceBenchmarkPhaseMetric Initialization,
    ReferenceBenchmarkPhaseMetric FrameUpdate);

/// <summary>ベンチマークの実行シナリオを定義するクラスです。</summary>
public sealed class ReferenceBenchmarkScenario
{
    /// <summary>対象 Boss 種別を取得します。</summary>
    public BossType BossType { get; }
    /// <summary>設定スナップショットを取得します。</summary>
    public BossConfigSnapshot ConfigurationSnapshot { get; }
    /// <summary>計測フレーム数を取得します。</summary>
    public int FrameCount { get; }
    /// <summary>乱数シード値を取得します。</summary>
    public int Seed { get; }
    /// <summary>ビルドプロファイルを取得します。</summary>
    public ReferenceBenchmarkBuildProfile BuildProfile { get; }
    /// <summary>設定スナップショットの識別名を取得します。</summary>
    public string ConfigurationSnapshotId { get; }

    /// <summary>ベンチマークシナリオを生成します。</summary>
    public ReferenceBenchmarkScenario(
        BossType bossType,
        BossConfigSnapshot configurationSnapshot,
        int frameCount,
        int seed,
        ReferenceBenchmarkBuildProfile buildProfile,
        string? configurationSnapshotId = null)
    {
        BossType = bossType;
        ConfigurationSnapshot = configurationSnapshot;
        FrameCount = frameCount;
        Seed = seed;
        BuildProfile = buildProfile;
        ConfigurationSnapshotId = configurationSnapshotId ?? $"boss-config-v{configurationSnapshot.Version}";
    }
}

/// <summary>ワークロードの実行コンテキストです。</summary>
public sealed class ReferenceBenchmarkWorkloadContext(ReferenceBenchmarkScenario scenario)
{
    /// <summary>実行中のシナリオを取得します。</summary>
    public ReferenceBenchmarkScenario Scenario { get; } = scenario ?? throw new ArgumentNullException(nameof(scenario));
}

/// <summary>計測対象のワークロード定義です。</summary>
public sealed class ReferenceBenchmarkWorkload(
    string name,
    Action<ReferenceBenchmarkWorkloadContext> initialize,
    Action<ReferenceBenchmarkWorkloadContext, long> frameUpdate)
{
    /// <summary>ワークロード名を取得します。</summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    /// <summary>初期化処理の委譲先を取得します。</summary>
    public Action<ReferenceBenchmarkWorkloadContext> Initialize { get; } = initialize ?? throw new ArgumentNullException(nameof(initialize));
    /// <summary>フレーム更新処理の委譲先を取得します。</summary>
    public Action<ReferenceBenchmarkWorkloadContext, long> FrameUpdate { get; } = frameUpdate ?? throw new ArgumentNullException(nameof(frameUpdate));
}

/// <summary>ベンチマークの計測成果物データです。</summary>
public sealed class ReferenceBenchmarkArtifact
{
    /// <summary>スキーマバージョンを取得します。</summary>
    public string SchemaVersion { get; } = "reference-benchmark/v1";
    /// <summary>計測ステータスを取得します。</summary>
    public ReferenceBenchmarkStatus Status { get; }
    /// <summary>レガシー実装の計測結果を取得します。</summary>
    public ReferenceBenchmarkWorkloadResult Legacy { get; }
    /// <summary>リファクタリング版の計測結果を取得します。</summary>
    public ReferenceBenchmarkWorkloadResult Refactored { get; }
    /// <summary>実行シナリオを取得します。</summary>
    public ReferenceBenchmarkScenario Scenario { get; }
    /// <summary>実行環境情報を取得します。</summary>
    public ReferenceBenchmarkEnvironment Environment { get; }
    /// <summary>未計測時の理由を取得します。</summary>
    public string? NotMeasuredReason { get; }

    /// <summary>成果物インスタンスを生成します。</summary>
    public ReferenceBenchmarkArtifact(
        ReferenceBenchmarkStatus status,
        ReferenceBenchmarkWorkloadResult legacy,
        ReferenceBenchmarkWorkloadResult refactored,
        ReferenceBenchmarkScenario scenario,
        ReferenceBenchmarkEnvironment environment,
        string? notMeasuredReason = null)
    {
        Status = status;
        Legacy = legacy;
        Refactored = refactored;
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        Environment = environment;
        NotMeasuredReason = notMeasuredReason;
    }

    /// <summary>成果物を JSON 文字列としてシリアライズします。</summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, ReferenceBenchmarkJsonContext.Default.ReferenceBenchmarkArtifact);
    }
}

/// <summary>ベンチマーク成果物の JSON ソース生成コンテキストです。</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ReferenceBenchmarkArtifact))]
internal sealed partial class ReferenceBenchmarkJsonContext : JsonSerializerContext
{
}

/// <summary>Boss AI の分離比較ベンチマークを実行するハーネスです。</summary>
public sealed class ReferenceBenchmarkHarness
{
    /// <summary>シナリオに従って旧版と新版のワークロードを計測します。</summary>
    public ReferenceBenchmarkArtifact Run(
        ReferenceBenchmarkScenario scenario,
        ReferenceBenchmarkWorkload legacy,
        ReferenceBenchmarkWorkload refactored)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(refactored);

        ReferenceBenchmarkEnvironment environment = new(System.Environment.ProcessorCount);

        if (scenario.BuildProfile.BuildConfiguration != "Release" || !scenario.BuildProfile.IsOptimized)
        {
            return new ReferenceBenchmarkArtifact(
                ReferenceBenchmarkStatus.NotMeasured,
                default,
                default,
                scenario,
                environment,
                "Release / 最適化ビルドではないため計測をスキップしました。");
        }

        ReferenceBenchmarkWorkloadResult legacyResult = MeasureWorkload(scenario, legacy);
        ReferenceBenchmarkWorkloadResult refactoredResult = MeasureWorkload(scenario, refactored);

        return new ReferenceBenchmarkArtifact(
            ReferenceBenchmarkStatus.Measured,
            legacyResult,
            refactoredResult,
            scenario,
            environment);
    }

    private static ReferenceBenchmarkWorkloadResult MeasureWorkload(
        ReferenceBenchmarkScenario scenario,
        ReferenceBenchmarkWorkload workload)
    {
        ReferenceBenchmarkWorkloadContext context = new(scenario);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long initAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch initStopwatch = Stopwatch.StartNew();
        TimeSpan initCpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
        int initGcBefore = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

        workload.Initialize(context);

        initStopwatch.Stop();
        long initAlloc = GC.GetAllocatedBytesForCurrentThread() - initAllocBefore;
        long initCpuTicks = (Process.GetCurrentProcess().TotalProcessorTime - initCpuBefore).Ticks;
        int initGc = (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2)) - initGcBefore;
        ReferenceBenchmarkPhaseMetric initMetric = new(
            Math.Max(0, initAlloc),
            initStopwatch.ElapsedTicks,
            Math.Max(0, initCpuTicks),
            initGc);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long frameAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch frameStopwatch = Stopwatch.StartNew();
        TimeSpan frameCpuBefore = Process.GetCurrentProcess().TotalProcessorTime;
        int frameGcBefore = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);

        for (int frame = 0; frame < scenario.FrameCount; frame++)
        {
            workload.FrameUpdate(context, frame);
        }

        frameStopwatch.Stop();
        long frameAlloc = GC.GetAllocatedBytesForCurrentThread() - frameAllocBefore;
        long frameCpuTicks = (Process.GetCurrentProcess().TotalProcessorTime - frameCpuBefore).Ticks;
        int frameGc = (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2)) - frameGcBefore;
        ReferenceBenchmarkPhaseMetric frameMetric = new(
            Math.Max(0, frameAlloc),
            frameStopwatch.ElapsedTicks,
            Math.Max(0, frameCpuTicks),
            frameGc);

        return new ReferenceBenchmarkWorkloadResult(true, initMetric, frameMetric);
    }
}
