using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Handlers;

/// <summary>
///     チュートリアル個別フェーズのライフサイクルと達成判定を司るインターフェース。
/// </summary>
public interface ITutorialPhaseHandler
{
    /// <summary>
    ///     フェーズ進入時の初期化処理。目標物、小怪、または扉エンティティを生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="root">チュートリアルシーンのルートエンティティ</param>
    /// <param name="config">当該フェーズの設定</param>
    /// <param name="globalConfig">チュートリアル全体設定</param>
    void Setup(World world, Entity root, TutorialPhaseConfig config, TutorialConfig globalConfig);

    /// <summary>
    ///     毎フレームの更新および達成判定。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="state">チュートリアル進行状態</param>
    /// <param name="config">当該フェーズの設定</param>
    /// <param name="globalConfig">チュートリアル全体設定</param>
    /// <param name="dt">デルタタイム（秒）</param>
    /// <returns>目標達成し次フェーズへ移行可能な場合 true</returns>
    bool UpdateAndCheckCompletion(World world, ref TutorialState state, TutorialPhaseConfig config, TutorialConfig globalConfig, float dt);

    /// <summary>
    ///     フェーズ退場時またはリトライ時の片付け処理。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="root">チュートリアルシーンのルートエンティティ</param>
    void Teardown(World world, Entity root);
}
