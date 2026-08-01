using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnimalBattle.Characters;
using AnimalBattle.Core;
using AnimalBattle.Inputs;
using AnimalBattle.Stages;

namespace AnimalBattle.Battle;

/// <summary>
/// 対戦バトルにおける動的プレイヤー群 (1～4人) の生成、物理状態同期、入力伝達、ヒット判定、および撃墜判定を管理するクラス。
/// </summary>
public class BattleManager
{
    private readonly List<Character> _players = [];
    private static readonly (uint Color, uint Border)[] _playerColors =
    [
        (0xFF3498DB, 0xFF2980B9), // P1: Blue
        (0xFF9B59B6, 0xFF8E44AD), // P2: Purple
        (0xFFE74C3C, 0xFFC0392B), // P3: Red
        (0xFF2ECC71, 0xFF27AE60)  // P4: Green
    ];

    /// <summary>
    /// 現在参戦中のプレイヤーリストを取得します。
    /// </summary>
    public ReadOnlyCollection<Character> Players => _players.AsReadOnly();

    /// <summary>
    /// Player 1 のキャラクターインスタンスを取得します。（存在しない場合は null）
    /// </summary>
    public Character? Player1 => _players.Count > 0 ? _players[0] : null;

    /// <summary>
    /// Player 2 のキャラクターインスタンスを取得します。（存在しない場合は null）
    /// </summary>
    public Character? Player2 => _players.Count > 1 ? _players[1] : null;

    /// <summary>
    /// 指定された人数 (1～4人) および初期マップに基づき、プレイヤーの物理オブジェクト登録と状態初期化を行います。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="initialMap">初期マップデータ</param>
    /// <param name="playerCount">参戦人数 (1～4)</param>
    public void Initialize(PhysicsEngine physics, MapData initialMap, int playerCount = 2)
    {
        playerCount = Math.Clamp(playerCount, 1, 4);
        _players.Clear();

        for (int i = 1; i <= playerCount; i++)
        {
            var (color, border) = _playerColors[(i - 1) % _playerColors.Length];
            var character = new Character(
                playerIndex: i,
                name: $"Player {i}",
                mainColor: color,
                outlineColor: border,
                teamId: i,
                playerType: PlayerType.Human,
                stockCount: 3);
            Point<float> spawn = initialMap.GetSpawnPosition(i);

            character.PhysicsObjectId = physics.AddObject(
                spawn.X, spawn.Y, character.Config.Width, character.Config.Height, character.Config.Weight);
            character.StateMachine.Reset();

            _players.Add(character);
        }
    }

    /// <summary>
    /// マップ切り替え時に既存の全プレイヤーの物理オブジェクトを再作成し、新しいマップのスポーン位置に配置します。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="newMap">切り替え後の新しいマップデータ</param>
    public void RecreatePlayers(PhysicsEngine physics, MapData newMap)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            physics.RemoveObject(player.PhysicsObjectId);

