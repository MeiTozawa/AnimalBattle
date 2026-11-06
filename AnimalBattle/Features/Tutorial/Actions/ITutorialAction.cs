namespace AnimalBattle.Features.Tutorial.Actions;

/// <summary>
///     チュートリアルステップ開始時に1回のみ実行されるアクションインターフェース。
/// </summary>
public interface ITutorialAction
{
    /// <summary>
    ///     アクションを実行します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="root">チュートリアルシーンのルートエンティティ</param>
    void Execute(World world, Entity root);
}
