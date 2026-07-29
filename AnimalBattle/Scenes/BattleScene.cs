using System;
using AnimalBattle.Animations;
using AnimalBattle.Battle;
using AnimalBattle.Core;
using AnimalBattle.Inputs;
using AnimalBattle.Stages;

namespace AnimalBattle.Scenes;

/// <summary>
/// 動的プレイヤー群 (1～4人) のバトルシーンのライフサイクル、カメラ制御、およびレンダリング描画を統括するクラス。
/// </summary>
public partial class BattleScene(RenderService render, PhysicsEngine physicsEngine) : IDisposable
{
    private readonly RenderService _render = render ?? throw new ArgumentNullException(nameof(render));
    private readonly PhysicsEngine _physicsEngine = physicsEngine ?? throw new ArgumentNullException(nameof(physicsEngine));
    private readonly Camera _camera = new(640.0f, 360.0f, 1.0f, 0.5f, 2.0f, 5.0f, 200.0f, 150.0f);
    private readonly StageManager _stageManager = new();
    private readonly BattleManager _battleManager = new();
    private readonly AnimationManager _animationManager = new();
    private readonly InputManager _input = new();

    private readonly CharacterAnimationType[] _playerCurrentAnims = [CharacterAnimationType.Idle, CharacterAnimationType.Idle, CharacterAnimationType.Idle, CharacterAnimationType.Idle];
    private bool _showDebugBoxes = true;
    private bool _isDisposed;

    /// <summary>
    /// バトルシーンに必要なアニメーションマネージャー、ステージ、およびバトルマネージャー (初期 2人) の初期化を行います。
    /// </summary>
    public void Initialize()
    {
        _animationManager.Initialize(4);

        _stageManager.LoadMap(_physicsEngine, 0);
        _battleManager.Initialize(_physicsEngine, _stageManager.CurrentMap, playerCount: 2);
    }

    /// <summary>
    /// シーンのアニメーションマネージャーおよびカメラリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _camera.Dispose();
        _animationManager.Dispose();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
