using System.Collections.Generic;
using AnimalBattle.Ai.Attack;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// 攻撃の特定トリガー時刻（TriggerTime）におけるアクションを定義するビルダーです。
/// </summary>
public sealed class AttackActionBuilder
{
    private readonly float _triggerTime;
    private readonly List<AttackComponent> _components;

    /// <summary>
    /// 指定されたトリガー時刻でアクションビルダーを初期化します。
    /// </summary>
    /// <param name="triggerTime">発動遅延時間（秒）。</param>
    /// <param name="components">追加対象のコンポーネントリスト。</param>
    public AttackActionBuilder(float triggerTime, List<AttackComponent> components)
    {
        _triggerTime = triggerTime;
        _components = components;
    }

    /// <summary>
    /// 突進・移動アクションを追加します。
    /// </summary>
    /// <param name="speedX">水平方向速度（px/s）。</param>
    /// <param name="speedY">垂直方向速度（px/s）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Dash(float speedX, float speedY = 0.0f)
    {
        int index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Movement, index, speedX, speedY, _triggerTime));
        return this;
    }

    /// <summary>
    /// 近接斬撃判定アクションを追加します。
    /// </summary>
    /// <param name="offsetX">Boss 前方へのオフセット距離（px）。</param>
    /// <param name="offsetY">垂直オフセット（px）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Slash(float offsetX = 80.0f, float offsetY = -10.0f)
    {
        int index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Hitbox, index, offsetX, offsetY, _triggerTime));
        return this;
    }

    /// <summary>
    /// 汎用 Hitbox 生成アクションを追加します。
    /// </summary>
    /// <param name="x">X 座標オフセット。</param>
    /// <param name="y">Y 座標オフセット。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Hitbox(float x = 0.0f, float y = 0.0f)
    {
        int index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Hitbox, index, x, y, _triggerTime));
        return this;
    }

    /// <summary>
    /// 弾丸発射アクションを追加します。
    /// </summary>
    /// <param name="x">X 座標オフセット。</param>
    /// <param name="y">Y 座標オフセット。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public AttackActionBuilder Bullet(float x = 0.0f, float y = 0.0f)
    {
        int index = _components.Count;
        _components.Add(new AttackComponent(AttackComponentKind.Bullet, index, x, y, _triggerTime));
        return this;
    }
}
