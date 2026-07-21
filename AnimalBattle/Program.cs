using System;
using System.Diagnostics;
using AnimalBattle.Bindings;

namespace AnimalBattle;

/// <summary>
/// アプリケーションのエントリポイントおよびメインゲームループを管理するクラス。
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        const int Width = 1280;
        const int Height = 720;

        Console.WriteLine("C++ DxLib ネイティブエンジンを初期化中...");
        if (NativeEngine.Initialize(Width, Height, true) == -1)
        {
            Console.WriteLine("ゲームエンジンの初期化に失敗しました！");
            return;
        }

        IntPtr nativeRenderService = NativeEngine.GetRenderService();
        var render = new RenderService(nativeRenderService);

        // 物理エンジンの初期化
        Console.WriteLine("物理エンジンを初期化中...");
        IntPtr physics = NativeEngine.PhysicsCreate();

        // Native C++ カメラモジュールの初期化
        Console.WriteLine("ネイティブカメラモジュールを初期化中...");
        IntPtr camera = NativeEngine.CameraCreate();
        NativeEngine.CameraSetConfig(camera, minScale: 0.45f, maxScale: 1.0f, lerpSpeed: 4.5f, paddingX: 280.0f, paddingY: 180.0f);

        // ステージマネージャーの初期化および初期マップ (Map 1) のロード
        Console.WriteLine("ステージマネージャーを初期化中 & マップ 1 を読み込み中...");
        var stageManager = new StageManager();
        stageManager.LoadMap(physics, 0);

        MapData currentMap = stageManager.CurrentMap;
        NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);

        // プレイヤーキャラクターの初期化
        var player1 = new Character(1, "Player 1", 0xFF3498DB, 0xFF1F618D);
        var player2 = new Character(2, "Player 2", 0xFF9B59B6, 0xFF5B2C6F);

        // 現在のマップのスポーン地点に 2 名のプレイヤーを生成
        player1.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, player1.Config.Width, player1.Config.Height, player1.Config.Weight);
        player2.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, player2.Config.Width, player2.Config.Height, player2.Config.Weight);

        player1.StateMachine.Reset();
        player2.StateMachine.Reset();

        // 入力マネージャーの初期化
        var input = new InputManager();

        var stopwatch = Stopwatch.StartNew();
        long lastTime = stopwatch.ElapsedTicks;
        double tickFrequency = (double)Stopwatch.Frequency;

        Console.WriteLine("メインゲームループを開始します...");
        while (NativeEngine.ProcessMessage() == 0 && !NativeEngine.IsExitRequested())
        {
            long currentTime = stopwatch.ElapsedTicks;
            float deltaTime = (float)((currentTime - lastTime) / tickFrequency);
            lastTime = currentTime;

            // 入力状態更新
            input.Update();

            // ESC キーでゲーム終了
            if (input.IsKeyDown(InputManager.KeyInputEscape))
            {
                break;
            }

            // リアルタイムマップ切り替え (1, 2, 3, 4 キー)
            int switchMapIndex = input.GetRequestedMapSwitchIndex();
            if (switchMapIndex != -1 && switchMapIndex != stageManager.CurrentMapIndex)
            {
                stageManager.LoadMap(physics, switchMapIndex);
                currentMap = stageManager.CurrentMap;

                // 新ステージへプレイヤーを再生成
                NativeEngine.PhysicsRemoveObject(physics, player1.PhysicsObjectId);
                NativeEngine.PhysicsRemoveObject(physics, player2.PhysicsObjectId);
                player1.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, player1.Config.Width, player1.Config.Height, player1.Config.Weight);
                player2.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, player2.Config.Width, player2.Config.Height, player2.Config.Weight);

                player1.StateMachine.Reset();
                player2.StateMachine.Reset();
                NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);
                continue;
            }

            // 外部 physics.toml 設定変更の同期
            player1.Config.SyncFromConfig();
            player2.Config.SyncFromConfig();

            // C++ からプレイヤー状態を取得
            NativeEngine.NativePhysicsObject p1Obj = default;
            NativeEngine.NativePhysicsObject p2Obj = default;
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, player1.PhysicsObjectId, &p1Obj);
                NativeEngine.PhysicsGetObjectState(physics, player2.PhysicsObjectId, &p2Obj);
            }

            // R キーで両プレイヤーをリセット
            if (input.IsResetPressed())
            {
                NativeEngine.PhysicsRemoveObject(physics, player1.PhysicsObjectId);
                NativeEngine.PhysicsRemoveObject(physics, player2.PhysicsObjectId);
                player1.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, player1.Config.Width, player1.Config.Height, player1.Config.Weight);
                player2.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, player2.Config.Width, player2.Config.Height, player2.Config.Weight);
                player1.StateMachine.Reset();
                player2.StateMachine.Reset();
                NativeEngine.CameraReset(camera, 640.0f, 360.0f, 1.0f);
                continue;
            }

            // テスト用吹っ飛びの適用 (P1: F キー, P2: G キー)
            if (input.IsKnockbackTestPressed(1))
            {
                NativeEngine.PhysicsApplyKnockback(physics, player1.PhysicsObjectId, 25.0f, (float)(Math.PI / 4.0), 450.0f, 1.3f);
                player1.StateMachine.Reset();
            }
            if (input.IsKnockbackTestPressed(2))
            {
                NativeEngine.PhysicsApplyKnockback(physics, player2.PhysicsObjectId, 25.0f, (float)(3.0 * Math.PI / 4.0), 450.0f, 1.3f);
                player2.StateMachine.Reset();
            }

            // キャラクターのステートマシン更新
            player1.StateMachine.Update(ref p1Obj, input, playerIndex: 1, player1.Config, deltaTime);
            player2.StateMachine.Update(ref p2Obj, input, playerIndex: 2, player2.Config, deltaTime);

            // 変更された速度・座標を C++ へ書き戻し
            unsafe
            {
                NativeEngine.PhysicsSetObjectState(physics, player1.PhysicsObjectId, &p1Obj);
                NativeEngine.PhysicsSetObjectState(physics, player2.PhysicsObjectId, &p2Obj);
            }

            // C++ 物理ステップ実行
            NativeEngine.PhysicsStep(physics, deltaTime);

            // 最終解決座標および衝突フラグを取得
            unsafe
            {
                NativeEngine.PhysicsGetObjectState(physics, player1.PhysicsObjectId, &p1Obj);
                NativeEngine.PhysicsGetObjectState(physics, player2.PhysicsObjectId, &p2Obj);
            }

            // 衝突応答処理
            player1.StateMachine.HandleCollisions(ref p1Obj);
            player2.StateMachine.HandleCollisions(ref p2Obj);

            // バーストゾーン（撃墜判定領域）チェック
            Rect<float> blastZone = currentMap.BlastZone;
            if (p1Obj.X < blastZone.X1 || p1Obj.X > blastZone.X2 || p1Obj.Y < blastZone.Y1 || p1Obj.Y > blastZone.Y2)
            {
                NativeEngine.PhysicsRemoveObject(physics, player1.PhysicsObjectId);
                player1.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P1Spawn.X, currentMap.P1Spawn.Y, player1.Config.Width, player1.Config.Height, player1.Config.Weight);
                player1.StateMachine.Reset();
                Console.WriteLine($"[撃墜!] {player1.Name} が {currentMap.Name} で撃墜されました！");
            }
            if (p2Obj.X < blastZone.X1 || p2Obj.X > blastZone.X2 || p2Obj.Y < blastZone.Y1 || p2Obj.Y > blastZone.Y2)
            {
                NativeEngine.PhysicsRemoveObject(physics, player2.PhysicsObjectId);
                player2.PhysicsObjectId = NativeEngine.PhysicsAddObject(physics, currentMap.P2Spawn.X, currentMap.P2Spawn.Y, player2.Config.Width, player2.Config.Height, player2.Config.Weight);
                player2.StateMachine.Reset();
                Console.WriteLine($"[撃墜!] {player2.Name} が {currentMap.Name} で撃墜されました！");
            }

            // カメラ追従ターゲットの更新
            Point<float>[] targets =
            [
                new Point<float>(p1Obj.X, p1Obj.Y - p1Obj.Height / 2.0f),
                new Point<float>(p2Obj.X, p2Obj.Y - p2Obj.Height / 2.0f)
            ];

            float camX, camY, camScale;
            unsafe
            {
                fixed (Point<float>* pTargets = targets)
                {
                    NativeEngine.CameraUpdate(camera, deltaTime, pTargets, 2, Width, Height);
                }
                NativeEngine.CameraGetState(camera, &camX, &camY, &camScale);
            }

            // フレーム描画開始
            NativeEngine.BeginFrame();

            // ワールドオブジェクト用カメラ変換の設定
            render.SetCameraState(camX, camY, camScale);
            render.SetCameraEnabled(true);

            // バーストゾーン境界線の描画
            render.DrawBox(new Rect<int>((int)blastZone.X1, (int)blastZone.Y1, (int)blastZone.X2, (int)blastZone.Y2), 0xFF333333, false);

            // 現在のマップの足場群を描画
            foreach (var plat in currentMap.Platforms)
            {
                int px1 = (int)plat.X1;
                int py1 = (int)plat.Y1;
                int px2 = (int)plat.X2;
                int py2 = (int)plat.Y2;

                // 塗りつぶし
                render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.FillColor, true);
                // 枠線
                render.DrawBox(new Rect<int>(px1, py1, px2, py2), plat.OutlineColor, false);

                // すり抜け床の上端インジケーター線
                if (plat.Type == NativeEngine.PlatformType.SemiSolid)
                {
                    render.DrawBox(new Rect<int>(px1, py1, px2, py1 + 2), 0xFFFFFFFF, true);
                }
            }

            // Player 1 描画 (ブルー)
            int p1X1 = (int)(p1Obj.X - p1Obj.Width / 2.0f);
            int p1Y1 = (int)(p1Obj.Y - p1Obj.Height);
            int p1X2 = (int)(p1Obj.X + p1Obj.Width / 2.0f);
            int p1Y2 = (int)p1Obj.Y;
            uint p1Color = (player1.StateMachine.State == CharacterState.Hitstun) ?
                (((int)(p1Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
                ((player1.StateMachine.State == CharacterState.Grounded) ? player1.MainColor : 0xFF2980B9);

            render.DrawBox(new Rect<int>(p1X1, p1Y1, p1X2, p1Y2), p1Color, true);
            render.DrawBox(new Rect<int>(p1X1, p1Y1, p1X2, p1Y2), player1.OutlineColor, false);

            // Player 2 描画 (パープル)
            int p2X1 = (int)(p2Obj.X - p2Obj.Width / 2.0f);
            int p2Y1 = (int)(p2Obj.Y - p2Obj.Height);
            int p2X2 = (int)(p2Obj.X + p2Obj.Width / 2.0f);
            int p2Y2 = (int)p2Obj.Y;
            uint p2Color = (player2.StateMachine.State == CharacterState.Hitstun) ?
                (((int)(p2Obj.HitstunTimer * 10) % 2 == 0) ? 0xFFE74C3C : 0xFFFFFFFF) :
                ((player2.StateMachine.State == CharacterState.Grounded) ? player2.MainColor : 0xFF8E44AD);

            render.DrawBox(new Rect<int>(p2X1, p2Y1, p2X2, p2Y2), p2Color, true);
            render.DrawBox(new Rect<int>(p2X1, p2Y1, p2X2, p2Y2), player2.OutlineColor, false);

            // HUD および UI 用にカメラ変換を無効化
            render.SetCameraEnabled(false);

            // タイトル＆マップステータスヘッダー
            int lineSpacing = 22;
            render.DrawString(new Point<int>(30, 15), $"=== {currentMap.Name} ===", currentMap.ThemeColor);
            render.DrawString(new Point<int>(30, 37), $"{currentMap.Subtitle}", 0xFFCCCCCC);
            render.DrawString(new Point<int>(30, 59), $"Camera: ({camX:F1}, {camY:F1}) | Zoom: {camScale:F2}x | Dist: {Math.Abs(p1Obj.X - p2Obj.X):F1}px", 0xFF00FF00);

            // Player 1 HUD (左側)
            int p1HudX = 30;
            int p1HudY = 90;
            render.DrawString(new Point<int>(p1HudX, p1HudY), $"[ {player1.Name.ToUpper()} ]", player1.MainColor);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing), $"Damage: {p1Obj.DamagePercent:F1}%", 0xFFE74C3C);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 2), $"Pos: ({p1Obj.X:F1}, {p1Obj.Y:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 3), $"Vel: ({p1Obj.Vx:F1}, {p1Obj.Vy:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p1HudX, p1HudY + lineSpacing * 4), $"State: {player1.StateMachine.State}", 0xFF00FF00);

            // Player 2 HUD (中央)
            int p2HudX = 360;
            int p2HudY = 90;
            render.DrawString(new Point<int>(p2HudX, p2HudY), $"[ {player2.Name.ToUpper()} ]", player2.MainColor);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing), $"Damage: {p2Obj.DamagePercent:F1}%", 0xFFE74C3C);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 2), $"Pos: ({p2Obj.X:F1}, {p2Obj.Y:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 3), $"Vel: ({p2Obj.Vx:F1}, {p2Obj.Vy:F1})", 0xFFDDDDDD);
            render.DrawString(new Point<int>(p2HudX, p2HudY + lineSpacing * 4), $"State: {player2.StateMachine.State}", 0xFF00FF00);

            // 操作方法・調整パネル (右側)
            int helpTextX = Width - 480;
            int helpTextY = 15;
            render.DrawString(new Point<int>(helpTextX, helpTextY), "--- Controls & Tuning ---", 0xFFFFFFFF);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing), "P1: A/D (Move) | W/Space (Jump) | S (FastFall)", player1.MainColor);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 2), "P2: Left/Right (Move) | Up (Jump) | Down (FastFall)", player2.MainColor);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 3), "F: Hit P1 | G: Hit P2 | R: Reset Players", 0xFFFF8888);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 4), "1-4: Switch Stages | Hot Reload Ready (Character.cs)", 0xFFFFFF00);
            render.DrawString(new Point<int>(helpTextX, helpTextY + lineSpacing * 5), "ESC: Exit Game", 0xFF888888);

            // ダブルバッファのフリップ
            NativeEngine.EndFrame(deltaTime);
        }

        Console.WriteLine("ネイティブカメラをクリーンアップ中...");
        NativeEngine.CameraDestroy(camera);

        Console.WriteLine("物理エンジンをクリーンアップ中...");
        NativeEngine.PhysicsDestroy(physics);

        Console.WriteLine("ネイティブエンジンを終了中...");
        NativeEngine.Shutdown();
    }
}
