using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AnimalBattle.Logging;

/// <summary>
/// プロジェクト共通のログ入口。Microsoft.Extensions.Logging をラップし、カテゴリ名ごとに ILogger をキャッシュする。
/// AOT 対応のため反射を使わず、DI コンテナも使用しない。
/// </summary>
public static class Log
{
    private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
    {
#if DEBUG
        builder.SetMinimumLevel(LogLevel.Debug);
#else
        builder.SetMinimumLevel(LogLevel.Warning);
#endif
        builder.AddDebug();
    });

    private static readonly ConcurrentDictionary<string, ILogger> Loggers = new();

    private static ILogger For(string category) => Loggers.GetOrAdd(category, Factory.CreateLogger);

    public static void Debug(string category, string message) => For(category).LogDebug("{Message}", message);

    public static void Info(string category, string message) => For(category).LogInformation("{Message}", message);

    public static void Warn(string category, string message) => For(category).LogWarning("{Message}", message);

    public static void Error(string category, string message, Exception? exception = null) =>
        For(category).LogError(exception, "{Message}", message);
}
