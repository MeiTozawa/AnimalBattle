using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Stages;

namespace AnimalBattle.Battle;

/// <summary>
/// 対戦バトルにおけるプレイヤーの生成、物理状態同期、入力伝達、ヒット判定、および撃墜判定を管理するクラス。
/// </summary>
public class BattleManager
{
    private readonly Character _player1 = new(1, "Player 1", 0xFF3498DB, 0xFF2980B9);
    private readonly Character _player2 = new(2, "Player 2", 0xFF9B59B6, 0xFF8E44AD);

    /// <summary>
    /// Player 1 のキャラクターインスタンスを取得します。
    /// </summary>
    public Character Player1 => _player1;

    /// <summary>
    /// Player 2 のキャラクターインスタンスを取得します。
    /// </summary>
    public Character Player2 => _player2;

    /// <summary>
    /// 指定された初期マップのスポーン位置に基づき、プレイヤーの物理オブジェクト登録と状態リセットを行います。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="initialMap">初期マップデータ</param>
    public void Initialize(PhysicsEngine physics, MapData initialMap)
    {
        _player1.PhysicsObjectId = physics.AddObject(
            initialMap.P1Spawn.X, initialMap.P1Spawn.Y, _player1.Config.Width, _player1.Config.Height, _player1.Config.Weight);
        _player2.PhysicsObjectId = physics.AddObject(
            initialMap.P2Spawn.X, initialMap.P2Spawn.Y, _player2.Config.Width, _player2.Config.Height, _player2.Config.Weight);

        _player1.StateMachine.Reset();
        _player2.StateMachine.Reset();
    }

    /// <summary>
    /// マップ切り替え時に既存のプレイヤー物理オブジェクトを削除し、新しいマップのスポーン位置に再生成します。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="newMap">切り替え後の新しいマップデータ</param>
    public void RecreatePlayers(PhysicsEngine physics, MapData newMap)
    {
        physics.RemoveObject(_player1.PhysicsObjectId);
        physics.RemoveObject(_player2.PhysicsObjectId);

        _player1.PhysicsObjectId = physics.AddObject(
            newMap.P1Spawn.X, newMap.P1Spawn.Y, _player1.Config.Width, _player1.Config.Height, _player1.Config.Weight);
        _player2.PhysicsObjectId = physics.AddObject(
            newMap.P2Spawn.X, newMap.P2Spawn.Y, _player2.Config.Width, _player2.Config.Height, _player2.Config.Weight);

        _player1.StateMachine.Reset();
        _player2.StateMachine.Reset();
    }

