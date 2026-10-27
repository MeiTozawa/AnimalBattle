namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     無敵点滅エフェクトの持続時間更新および自動破棄を制御する ECS システム。
///     HitFlashEffectSystem と同一のライフサイクル管理パターンに従います。
/// </summary>
public static class InvincibleBlinkEffectSystem
{
    /// <summary>
    ///     無敵点滅エフェクトシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddInvincibleBlinkEffectSystem(this World world)
    {
        world.System<InvincibleBlinkEffect>("InvincibleBlinkEffectSystem")
            .Kind(Ecs.OnUpdate)
            .Each(UpdateInvincibleBlinkEffect);

        return world;
    }

    /// <summary>
    ///     無敵点滅タイマーの更新と終了時コンポーネント削除処理。
    /// </summary>
    public static void UpdateInvincibleBlinkEffect(Iter it, int i, ref InvincibleBlinkEffect blink)
    {
        blink.ElapsedTimer += it.DeltaTime();
        if (blink.IsFinished)
        {
            it.Entity(i).Remove<InvincibleBlinkEffect>();
        }
    }
}