            Point<float> spawn = newMap.GetSpawnPosition(player.PlayerIndex);
            player.PhysicsObjectId = physics.AddObject(
                spawn.X, spawn.Y, player.Config.Width, player.Config.Height, player.Config.Weight);
            player.StateMachine.Reset();
        }
    }

    /// <summary>
    /// 全プレイヤーの入力処理、物理ステップ、バーストゾーン判定、向き更新、および多対多 Hitbox 衝突判定を行います。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="input">入力マネージャー</param>
    /// <param name="currentMap">カレントマップデータ</param>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(PhysicsEngine physics, InputManager input, MapData currentMap, float deltaTime)
    {
        int count = _players.Count;
        if (count == 0) return;

        Span<NativePhysicsObject> pObjs = stackalloc NativePhysicsObject[count];

        // 1. 各プレイヤーの物理状態の取得
        for (int i = 0; i < count; i++)
        {
            physics.GetObjectState(_players[i].PhysicsObjectId, out pObjs[i]);
        }

        // 2. 入力判定およびステートマシンの更新
        for (int i = 0; i < count; i++)
        {
            var player = _players[i];
            player.StateMachine.Update(ref pObjs[i], input, player.PlayerIndex, player.Config, deltaTime);
        }

        // 3. 更新された物理状態を Native エンジンに書き戻し
        for (int i = 0; i < count; i++)
        {
            physics.SetObjectState(_players[i].PhysicsObjectId, in pObjs[i]);
        }

        // 4. 物理エンジンの 1 ステップ進める
        physics.Step(deltaTime);

        // 5. 物理ステップ後の最新状態を再取得
        for (int i = 0; i < count; i++)
        {
            physics.GetObjectState(_players[i].PhysicsObjectId, out pObjs[i]);
        }

        // 6. 着地判定および足場衝突応答
        for (int i = 0; i < count; i++)
        {
            _players[i].StateMachine.HandleCollisions(ref pObjs[i]);
        }

        for (int i = 0; i < count; i++)
        {
            physics.SetObjectState(_players[i].PhysicsObjectId, in pObjs[i]);
        }

        // 7. バーストゾーン脱落判定およびリスポーン
        Rect<float> blastZone = currentMap.BlastZone;
        for (int i = 0; i < count; i++)
        {
            var player = _players[i];
            if (!blastZone.Contains(pObjs[i].X, pObjs[i].Y))
            {
                if (player.StockCount > 0)
                {
                    player.StockCount--;
                }

                physics.RemoveObject(player.PhysicsObjectId);
                Point<float> spawn = currentMap.GetSpawnPosition(player.PlayerIndex);
                player.PhysicsObjectId = physics.AddObject(
                    spawn.X, spawn.Y, player.Config.Width, player.Config.Height, player.Config.Weight);
                player.StateMachine.Reset();
                Console.WriteLine($"[撃墜!] {player.Name} が {currentMap.Name} で撃墜されました！（残りストック: {player.StockCount}）");
            }

            // 8. 移動速度に応じた向き (FacingLeft) 更新
            if (pObjs[i].Vx < -5.0f) player.FacingLeft = true;
            else if (pObjs[i].Vx > 5.0f) player.FacingLeft = false;

            // 9. Hitbox アクティブ状態の設定
            bool isAttacking = player.StateMachine is { State: CharacterState.Attacking, AttackTimer: > 0.10f };
            if (isAttacking && !player.Hitbox.IsActive)
            {
                player.Hitbox.ResetHitTargets();
            }
            player.Hitbox.IsActive = isAttacking;
        }

        // 10. 多対多 (All-vs-All) Hitbox vs Hurtbox 相互当たり判定
        for (int attackerIdx = 0; attackerIdx < count; attackerIdx++)
        {
            var attacker = _players[attackerIdx];
            if (!attacker.Hitbox.IsActive) continue;

            for (int defenderIdx = 0; defenderIdx < count; defenderIdx++)
            {
                if (attackerIdx == defenderIdx) continue; // 自分自身にはヒットしない

                var defender = _players[defenderIdx];
                if (attacker.Hitbox.Intersects(
                    defender.Hurtbox,
                    pObjs[attackerIdx].X, pObjs[attackerIdx].Y, attacker.FacingLeft,
                    pObjs[defenderIdx].X, pObjs[defenderIdx].Y, defender.FacingLeft))
                {
                    float angle = attacker.FacingLeft ? (float)(Math.PI - attacker.Hitbox.KnockbackAngle) : attacker.Hitbox.KnockbackAngle;
                    physics.ApplyKnockback(defender.PhysicsObjectId, attacker.Hitbox.Damage, angle, attacker.Hitbox.KnockbackForce, 1.2f);
                    defender.StateMachine.Reset();
                    attacker.Hitbox.RegisterHit(defender.Hurtbox.OwnerIndex);
                    Console.WriteLine($"[Hitbox ヒット!] {attacker.Name} の Hitbox が {defender.Name} の Hurtbox に命中！");
                }
            }
        }
    }
}