    /// <summary>
    /// 各プレイヤーの入力処理、物理ステップ、バーストゾーン判定、向き更新、および Hitbox 衝突判定を行います。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="input">入力マネージャー</param>
    /// <param name="currentMap">カレントマップデータ</param>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(PhysicsEngine physics, InputManager input, MapData currentMap, float deltaTime)
    {
        NativePhysicsObject p1Obj = default;
        NativePhysicsObject p2Obj = default;
        unsafe
        {
            physics.GetObjectState(_player1.PhysicsObjectId, &p1Obj);
            physics.GetObjectState(_player2.PhysicsObjectId, &p2Obj);
        }

        _player1.StateMachine.Update(ref p1Obj, input, _player1.PlayerIndex, _player1.Config, deltaTime);
        _player2.StateMachine.Update(ref p2Obj, input, _player2.PlayerIndex, _player2.Config, deltaTime);

        unsafe
        {
            physics.SetObjectState(_player1.PhysicsObjectId, &p1Obj);
            physics.SetObjectState(_player2.PhysicsObjectId, &p2Obj);
        }

        physics.Step(deltaTime);

        unsafe
        {
            physics.GetObjectState(_player1.PhysicsObjectId, &p1Obj);
            physics.GetObjectState(_player2.PhysicsObjectId, &p2Obj);
        }

        _player1.StateMachine.HandleCollisions(ref p1Obj);
        _player2.StateMachine.HandleCollisions(ref p2Obj);

        unsafe
        {
            physics.SetObjectState(_player1.PhysicsObjectId, &p1Obj);
            physics.SetObjectState(_player2.PhysicsObjectId, &p2Obj);
        }

        Rect<float> blastZone = currentMap.BlastZone;
        if (!blastZone.Contains(p1Obj.X, p1Obj.Y))
        {
            physics.RemoveObject(_player1.PhysicsObjectId);
            _player1.PhysicsObjectId = physics.AddObject(
                currentMap.P1Spawn.X, currentMap.P1Spawn.Y, _player1.Config.Width, _player1.Config.Height, _player1.Config.Weight);
            _player1.StateMachine.Reset();
            Console.WriteLine($"[撃墜!] {_player1.Name} が {currentMap.Name} で撃墜されました！");
        }
        if (!blastZone.Contains(p2Obj.X, p2Obj.Y))
        {
            physics.RemoveObject(_player2.PhysicsObjectId);
            _player2.PhysicsObjectId = physics.AddObject(
                currentMap.P2Spawn.X, currentMap.P2Spawn.Y, _player2.Config.Width, _player2.Config.Height, _player2.Config.Weight);
            _player2.StateMachine.Reset();
            Console.WriteLine($"[撃墜!] {_player2.Name} が {currentMap.Name} で撃墜されました！");
        }

        if (p1Obj.Vx < -5.0f) _player1.FacingLeft = true;
        else if (p1Obj.Vx > 5.0f) _player1.FacingLeft = false;

        if (p2Obj.Vx < -5.0f) _player2.FacingLeft = true;
        else if (p2Obj.Vx > 5.0f) _player2.FacingLeft = false;

        bool p1Attacking = _player1.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.10f };
        if (p1Attacking && !_player1.Hitbox.IsActive)
        {
            _player1.Hitbox.ResetHitTargets();
        }
        _player1.Hitbox.IsActive = p1Attacking;

        bool p2Attacking = _player2.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.10f };
        if (p2Attacking && !_player2.Hitbox.IsActive)
        {
            _player2.Hitbox.ResetHitTargets();
        }
        _player2.Hitbox.IsActive = p2Attacking;

        if (_player1.Hitbox.Intersects(_player2.Hurtbox, p1Obj.X, p1Obj.Y, _player1.FacingLeft, p2Obj.X, p2Obj.Y, _player2.FacingLeft))
        {
            float angle = _player1.FacingLeft ? (float)(Math.PI - _player1.Hitbox.KnockbackAngle) : _player1.Hitbox.KnockbackAngle;
            physics.ApplyKnockback(_player2.PhysicsObjectId, _player1.Hitbox.Damage, angle, _player1.Hitbox.KnockbackForce, 1.2f);
            _player2.StateMachine.Reset();
            _player1.Hitbox.RegisterHit(_player2.Hurtbox.OwnerIndex);
            Console.WriteLine($"[Hitbox ヒット!] {_player1.Name} の Hitbox が {_player2.Name} の Hurtbox に命中！");
        }

        if (_player2.Hitbox.Intersects(_player1.Hurtbox, p2Obj.X, p2Obj.Y, _player2.FacingLeft, p1Obj.X, p1Obj.Y, _player1.FacingLeft))
        {
            float angle = _player2.FacingLeft ? (float)(Math.PI - _player2.Hitbox.KnockbackAngle) : _player2.Hitbox.KnockbackAngle;
            physics.ApplyKnockback(_player1.PhysicsObjectId, _player2.Hitbox.Damage, angle, _player2.Hitbox.KnockbackForce, 1.2f);
            _player1.StateMachine.Reset();
            _player2.Hitbox.RegisterHit(_player1.Hurtbox.OwnerIndex);
            Console.WriteLine($"[Hitbox ヒット!] {_player2.Name} の Hitbox が {_player1.Name} の Hurtbox に命中！");
        }
    }
}
