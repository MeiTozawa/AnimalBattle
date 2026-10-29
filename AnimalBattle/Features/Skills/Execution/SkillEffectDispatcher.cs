namespace AnimalBattle.Skills;

/// <summary>
/// 登録された各種ハンドラーを介して技能エフェクトを分配・実行するディスパッチャー。
/// </summary>
public static class SkillEffectDispatcher
{
    private static readonly Dictionary<SkillEffectKind, ISkillEffectHandler> _handlers = new();

    static SkillEffectDispatcher()
    {
        RegisterHandler(new BulletEffectHandler());
        RegisterHandler(new HitboxEffectHandler());
        RegisterHandler(new VelocityEffectHandler());
        RegisterHandler(new ScreenShakeEffectHandler());
        RegisterHandler(new SummonEffectHandler());
    }

    /// <summary>
    /// 新規エフェクトハンドラーを動的に登録・拡張します。
    /// </summary>
    public static void RegisterHandler(ISkillEffectHandler handler)
    {
        _handlers[handler.Kind] = handler;
    }

    /// <summary>
    /// 対象のエフェクトを実行します。
    /// </summary>
    public static void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        if (_handlers.TryGetValue(effect.Kind, out var handler))
        {
            handler.Execute(in ctx, effect, ref angleOffset);
        }
    }
}
