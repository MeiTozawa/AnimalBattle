using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnimalBattle.Core;

namespace AnimalBattle.Stages;
/// <summary>
/// 4 つの対戦マップデータおよび足場（Platform）の幾何構成を管理するマネージャークラス。
/// </summary>
public class StageManager
{
    private readonly List<MapData> _maps = [];
    private int _currentMapIndex;

    /// <summary>
    /// 登録されている全マップの読み取り専用リストを取得します。
    /// </summary>
    public ReadOnlyCollection<MapData> Maps => _maps.AsReadOnly();

    /// <summary>
    /// 現在選択中のアクティブマップデータを取得します。
    /// </summary>
    public MapData CurrentMap => _maps[_currentMapIndex];

    /// <summary>
    /// 現在選択中のマップインデックス (0～3) を取得します。
    /// </summary>
    public int CurrentMapIndex => _currentMapIndex;

    /// <summary>
    /// <see cref="StageManager"/> の新しいインスタンスを初期化し、4 つの対戦マップを生成します。
    /// </summary>
    public StageManager()
    {
        InitializeMaps();
        _currentMapIndex = 0;
    }

    private void InitializeMaps()
    {
        _maps.Clear();

        MapData map1 = new MapData(
            id: 1,
            name: "Battlefield Citadel",
            subtitle: "競技トーナメント標準レイアウト | 3つの浮遊足場",
            themeColor: 0xFF00ADB5,
            bgColor: 0xFF121B24,
            blastZone: new Rect<float>(-300.0f, -300.0f, 1580.0f, 1020.0f));

        map1.AddPlatform(new PlatformData(640.0f, 560.0f, 760.0f, 30.0f, PlatformType.Solid, 0xFF1E5128, 0xFF4E9F3D));
        map1.AddPlatform(new PlatformData(440.0f, 440.0f, 180.0f, 16.0f, PlatformType.SemiSolid, 0xFF194350, 0xFFFF8882));
        map1.AddPlatform(new PlatformData(840.0f, 440.0f, 180.0f, 16.0f, PlatformType.SemiSolid, 0xFF194350, 0xFFFF8882));
        map1.AddPlatform(new PlatformData(640.0f, 320.0f, 180.0f, 16.0f, PlatformType.SemiSolid, 0xFF194350, 0xFFFF8882));

        map1.SetSpawnPositions(
            new Point<float>(380.0f, 320.0f),
            new Point<float>(540.0f, 450.0f),
            new Point<float>(740.0f, 450.0f),
            new Point<float>(900.0f, 320.0f));
        _maps.Add(map1);

        MapData map2 = new MapData(
            id: 2,
            name: "Final Sanctuary",
            subtitle: "ギミックなしの真剣勝負 | 1つの巨大な主足場",
            themeColor: 0xFFE94560,
            bgColor: 0xFF1A1A2E,
            blastZone: new Rect<float>(-350.0f, -350.0f, 1630.0f, 1050.0f));

        map2.AddPlatform(new PlatformData(640.0f, 550.0f, 840.0f, 36.0f, PlatformType.Solid, 0xFF0F3460, 0xFFE94560));

        map2.SetSpawnPositions(
            new Point<float>(360.0f, 420.0f),
            new Point<float>(520.0f, 420.0f),
            new Point<float>(760.0f, 420.0f),
            new Point<float>(920.0f, 420.0f));
        _maps.Add(map2);

        MapData map3 = new MapData(
            id: 3,
            name: "Twin Towers",
            subtitle: "中央クレバスと2連の独立タワー | 垂直立体機動",
            themeColor: 0xFFF39C12,
            bgColor: 0xFF2C3E50,
            blastZone: new Rect<float>(-300.0f, -300.0f, 1580.0f, 1020.0f));

        map3.AddPlatform(new PlatformData(420.0f, 540.0f, 320.0f, 28.0f, PlatformType.Solid, 0xFF34495E, 0xFFF1C40F));
        map3.AddPlatform(new PlatformData(860.0f, 540.0f, 320.0f, 28.0f, PlatformType.Solid, 0xFF34495E, 0xFFF1C40F));
        map3.AddPlatform(new PlatformData(420.0f, 400.0f, 160.0f, 16.0f, PlatformType.SemiSolid, 0xFF2C3E50, 0xFFE67E22));
        map3.AddPlatform(new PlatformData(860.0f, 400.0f, 160.0f, 16.0f, PlatformType.SemiSolid, 0xFF2C3E50, 0xFFE67E22));
        map3.AddPlatform(new PlatformData(640.0f, 280.0f, 200.0f, 16.0f, PlatformType.SemiSolid, 0xFF2C3E50, 0xFFE74C3C));

        map3.SetSpawnPositions(
            new Point<float>(360.0f, 320.0f),
            new Point<float>(480.0f, 450.0f),
            new Point<float>(800.0f, 450.0f),
            new Point<float>(920.0f, 320.0f));
        _maps.Add(map3);

        MapData map4 = new MapData(
            id: 4,
            name: "Floating Islands",
            subtitle: "落下リスクの高い3連浮遊島 | 高度な復帰戦",
            themeColor: 0xFF9B59B6,
            bgColor: 0xFF1B263B,
            blastZone: new Rect<float>(-300.0f, -300.0f, 1580.0f, 1020.0f));

        map4.AddPlatform(new PlatformData(640.0f, 560.0f, 360.0f, 26.0f, PlatformType.Solid, 0xFF415A77, 0xFF8E44AD));
        map4.AddPlatform(new PlatformData(300.0f, 480.0f, 220.0f, 22.0f, PlatformType.SemiSolid, 0xFF415A77, 0xFF3498DB));
        map4.AddPlatform(new PlatformData(980.0f, 480.0f, 220.0f, 22.0f, PlatformType.SemiSolid, 0xFF415A77, 0xFF3498DB));
        map4.AddPlatform(new PlatformData(640.0f, 360.0f, 180.0f, 16.0f, PlatformType.SemiSolid, 0xFF415A77, 0xFF1ABC9C));

        map4.SetSpawnPositions(
            new Point<float>(300.0f, 380.0f),
            new Point<float>(560.0f, 450.0f),
            new Point<float>(720.0f, 450.0f),
            new Point<float>(980.0f, 380.0f));
        _maps.Add(map4);
    }

    /// <summary>
    /// 指定されたマップインデックス (0～3) にアクティブマップを切り替え、物理エンジンへ足場を再登録します。
    /// </summary>
    /// <param name="physics">物理エンジンインスタンス</param>
    /// <param name="index">マップインデックス (0～3)</param>
    public void SwitchMap(PhysicsEngine physics, int index)
    {
        if (index < 0 || index >= _maps.Count)
        {
            return;
        }

        MapData oldMap = _maps[_currentMapIndex];
        foreach (PlatformData plat in oldMap.Platforms)
        {
            if (plat.PhysicsPlatformId != -1)
            {
                physics.RemovePlatform(plat.PhysicsPlatformId);
                plat.PhysicsPlatformId = -1;
            }
        }

        _currentMapIndex = index;
        MapData newMap = _maps[_currentMapIndex];
        foreach (PlatformData plat in newMap.Platforms)
        {
            plat.PhysicsPlatformId = physics.AddPlatform(
                plat.BoundingBox.Left,
                plat.BoundingBox.Top,
                plat.BoundingBox.Right,
                plat.BoundingBox.Bottom,
                (int)plat.Type);
        }
    }
}
