using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// Native C++ 物理エンジン (AnimalBattleCore.dll) を P/Invoke 経由で操作し、
/// 重力、運動積分、地形衝突判定、およびノックバック計算を管理するクラス。
/// </summary>
public unsafe class PhysicsEngine : IDisposable
{
    private IntPtr _nativeEngine;
    private bool _isDisposed;

    /// <summary>
    /// 物理エンジンの設定コンテナを取得または設定します。
    /// </summary>
    public ConfigBase<PhysicsConfig> ConfigHolder { get; set; }

    /// <summary>
    /// 物理パラメータ設定を取得します。
    /// </summary>
    public PhysicsConfig Config => ConfigHolder.Config;

    /// <summary>
    /// 指定された TOML ファイルパスから物理パラメータを読み込み、<see cref="PhysicsEngine"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="configPath">読み込む TOML ファイルパス</param>
    public PhysicsEngine(string configPath = "physics.toml")
    {
        _nativeEngine = NativeBindings.Physics_Create();
        Result<ConfigBase<PhysicsConfig>> configResult = PhysicsConfig.LoadFromFile(configPath);
        ConfigHolder = configResult.IsOk ? configResult.Value! : new ConfigBase<PhysicsConfig>();
        ConfigHolder.OnConfigReloaded += ApplyConfigToNative;

        ApplyConfigToNative();
    }

    /// <summary>
    /// デフォルト設定オブジェクトインスタンスで <see cref="PhysicsEngine"/> を初期化します。
    /// </summary>
    /// <param name="config">物理設定オブジェクト</param>
    public PhysicsEngine(PhysicsConfig config)
    {
        _nativeEngine = NativeBindings.Physics_Create();
        ConfigHolder = new ConfigBase<PhysicsConfig>(config);
        ConfigHolder.OnConfigReloaded += ApplyConfigToNative;

        ApplyConfigToNative();
    }

    /// <summary>
    /// 物理設定パラメータを Native C++ 物理エンジンへ同期・反映します。
    /// </summary>
    public void ApplyConfigToNative()
    {
        if (_nativeEngine == IntPtr.Zero)
        {
            return;
        }

        NativeBindings.Physics_SetKnockbackParams(
            _nativeEngine,
            Config.KnockbackWeightOffset,
            Config.KnockbackDamageScale1,
            Config.KnockbackDamageScale2,
            Config.KnockbackScaleMultiplier,
            Config.KnockbackBaseMultiplier);

        NativeBindings.Physics_SetHitstunParams(
            _nativeEngine,
            Config.HitstunBaseSeconds,
            Config.HitstunKnockbackScale,
            Config.HitstunMaxSeconds);
    }

    /// <summary>
    /// 動的物理オブジェクト（プレイヤー等）をエンジンに登録します。
    /// </summary>
    /// <param name="x">初期 X 座標</param>
    /// <param name="y">初期 Y 座標</param>
    /// <param name="width">バウンディング幅</param>
    /// <param name="height">バウンディング高さ</param>
    /// <param name="weight">オブジェクトの体重</param>
    /// <returns>割り当てられたオブジェクト ID</returns>
    public int AddObject(float x, float y, float width, float height, float weight)
    {
        return NativeBindings.Physics_AddObject(_nativeEngine, x, y, width, height, weight);
    }

    /// <summary>
    /// 指定 ID の動的物理オブジェクトを削除・登録解除します。
    /// </summary>
    /// <param name="id">オブジェクト ID</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool RemoveObject(int id)
    {
        return NativeBindings.Physics_RemoveObject(_nativeEngine, id);
    }

    /// <summary>
    /// 指定 ID の物理オブジェクトの状態（座標、速度、接地状態等）を高速に取得します。
    /// </summary>
    /// <param name="id">オブジェクト ID</param>
    /// <param name="outState">取得先構造体</param>
    /// <returns>取得に成功した場合は true</returns>
    public bool GetObjectState(int id, out NativePhysicsObject outState)
    {
        fixed (NativePhysicsObject* ptr = &outState)
        {
            return NativeBindings.Physics_GetObjectState(_nativeEngine, id, ptr);
        }
    }

    /// <summary>
    /// 指定 ID の物理オブジェクトの状態を高速に更新・上書きします。
    /// </summary>
    /// <param name="id">オブジェクト ID</param>
    /// <param name="inState">設定する状態構造体</param>
    /// <returns>更新に成功した場合は true</returns>
    public bool SetObjectState(int id, in NativePhysicsObject inState)
    {
        fixed (NativePhysicsObject* ptr = &inState)
        {
            return NativeBindings.Physics_SetObjectState(_nativeEngine, id, ptr);
        }
    }

    /// <summary>
    /// 静的足場（プラットフォーム）をエンジンに登録します。
    /// </summary>
    /// <param name="x1">左端 X 座標</param>
    /// <param name="y1">上端 Y 座標</param>
    /// <param name="x2">右端 X 座標</param>
    /// <param name="y2">下端 Y 座標</param>
    /// <param name="type">足場種別 (Solid / SemiSolid)</param>
    /// <returns>割り当てられたプラットフォーム ID</returns>
    public int AddPlatform(float x1, float y1, float x2, float y2, int type)
    {
        return NativeBindings.Physics_AddPlatform(_nativeEngine, x1, y1, x2, y2, type);
    }

    /// <summary>
    /// 指定 ID のプラットフォームを削除・登録解除します。
    /// </summary>
    /// <param name="id">プラットフォーム ID</param>
    /// <returns>削除に成功した場合は true</returns>
    public bool RemovePlatform(int id)
    {
        return NativeBindings.Physics_RemovePlatform(_nativeEngine, id);
    }

    /// <summary>
    /// 対象オブジェクトにノックバック衝撃力を適用します。
    /// </summary>
    /// <param name="id">オブジェクト ID</param>
    /// <param name="damage">付与ダメージ</param>
    /// <param name="angleRad">ノックバック射出角度 (ラジアン)</param>
    /// <param name="baseKnockback">基本ノックバック値</param>
    /// <param name="scaling">スケーリング係数</param>
    public void ApplyKnockback(int id, float damage, float angleRad, float baseKnockback, float scaling)
    {
        NativeBindings.Physics_ApplyKnockback(_nativeEngine, id, damage, angleRad, baseKnockback, scaling);
    }

    /// <summary>
    /// 物理シミュレーションをデルタタイム分ステップ進行させます。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Step(float deltaTime)
    {
        NativeBindings.Physics_Step(_nativeEngine, deltaTime);
    }

    /// <summary>
    /// ネイティブ物理エンジンリソースおよび設定コンテナを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_nativeEngine != IntPtr.Zero)
            {
                NativeBindings.Physics_Destroy(_nativeEngine);
                _nativeEngine = IntPtr.Zero;
            }
            ConfigHolder.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
