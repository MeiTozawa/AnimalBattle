using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Plugins;
using AnimalBattle.Stages;
using AnimalBattle.Systems;

namespace AnimalBattle.Scenes;
/// <summary>
/// Flecs.NET ワールドおよび純粋 ECS パイプラインを統括するバトルシーンクラス。
/// </summary>
public partial class BattleScene : IDisposable
{
    private readonly World _ecsWorld;
    private bool _isDisposed;
    private bool _previousKey1;
    private bool _previousKey2;
    private bool _previousKey3;
    private bool _previousKey4;
    private bool _previousKeyM;

    /// <summary>
    /// <see cref="BattleScene"/> の新しいインスタンスを初期化します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
    }

    /// <summary>
    /// Flecs.NET システムおよびエンティティの初期化を行います。
    /// </summary>
    public void Initialize()
    {
        AnimationManager.Initialize();
        int catHandle = AnimationManager.GetCharacterSpriteHandle();

        App app = new App(_ecsWorld)
            .AddPluginGroup(new DefaultPlugins());
        app.FinishSetup();

        SpawnPlayer(1, 380.0f, 320.0f, false, catHandle);
        SpawnPlayer(2, 540.0f, 450.0f, false, catHandle);
        SpawnPlayer(3, 740.0f, 450.0f, true, catHandle);
        SpawnPlayer(4, 900.0f, 320.0f, true, catHandle);

        LoadStage(StageRegistry.CurrentStageIndex);
    }

    /// <summary>
    /// 指定されたインデックス (0～3) のマップ足場をロード・切り替えます。
    /// </summary>
    /// <param name="stageIndex">切り替え先ステージインデックス</param>
    public void LoadStage(int stageIndex)
    {
        StageRegistry.SetStageIndex(stageIndex);
        StageInfo stage = StageRegistry.CurrentStage;

        _ecsWorld.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build()
                 .Each((Entity e, ref Transform _, ref CollisionBox _) =>
                 {
                     e.Destruct();
                 });

        for (int p = 0; p < stage.Platforms.Count; p++)
        {
            PlatformConfig plat = stage.Platforms[p];
            _ecsWorld.Entity($"Platform_{stage.Id}_{p}")
                     .Set(new Transform(plat.X, plat.Y))
                     .Set(new CollisionBox(plat.Width, plat.Height))
                     .Add<PlatformTag>();
        }

        _ecsWorld.QueryBuilder<PlayerId, Transform, Velocity, CollisionBox, Animation, Sprite, CharacterStats>()
                 .With<PlayerTag>()
                 .Build()
                 .Each((ref PlayerId pid, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Animation anim, ref Sprite sprite, ref CharacterStats stats) =>
                 {
                     int idx = pid.Id - 1;
                     if (idx >= 0 && idx < stage.Spawns.Length)
                     {
                         trans.X = stage.Spawns[idx].X;
                         trans.Y = stage.Spawns[idx].Y;
                     }

                     stats.DamagePercent = 0.0f;
                     stats.HitstunTimer = 0.0f;
                     stats.AttackCooldown = 0.0f;
                     vel.KnockbackX = 0.0f;
                     vel.KnockbackY = 0.0f;
                     vel.Vx = 0.0f;
                     vel.Vy = 0.0f;
                     col.IsGrounded = false;
                     anim.CurrentClip = "Idle";
                     anim.Timer = 0.0f;
                 });

        Console.WriteLine($"[マップ読込完了] マップ {stage.Id}: {stage.Name} に切り替えました（プレイヤーおよびステータスを初期化）");
    }

    private void SpawnPlayer(int id, float x, float y, bool flipX, int catHandle)
    {
        _ecsWorld.Entity($"Player{id}")
            .Set(new PlayerId(id))
            .Set(new Transform(x, y, scaleX: 2.2f, scaleY: 2.2f))
            .Set(new Velocity())
            .Set(new CollisionBox(40.0f, 65.0f))
            .Set(new Input())
            .Set(new Sprite(catHandle, flipX: flipX, isVisible: true, srcRect: new Rect<int>(0, 0, 32, 32)))
            .Set(new Animation("Idle", isPlaying: true))
            .Set(new CharacterStats(100.0f))
            .Add<PlayerTag>();
    }

    /// <summary>
    /// シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    /// <returns>正常続行時は true</returns>
    public bool Update(float deltaTime)
    {
        bool isKey1Pressed = GameEngine.IsKeyHit(0x02);
        bool isKey2Pressed = GameEngine.IsKeyHit(0x03);
        bool isKey3Pressed = GameEngine.IsKeyHit(0x04);
        bool isKey4Pressed = GameEngine.IsKeyHit(0x05);
        bool isKeyMPressed = GameEngine.IsKeyHit(0x32);

        if (isKey1Pressed && !_previousKey1)
        {
            LoadStage(0);
        }
        if (isKey2Pressed && !_previousKey2)
        {
            LoadStage(1);
        }
        if (isKey3Pressed && !_previousKey3)
        {
            LoadStage(2);
        }
        if (isKey4Pressed && !_previousKey4)
        {
            LoadStage(3);
        }
        if (isKeyMPressed && !_previousKeyM)
        {
            LoadStage((StageRegistry.CurrentStageIndex + 1) % 4);
        }

        _previousKey1 = isKey1Pressed;
        _previousKey2 = isKey2Pressed;
        _previousKey3 = isKey3Pressed;
        _previousKey4 = isKey4Pressed;
        _previousKeyM = isKeyMPressed;

        _ecsWorld.Progress(deltaTime);
        return true;
    }

    /// <summary>
    /// シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _ecsWorld.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
