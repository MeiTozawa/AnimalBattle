using AnimalBattle.Animations;
using AnimalBattle.Battle;
using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Stages;

namespace AnimalBattle.Scenes;

/// <summary>
/// 対戦バトルシーンのライフサイクル、カメラ制御、およびレンダリング描画を管理するクラス。
/// プレイヤー管理および戦闘物理シミュレーションは <see cref="BattleManager"/> へ委譲します。
/// </summary>
/// <remarks>
/// レンダリングサービスおよび物理エンジンハンドルを指定して <see cref="BattleScene"/> インスタンスを初期化します。
/// </remarks>
/// <param name="render">レンダリングサービスインスタンス</param>
/// <param name="physicsEngine">物理エンジンインスタンス</param>
public class BattleScene(RenderService render, PhysicsEngine physicsEngine) : IDisposable
{
    private readonly RenderService _render = render ?? throw new ArgumentNullException(nameof(render));
    private readonly PhysicsEngine _physicsEngine = physicsEngine ?? throw new ArgumentNullException(nameof(physicsEngine));
    private readonly Camera _camera = new(640.0f, 360.0f, 1.0f, 0.5f, 2.0f, 5.0f, 200.0f, 150.0f);
    private readonly StageManager _stageManager = new();
    private readonly BattleManager _battleManager = new();
    private readonly AnimationManager _animationManager = new AnimationManager();
    private readonly InputManager _input = new();

    private string _p1CurrentAnim = "Idle";
    private string _p2CurrentAnim = "Idle";
    private bool _showDebugBoxes = true;
    private bool _isDisposed;

    /// <summary>
    /// バトルシーンに必要なアニメーションマネージャー、ステージ、およびバトルマネージャーの初期化を行います。
    /// </summary>
    public void Initialize()
    {
        _animationManager.Initialize();

        _stageManager.LoadMap(_physicsEngine, 0);
        _battleManager.Initialize(_physicsEngine, _stageManager.CurrentMap);
    }

    /// <summary>
    /// シーン入力の受信、マップ切替、バトルマネージャーによる物理・戦闘更新、およびカメラ更新を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    /// <returns>ゲームを継続する場合は true、終了を要求する場合は false</returns>
    public bool Update(float deltaTime)
    {
        _input.Update();

        if (_input.IsKeyDown(InputManager.KeyInputEscape))
        {
            return false;
        }

        if (_input.IsDebugTogglePressed())
        {
            _showDebugBoxes = !_showDebugBoxes;
            Console.WriteLine($"[Debug 表示] {(_showDebugBoxes ? "ON" : "OFF")}");
        }

        int switchMapIndex = _input.GetRequestedMapSwitchIndex();
        if (switchMapIndex != -1 && switchMapIndex != _stageManager.CurrentMapIndex)
        {
            if (_stageManager.LoadMap(_physicsEngine, switchMapIndex).IsOk)
            {
                _battleManager.RecreatePlayers(_physicsEngine, _stageManager.CurrentMap);
            }
        }

        _battleManager.Update(_physicsEngine, _input, _stageManager.CurrentMap, deltaTime);

        Character player1 = _battleManager.Player1;
        Character player2 = _battleManager.Player2;

        NativePhysicsObject p1Obj = default;
        NativePhysicsObject p2Obj = default;
        unsafe
        {
            _physicsEngine.GetObjectState(player1.PhysicsObjectId, &p1Obj);
            _physicsEngine.GetObjectState(player2.PhysicsObjectId, &p2Obj);
        }

        // アニメーション状態の判定と更新
        if (_animationManager.HasCatSprite)
        {
            static string UpdateStableAnim(string prevAnim, CharacterStateMachine sm, ref NativePhysicsObject pObj)
            {
                if (sm.State == CharacterState.Hitstun) return "Hurt";
                if (sm.State == CharacterState.Attacking) return "Attack";
                if (sm.State == CharacterState.Airborne || sm.State == CharacterState.FastFalling) return "Jump";

                float speedX = Math.Abs(pObj.Vx);

                if (prevAnim == "Run")
                {
                    if (speedX > 60.0f) return "Run";
                    if (speedX > 10.0f) return "Walk";
                    return "Idle";
                }
                if (prevAnim == "Walk")
                {
                    if (speedX > 120.0f) return "Run";
                    if (speedX > 5.0f) return "Walk";
                    return "Idle";
                }

                if (speedX > 120.0f) return "Run";
                if (speedX > 15.0f) return "Walk";
                return "Idle";
            }

            if (player1.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.35f })
            {
                _animationManager.PlayEffectAnimation(1, "Slash");
            }
            if (player2.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.35f })
            {
                _animationManager.PlayEffectAnimation(2, "Slash");
            }

            _p1CurrentAnim = UpdateStableAnim(_p1CurrentAnim, player1.StateMachine, ref p1Obj);
            _animationManager.PlayCharacterAnimation(1, _p1CurrentAnim);

