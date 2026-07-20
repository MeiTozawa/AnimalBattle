using System;
using System.Collections.Generic;
using AnimalBattle.Bindings;

namespace AnimalBattle;

public class PlatformData
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public NativeEngine.PlatformType Type { get; set; }
    public uint FillColor { get; set; }
    public uint OutlineColor { get; set; }
    public int RuntimeId { get; set; } = -1;
}

public class MapData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public uint ThemeColor { get; set; }
    public uint BgColor { get; set; }
    public Point<float> P1Spawn { get; set; }
    public Point<float> P2Spawn { get; set; }
    public Rect<float> BlastZone { get; set; }
    public List<PlatformData> Platforms { get; set; } = [];
}

public class StageManager
{
    private readonly List<MapData> _maps = [];
    private int _currentMapIndex = 0;

    public MapData CurrentMap => _maps[_currentMapIndex];
    public int CurrentMapIndex => _currentMapIndex;
    public int MapCount => _maps.Count;

    public StageManager()
    {
        InitializeMaps();
    }

    private void InitializeMaps()
    {
        // Map 1: Battlefield Citadel (战术城堡)
        _maps.Add(new MapData
        {
            Id = 1,
            Name = "1: Battlefield Citadel (战术城堡)",
            Subtitle = "Classic Tournament Layout | 3 Floating Platforms",
            ThemeColor = 0xFF00ADB5,
            BgColor = 0xFF121B24,
            P1Spawn = new Point<float>(450.0f, 300.0f),
            P2Spawn = new Point<float>(830.0f, 300.0f),
            BlastZone = new Rect<float>(-250.0f, -400.0f, 1530.0f, 900.0f),
            Platforms =
            [
                // Main Solid Stage
                new PlatformData { X1 = 260.0f, Y1 = 500.0f, X2 = 1020.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF1E5128, OutlineColor = 0xFF4E9F3D },
                // Lower Left Semi-Solid
                new PlatformData { X1 = 340.0f, Y1 = 380.0f, X2 = 520.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF00ADB5, OutlineColor = 0xFFEEEEEE },
                // Lower Right Semi-Solid
                new PlatformData { X1 = 760.0f, Y1 = 380.0f, X2 = 940.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF00ADB5, OutlineColor = 0xFFEEEEEE },
                // Top Center Semi-Solid
                new PlatformData { X1 = 550.0f, Y1 = 260.0f, X2 = 730.0f, Y2 = 270.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFFC72C, OutlineColor = 0xFFF8F3D4 }
            ]
        });

        // Map 2: Final Destination (终极决斗场)
        _maps.Add(new MapData
        {
            Id = 2,
            Name = "2: Final Destination (终极决斗场)",
            Subtitle = "Pure 1v1 Neutral Arena | Wide Flat Ground",
            ThemeColor = 0xFFF72585,
            BgColor = 0xFF1A0933,
            P1Spawn = new Point<float>(400.0f, 320.0f),
            P2Spawn = new Point<float>(880.0f, 320.0f),
            BlastZone = new Rect<float>(-300.0f, -450.0f, 1580.0f, 950.0f),
            Platforms =
            [
                // Massive Single Tournament Stage
                new PlatformData { X1 = 150.0f, Y1 = 520.0f, X2 = 1130.0f, Y2 = 580.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF3A1078, OutlineColor = 0xFFF72585 }
            ]
        });

        // Map 3: Twin Floating Islands (双子浮岛)
        _maps.Add(new MapData
        {
            Id = 3,
            Name = "3: Twin Floating Islands (双子浮岛)",
            Subtitle = "Dual Island Split Stage | Central Death Pit & Sky Bridge",
            ThemeColor = 0xFFE67E22,
            BgColor = 0xFF2C1405,
            P1Spawn = new Point<float>(370.0f, 300.0f),
            P2Spawn = new Point<float>(910.0f, 300.0f),
            BlastZone = new Rect<float>(-250.0f, -400.0f, 1530.0f, 880.0f),
            Platforms =
            [
                // Left Solid Island
                new PlatformData { X1 = 180.0f, Y1 = 500.0f, X2 = 560.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFFD35400, OutlineColor = 0xFFE67E22 },
                // Right Solid Island
                new PlatformData { X1 = 720.0f, Y1 = 500.0f, X2 = 1100.0f, Y2 = 560.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFFD35400, OutlineColor = 0xFFE67E22 },
                // Central High Sky Bridge (Semi-Solid)
                new PlatformData { X1 = 520.0f, Y1 = 340.0f, X2 = 760.0f, Y2 = 350.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFF2ECC71, OutlineColor = 0xFF27AE60 }
            ]
        });

        // Map 4: Cyber Neon Arena (赛博数码竞技场)
        _maps.Add(new MapData
        {
            Id = 4,
            Name = "4: Cyber Neon Arena (赛博数码竞技场)",
            Subtitle = "High-Tech Cyber Grid | 4 Multi-Tier Floating Platforms",
            ThemeColor = 0xFF00F5FF,
            BgColor = 0xFF080E18,
            P1Spawn = new Point<float>(420.0f, 300.0f),
            P2Spawn = new Point<float>(860.0f, 300.0f),
            BlastZone = new Rect<float>(-300.0f, -450.0f, 1580.0f, 920.0f),
            Platforms =
            [
                // Cyber Main Stage
                new PlatformData { X1 = 280.0f, Y1 = 520.0f, X2 = 1000.0f, Y2 = 570.0f, Type = NativeEngine.PlatformType.Solid, FillColor = 0xFF0F4C81, OutlineColor = 0xFF00F5FF },
                // Left Lower Wing
                new PlatformData { X1 = 200.0f, Y1 = 420.0f, X2 = 380.0f, Y2 = 430.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFE056FD, OutlineColor = 0xFF7D5FFF },
                // Right Lower Wing
                new PlatformData { X1 = 900.0f, Y1 = 420.0f, X2 = 1080.0f, Y2 = 430.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFE056FD, OutlineColor = 0xFF7D5FFF },
                // Center Mid Platform
                new PlatformData { X1 = 480.0f, Y1 = 380.0f, X2 = 800.0f, Y2 = 390.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFEE715, OutlineColor = 0xFFFFDD59 },
                // Center High Platform
                new PlatformData { X1 = 580.0f, Y1 = 240.0f, X2 = 700.0f, Y2 = 250.0f, Type = NativeEngine.PlatformType.SemiSolid, FillColor = 0xFFFF3838, OutlineColor = 0xFFFF4D4D }
            ]
        });
    }

    public void LoadMap(IntPtr physics, int mapIndex)
    {
        if (mapIndex < 0 || mapIndex >= _maps.Count) return;

        // 1. Remove existing physics platforms
        MapData oldMap = CurrentMap;
        foreach (var plat in oldMap.Platforms)
        {
            if (plat.RuntimeId != -1)
            {
                NativeEngine.PhysicsRemovePlatform(physics, plat.RuntimeId);
                plat.RuntimeId = -1;
            }
        }

        // 2. Set current map
        _currentMapIndex = mapIndex;
        MapData newMap = CurrentMap;

        // 3. Add new platforms to native C++ physics engine
        foreach (var plat in newMap.Platforms)
        {
            plat.RuntimeId = NativeEngine.PhysicsAddPlatform(
                physics, plat.X1, plat.Y1, plat.X2, plat.Y2, (int)plat.Type);
        }

        Console.WriteLine($"[StageManager] Switched to Map {newMap.Id}: {newMap.Name}");
    }
}
