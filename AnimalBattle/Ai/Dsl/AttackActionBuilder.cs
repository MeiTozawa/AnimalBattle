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
    private readonly List<AttackComponent> _components = components;
    private readonly bool _isOnGrounded = isOnGrounded;
    private readonly float _triggerTime = triggerTime;

    /// <summary>
    ///     突進・移動アクションを追加します。
    /// </summary>
    /// <param name="speedX">水平方向速度（px/s）。</param>
    /// <param name="speedY">垂直方向速度（px/s）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Dash(float speedX, float speedY = 0.0f)
    {
        var index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Movement, index, speedX, speedY, _triggerTime));
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
        var index = _components.Count;
        var kind = _isOnGrounded ? AttackComponentKind.OnGroundedHitbox : AttackComponentKind.Hitbox;
        _components.Add(new AttackComponent(kind, index, offsetX, offsetY, _triggerTime));
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
        var index = _components.Count;
        var kind = _isOnGrounded ? AttackComponentKind.OnGroundedHitbox : AttackComponentKind.Hitbox;
        _components.Add(new AttackComponent(kind, index, x, y, _triggerTime));
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
        var index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Bullet, index, x, y, _triggerTime));
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
        var index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.FlyToTarget, index, peakAltitudeY, flyDuration,
            _triggerTime));
        return this;
    }

    /// <summary>
    ///     高速垂直急降下アクションを追加します。
    /// </summary>
    /// <param name="descentSpeed">垂直降下速度（px/s）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Dive(float descentSpeed = 1200.0f)
    {
        var index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Dive, index, 0.0f, descentSpeed, _triggerTime));
        return this;
    }
}