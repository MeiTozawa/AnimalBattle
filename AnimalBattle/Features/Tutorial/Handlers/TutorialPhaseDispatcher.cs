using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Handlers;

/// <summary>
///     チュートリアルフェーズ種別に応じたハンドラーの取得および振り分けを司るディスパッチャー。
/// </summary>
public static class TutorialPhaseDispatcher
{
    private static readonly ITutorialPhaseHandler[] Handlers =
    [
        new CollectPhaseHandler(),
        new CombatPhaseHandler(),
        new BossDoorPhaseHandler()
    ];

    /// <summary>
    ///     指定されたフェーズ種別に対応するハンドラーを取得します。
    /// </summary>
    /// <param name="type">フェーズ種別</param>
    /// <returns>対応するハンドラー、存在しない場合は null</returns>
    public static ITutorialPhaseHandler? GetHandler(TutorialPhaseType type)
    {
        var index = (int)type;
        if ((uint)index < (uint)Handlers.Length)
        {
            return Handlers[index];
        }

        Debug.Assert(false, $"未対応のチュートリアルフェーズ種別です: {type}");
        return null;
    }
}