            _p2CurrentAnim = UpdateStableAnim(_p2CurrentAnim, player2.StateMachine, ref p2Obj);
            _animationManager.PlayCharacterAnimation(2, _p2CurrentAnim);

            _animationManager.Update(deltaTime);
        }

        // カメラ追従ターゲットの更新
        Point<float>[] targets =
        [
            new Point<float>(p1Obj.X, p1Obj.Y - p1Obj.Height / 2.0f),
            new Point<float>(p2Obj.X, p2Obj.Y - p2Obj.Height / 2.0f)
        ];

        _camera.Update(deltaTime, targets, 1280.0f, 720.0f);

        return true;
    }

    /// <summary>
    /// バトルシーンのワールド、キャラクター、エフェクト、および HUD UI を描画します。
    /// </summary>
    public void Render()
    {
        MapData currentMap = _stageManager.CurrentMap;
        Character player1 = _battleManager.Player1;
        Character player2 = _battleManager.Player2;

        NativePhysicsObject p1Obj = default;
        NativePhysicsObject p2Obj = default;
        unsafe
        {
            _physicsEngine.GetObjectState(player1.PhysicsObjectId, &p1Obj);
            _physicsEngine.GetObjectState(player2.PhysicsObjectId, &p2Obj);
        }

        var (camX, camY, camScale) = _camera.GetState();

        _render.SetCameraEnabled(true);
        _render.SetCameraState(camX, camY, camScale);

        // 背景描画
        _render.DrawBox(new Rect<int>(0, 0, 1280, 720), currentMap.BgColor, true);

        // 各プラットフォームの描画
        foreach (var plat in currentMap.Platforms)
        {
            int px1 = (int)plat.X1;
            int py1 = (int)plat.Y1;
            int px2 = (int)plat.X2;
            int py2 = (int)plat.Y2;

            _render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.FillColor, true);

            if (plat.Type == PlatformType.SemiSolid)
            {
                _render.DrawBox(new Rect<int>(px1, py1, px2, py1 + 2), 0xFFFFFFFF, true);
            }
        }

        // Player 1 スプライト描画
        int p1X1 = (int)(p1Obj.X - p1Obj.Width / 2.0f);
        int p1Y1 = (int)(p1Obj.Y - p1Obj.Height);
        int p1X2 = (int)(p1Obj.X + p1Obj.Width / 2.0f);
        int p1Y2 = (int)p1Obj.Y;
        uint p1Color = (player1.StateMachine.State == CharacterState.Hitstun) ?
            (((int)(p1Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
            ((player1.StateMachine.State == CharacterState.Grounded) ? player1.MainColor : 0xFF2980B9);

        float spriteScale = 2.5f;
        float spriteHalfHeight = (32.0f * spriteScale) / 2.0f;

        if (_animationManager.HasCatSprite)
        {
            var catPos = new Point<int>((int)MathF.Round(p1Obj.X), (int)MathF.Round(p1Obj.Y - spriteHalfHeight));
            _animationManager.DrawCharacter(_render, 1, catPos, facingLeft: player1.FacingLeft, scale: spriteScale);
        }
        else
        {
            _render.DrawBox(new Rect<int>(p1X1, p1Y1, p1X2, p1Y2), p1Color, true);
        }

        // Player 2 スプライト描画
        int p2X1 = (int)(p2Obj.X - p2Obj.Width / 2.0f);
        int p2Y1 = (int)(p2Obj.Y - p2Obj.Height);
        int p2X2 = (int)(p2Obj.X + p2Obj.Width / 2.0f);
        int p2Y2 = (int)p2Obj.Y;
        uint p2Color = (player2.StateMachine.State == CharacterState.Hitstun) ?
            (((int)(p2Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
            ((player2.StateMachine.State == CharacterState.Grounded) ? player2.MainColor : 0xFF8E44AD);

        if (_animationManager.HasCatSprite)
        {
            var p2CatPos = new Point<int>((int)MathF.Round(p2Obj.X), (int)MathF.Round(p2Obj.Y - spriteHalfHeight));
            _animationManager.DrawCharacter(_render, 2, p2CatPos, facingLeft: player2.FacingLeft, scale: spriteScale);
        }
        else
        {
            _render.DrawBox(new Rect<int>(p2X1, p2Y1, p2X2, p2Y2), p2Color, true);
        }

        // 攻撃エフェクト描画 (P1 & P2)
        if (_animationManager.HasEffectSprite)
        {
            if (player1.StateMachine.State == CharacterState.Attacking)
            {
                var hitBounds = player1.Hitbox.GetWorldBounds(p1Obj.X, p1Obj.Y, player1.FacingLeft);
                var fxPos = new Point<int>((int)MathF.Round((hitBounds.X1 + hitBounds.X2) / 2.0f), (int)MathF.Round((hitBounds.Y1 + hitBounds.Y2) / 2.0f));
                _animationManager.DrawEffect(_render, 1, fxPos, facingLeft: player1.FacingLeft, scale: 2.0f);
            }
            if (player2.StateMachine.State == CharacterState.Attacking)
            {
                var hitBounds = player2.Hitbox.GetWorldBounds(p2Obj.X, p2Obj.Y, player2.FacingLeft);
                var fxPos = new Point<int>((int)MathF.Round((hitBounds.X1 + hitBounds.X2) / 2.0f), (int)MathF.Round((hitBounds.Y1 + hitBounds.Y2) / 2.0f));
                _animationManager.DrawEffect(_render, 2, fxPos, facingLeft: player2.FacingLeft, scale: 2.0f);
            }
        }

        // Debug 判定枠表示 (Hurtbox & Hitbox)
        if (_showDebugBoxes)
        {
            var p1HurtBounds = player1.Hurtbox.GetWorldBounds(p1Obj.X, p1Obj.Y, player1.FacingLeft);
            _render.DrawBox(new Rect<int>((int)p1HurtBounds.X1, (int)p1HurtBounds.Y1, (int)p1HurtBounds.X2, (int)p1HurtBounds.Y2), 0xFF00FF00, false);

            var p2HurtBounds = player2.Hurtbox.GetWorldBounds(p2Obj.X, p2Obj.Y, player2.FacingLeft);
            _render.DrawBox(new Rect<int>((int)p2HurtBounds.X1, (int)p2HurtBounds.Y1, (int)p2HurtBounds.X2, (int)p2HurtBounds.Y2), 0xFF00FF00, false);

            if (player1.Hitbox.IsActive)
            {
                var p1HitBounds = player1.Hitbox.GetWorldBounds(p1Obj.X, p1Obj.Y, player1.FacingLeft);
                _render.DrawBox(new Rect<int>((int)p1HitBounds.X1, (int)p1HitBounds.Y1, (int)p1HitBounds.X2, (int)p1HitBounds.Y2), 0xFFFF0000, true);
                _render.DrawBox(new Rect<int>((int)p1HitBounds.X1, (int)p1HitBounds.Y1, (int)p1HitBounds.X2, (int)p1HitBounds.Y2), 0xFFFFFFFF, false);
            }

            if (player2.Hitbox.IsActive)
            {
                var p2HitBounds = player2.Hitbox.GetWorldBounds(p2Obj.X, p2Obj.Y, player2.FacingLeft);
                _render.DrawBox(new Rect<int>((int)p2HitBounds.X1, (int)p2HitBounds.Y1, (int)p2HitBounds.X2, (int)p2HurtBounds.Y2), 0xFFFF0000, true);
                _render.DrawBox(new Rect<int>((int)p2HitBounds.X1, (int)p2HitBounds.Y1, (int)p2HitBounds.X2, (int)p2HurtBounds.Y2), 0xFFFFFFFF, false);
            }
        }

        // HUD UI 描画（カメラ変換オフ）
        _render.SetCameraEnabled(false);

        _render.DrawString(new Point<int>(30, 15), $"=== {currentMap.Name} ===", currentMap.ThemeColor);
        _render.DrawString(new Point<int>(30, 37), $"{currentMap.Subtitle}", 0xFFCCCCCC);
        _render.DrawString(new Point<int>(30, 59), $"Camera: ({camX:F1}, {camY:F1}) | Zoom: {camScale:F2}x | Debug (F1): {(_showDebugBoxes ? "ON" : "OFF")}", 0xFF00FF00);

        // Player 1 HUD (左側)
        int p1HudX = 30;
        int p1HudY = 620;
        _render.DrawString(new Point<int>(p1HudX, p1HudY), $"{player1.Name}", player1.MainColor);
        _render.DrawString(new Point<int>(p1HudX, p1HudY + 20), $"Damage: {p1Obj.DamagePercent:F1}%", 0xFFFFEAA7);
        _render.DrawString(new Point<int>(p1HudX, p1HudY + 40), $"State: {player1.StateMachine.State} | Pos: ({p1Obj.X:F0}, {p1Obj.Y:F0})", 0xFFDFE6E9);

        // Player 2 HUD (右側)
        int p2HudX = 980;
        int p2HudY = 620;
        _render.DrawString(new Point<int>(p2HudX, p2HudY), $"{player2.Name}", player2.MainColor);
        _render.DrawString(new Point<int>(p2HudX, p2HudY + 20), $"Damage: {p2Obj.DamagePercent:F1}%", 0xFFFFEAA7);
        _render.DrawString(new Point<int>(p2HudX, p2HudY + 40), $"State: {player2.StateMachine.State} | Pos: ({p2Obj.X:F0}, {p2Obj.Y:F0})", 0xFFDFE6E9);

        // 操作説明
        _render.DrawString(new Point<int>(30, 100), "[1-4] Switch Map | [F1] Debug Box | [P1] WASD+F/J | [P2] Arrow+G/L", 0xFF888888);
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
