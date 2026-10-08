using AnimalBattle.Ai.Attack;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     攻撃の特定トリガー時刻（TriggerTime）におけるアクションを定義するビルダーです。
/// </summary>
/// <param name="triggerTime">発動遅延時間（秒）。</param>
/// <param name="components">追加対象のコンポーネントリスト。</param>
/// <param name="isOnGrounded">接地トリガーコンポーネントかどうか。</param>
public sealed class AttackActionBuilder(
    float triggerTime,
    List<AttackComponent> components,
    bool isOnGrounded = false) {
    /// <summary>
    ///     突進・移動アクションを追加します。
    /// </summary>
    /// <param name="speedX">水平方向速度（px/s）。</param>
    /// <param name="speedY">垂直方向速度（px/s）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Dash(float speedX, float speedY = 0.0f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.Movement, index, speedX, speedY, triggerTime));
        return this;
    }

    /// <summary>
    ///     近接斬撃判定アクションを追加します。
    /// </summary>
    /// <param name="offsetX">Boss 前方へのオフセット距離（px）。</param>
    /// <param name="offsetY">垂直オフセット（px）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Slash(float offsetX = 80.0f, float offsetY = -10.0f)
    {
        var index = components.Count;
        var kind = isOnGrounded ? AttackComponentKind.OnGroundedHitbox : AttackComponentKind.Hitbox;
        components.Add(new AttackComponent(kind, index, offsetX, offsetY, triggerTime));
        return this;
    }

    /// <summary>
    ///     汎用 Hitbox 生成アクションを追加します。
    /// </summary>
    /// <param name="x">X 座標オフセット。</param>
    /// <param name="y">Y 座標オフセット。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Hitbox(float x = 0.0f, float y = 0.0f)
    {
        var index = components.Count;
        var kind = isOnGrounded ? AttackComponentKind.OnGroundedHitbox : AttackComponentKind.Hitbox;
        components.Add(new AttackComponent(kind, index, x, y, triggerTime));
        return this;
    }

    /// <summary>
    ///     弾丸発射アクションを追加します。
    /// </summary>
    /// <param name="x">X 座標オフセット。</param>
    /// <param name="y">Y 座標オフセット。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Bullet(float x = 0.0f, float y = 0.0f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.Bullet, index, x, y, 0.0f, triggerTime));
        return this;
    }

    /// <summary>
    ///     弾幕一斉発射または拡散弾アクションを追加します。
    /// </summary>
    /// <param name="bulletSpeed">弾丸初速（px/s）。</param>
    /// <param name="bulletCount">発射弾数。</param>
    /// <param name="spreadAngle">拡散角度（ラジアン）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Barrage(float bulletSpeed, float bulletCount = 1.0f, float spreadAngle = 0.0f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.Bullet, index, bulletSpeed, bulletCount, spreadAngle,
            triggerTime));
        return this;
    }

    /// <summary>
    ///     プレイヤー頭上へのイージング飛翔アクションを追加します。
    /// </summary>
    /// <param name="peakAltitudeY">最高点の目標高度 Y 座標。</param>
    /// <param name="flyDuration">上昇飛翔の所要時間（秒）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder FlyToTarget(float peakAltitudeY = 130.0f, float flyDuration = 0.45f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.EasingMovement, index, peakAltitudeY, flyDuration, 0.0f,
            triggerTime));
        return this;
    }

    /// <summary>
    ///     高速垂直急降下アクションを追加します。
    /// </summary>
    /// <param name="descentSpeed">垂直降下速度（px/s）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Dive(float descentSpeed = 1200.0f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.VelocityDive, index, 0.0f, descentSpeed, 0.0f,
            triggerTime));
        return this;
    }

    /// <summary>
    ///     指定された効果音 Cue 再生アクションを追加します。
    /// </summary>
    /// <param name="soundId">音響リソースID。</param>
    /// <param name="volume">音量倍率（0.0f - 1.0f）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Sound(int soundId, float volume = 1.0f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.AudioCue, index, soundId, volume, 0.0f, triggerTime));
        return this;
    }

    /// <summary>
    ///     画面振動（Camera Shake）アクションを追加します。
    /// </summary>
    /// <param name="intensity">振動振幅強度（px）。</param>
    /// <param name="duration">振動継続時間（秒）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder CameraShake(float intensity = 5.0f, float duration = 0.2f)
    {
        var index = components.Count;
        components.Add(new AttackComponent(AttackComponentKind.ScreenShake, index, intensity, duration, 0.0f,
            triggerTime));
        return this;
    }
}