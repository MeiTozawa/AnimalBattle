using System;
using System.Collections.Generic;
using AnimalBattle.Bindings;

namespace AnimalBattle.Stages;

/// <summary>
/// ステージ（マップ）の切り替え・物理エンジン登録および管理を行うマネージャークラス。
/// </summary>
public class StageManager
{
    private readonly List<MapData> _maps = [];
    private int _currentMapIndex = 0;

    /// <summary>
    /// 現在選択されているマップデータを取得します。
    /// </summary>
    public MapData CurrentMap => _maps[_currentMapIndex];

    /// <summary>
    /// 現在選択されているマップのインデックスを取得します。
    /// </summary>
    public int CurrentMapIndex => _currentMapIndex;

    /// <summary>
    /// 登録されている総マップ数を取得します。
    /// </summary>
    public int MapCount => _maps.Count;

    /// <summary>
    /// マップデータを初期化して <see cref="StageManager"/> の新しいインスタンスを生成します。
    /// </summary>
    public StageManager()
    {
        InitializeMaps();
    }

    private void InitializeMaps()
    {
        // マップ 1: 戦術城堡 (Battlefield Citadel)
        _maps.Add(new MapData
        {
            Id = 1,
            Name = "1: Battlefield Citadel (戦術城堡)",
            Subtitle = "Classic Tournament Layout | 3 Floating Platforms",
            ThemeColor = 0xFF00ADB5,
            BgColor = 0xFF121B24,
            P1Spawn = new Point<float>(450.0f, 300.0f),
            P2Spawn = new Point<float>(830.0f, 300.0f),
            BlastZone = new Rect<float>(-250.0f, -400.0f, 1530.0f, 900.0f),
            Platforms =
            [
                // メイン実体床
                new PlatformData { X1 = 260.0f, Y1 = 500.0f, X2 = 1020.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF1E5128, OutlineColor = 0xFF4E9F3D },
                // 左下すり抜け床
                new PlatformData { X1 = 340.0f, Y1 = 380.0f, X2 = 520.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF00ADB5, OutlineColor = 0xFFEEEEEE },
                // 右下すり抜け床
                new PlatformData { X1 = 760.0f, Y1 = 380.0f, X2 = 940.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF00ADB5, OutlineColor = 0xFFEEEEEE },
                // 中央上すり抜け床
                new PlatformData { X1 = 550.0f, Y1 = 260.0f, X2 = 730.0f, Y2 = 270.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFFC72C, OutlineColor = 0xFFF8F3D4 }
            ]
        });

        // マップ 2: 終極決闘場 (Final Destination)
        _maps.Add(new MapData
        {
            Id = 2,
            Name = "2: Final Destination (終極決闘場)",
            Subtitle = "Pure 1v1 Neutral Arena | Wide Flat Ground",
            ThemeColor = 0xFFF72585,
            BgColor = 0xFF1A0933,
            P1Spawn = new Point<float>(400.0f, 320.0f),
            P2Spawn = new Point<float>(880.0f, 320.0f),
            BlastZone = new Rect<float>(-300.0f, -450.0f, 1580.0f, 950.0f),
            Platforms =
            [
                // 大型平坦床
                new PlatformData { X1 = 150.0f, Y1 = 520.0f, X2 = 1130.0f, Y2 = 580.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF3A1078, OutlineColor = 0xFFF72585 }
            ]
        });

        // マップ 3: 雙子浮島 (Twin Floating Islands)
        _maps.Add(new MapData
        {
            Id = 3,
            Name = "3: Twin Floating Islands (雙子浮島)",
            Subtitle = "Dual Island Split Stage | Central Death Pit & Sky Bridge",
            ThemeColor = 0xFFE67E22,
            BgColor = 0xFF2C1405,
            P1Spawn = new Point<float>(370.0f, 300.0f),
            P2Spawn = new Point<float>(910.0f, 300.0f),
            BlastZone = new Rect<float>(-250.0f, -400.0f, 1530.0f, 880.0f),
            Platforms =
            [
                // 左浮島
                new PlatformData { X1 = 180.0f, Y1 = 500.0f, X2 = 560.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFFD35400, OutlineColor = 0xFFE67E22 },
                // 右浮島
                new PlatformData { X1 = 720.0f, Y1 = 500.0f, X2 = 1100.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFFD35400, OutlineColor = 0xFFE67E22 },
                // 中央高所空中ブリッジ (すり抜け)
                new PlatformData { X1 = 520.0f, Y1 = 340.0f, X2 = 760.0f, Y2 = 350.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF2ECC71, OutlineColor = 0xFF27AE60 }
            ]
        });

        // マップ 4: 賽博數碼競技場 (Cyber Neon Arena)
        _maps.Add(new MapData
        {
            Id = 4,
            Name = "4: Cyber Neon Arena (賽博數碼競技場)",
            Subtitle = "High-Tech Cyber Grid | 4 Multi-Tier Floating Platforms",
            ThemeColor = 0xFF00F5FF,
            BgColor = 0xFF080E18,
            P1Spawn = new Point<float>(420.0f, 300.0f),
            P2Spawn = new Point<float>(860.0f, 300.0f),
            BlastZone = new Rect<float>(-300.0f, -450.0f, 1580.0f, 920.0f),
            Platforms =
            [
                // メインサイバー床
                new PlatformData { X1 = 280.0f, Y1 = 520.0f, X2 = 1000.0f, Y2 = 570.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF0F4C81, OutlineColor = 0xFF00F5FF },
                // 左下サイド床
                new PlatformData { X1 = 200.0f, Y1 = 420.0f, X2 = 380.0f, Y2 = 430.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFE056FD, OutlineColor = 0xFF7D5FFF },
                // 右下サイド床
                new PlatformData { X1 = 900.0f, Y1 = 420.0f, X2 = 1080.0f, Y2 = 430.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFE056FD, OutlineColor = 0xFF7D5FFF },
                // 中央中段床
                new PlatformData { X1 = 480.0f, Y1 = 380.0f, X2 = 800.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFEE715, OutlineColor = 0xFFFFDD59 },
                // 中央最上段床
                new PlatformData { X1 = 580.0f, Y1 = 240.0f, X2 = 700.0f, Y2 = 250.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFF3838, OutlineColor = 0xFFFF4D4D }
            ]
        });
    }

    /// <summary>
    /// 指定されたインデックスのマップを読み込み、Native C++ 物理エンジンに登録します。
    /// </summary>
    /// <param name="physics">Native C++ 物理エンジンのポインタ</param>
    /// <param name="mapIndex">読み込むマップのインデックス</param>
    public void LoadMap(IntPtr physics, int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= _maps.Count)
        {
            return;
        }

        // 1. 既存の物理エンジン上の足場を削除
        MapData oldMap = CurrentMap;
        foreach (var plat in oldMap.Platforms)
        {
            if (plat.RuntimeId != -1)
            {
                NativeEngine.PhysicsRemovePlatform(physics, plat.RuntimeId);
                plat.RuntimeId = -1;
            }
        }

        // 2. カレントマップの更新
        _currentMapIndex = mapIndex;
        MapData newMap = CurrentMap;

        // 3. 新しい足場を Native C++ 物理エンジンへ登録
        foreach (var plat in newMap.Platforms)
        {
            plat.RuntimeId = NativeEngine.PhysicsAddPlatform(
                physics, plat.X1, plat.Y1, plat.X2, plat.Y2, (int)plat.Type);
        }

        Console.WriteLine($"[ステージマネージャー] マップ {newMap.Id} に切り替えました: {newMap.Name}");
    }
}
