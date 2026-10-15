using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     Boss 1 (Ancient Cat Overlord) 個別フェーズの FSM 状態機械設定クラス。
/// </summary>
public sealed class Boss1PhaseFsmConfig {
    /// <summary>このフェーズが有効となる HP 割合上限</summary>
    public float HpThreshold { get; set; }

    /// <summary>攻撃前の巡航遊弋待機時間（秒）</summary>
    public float HoverDuration { get; set; }

    /// <summary>このフェーズでの巡回移動時間（秒）</summary>
    public float PatrolDuration { get; set; }

    /// <summary>浮遊基準 Y 座標</summary>
    public float HoverBaseY { get; set; }

    /// <summary>水平浮遊速度</summary>
    public float HoverSpeedX { get; set; }

    /// <summary>水平浮遊振幅</summary>
    public float HoverAmpX { get; set; }

    /// <summary>垂直浮遊速度</summary>
    public float HoverSpeedY { get; set; }

    /// <summary>垂直浮遊振幅</summary>
    public float HoverAmpY { get; set; }

    /// <summary>浮動追従の補間速度（Lerp Speed）</summary>
    public float HoverLerpSpeed { get; set; }

    /// <summary>プレイヤー X 座標への接近追従重み（0.0 ~ 1.0）</summary>
    public float PlayerPursuitWeight { get; set; }

    /// <summary>突進予兆時の機体振動振幅（ピクセル）</summary>
    public float TelegraphShakeIntensity { get; set; }

    /// <summary>突進着地硬直後の高空帰還速度</summary>
    public float RecoverFlybackSpeed { get; set; }

    /// <summary>着地硬直から帰還を開始する時間比率（0.0 ~ 1.0）</summary>
    public float RecoverDelayRatio { get; set; }

    /// <summary>フェーズ突入時の無敵・清屏演出時間（秒）</summary>
    public float TransitionDuration { get; set; }

    /// <summary>フェーズ突入時の高空帰還・浮遊移動速度</summary>
    public float TransitionFloatSpeed { get; set; }

    /// <summary>このフェーズで実行される技能名リスト</summary>
    public List<string> Skills { get; set; } = [];

    /// <summary>このフェーズで評価される技能意思決定ルールリスト</summary>
    public List<AttackRuleConfig> Rules { get; set; } = [];
}

/// <summary>
///     Config/boss1.toml から読み込まれる Boss 1 の基本パラメータおよび各フェーズの FSM 設定クラス。
/// </summary>
public sealed class Boss1Config : BaseBossConfig {
    /// <summary>被弾時ホワイトフラッシュ持続時間（秒）</summary>
    public float HitFlashDuration { get; set; }

    /// <summary>急降下突進技能識別名</summary>
    public string DiveSkill { get; set; } = string.Empty;

    /// <summary>Phase 1 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase1 { get; set; } = new();

    /// <summary>Phase 2 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase2 { get; set; } = new();

    /// <summary>Phase 3 状態機設定</summary>
    public Boss1PhaseFsmConfig Phase3 { get; set; } = new();

    /// <summary>Phase 遷移（フェーズ移行）能力設定</summary>
    public PhaseTransitionAbilityConfig PhaseTransition { get; set; } = new();

    /// <summary>
    ///     指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{Boss1Config}" /> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr<ConfigBase<Boss1Config>> LoadFromFile(string path = ResourcePath.Config.Boss1)
    {
        return ConfigBase<Boss1Config>.LoadFromFile(path);
    }
}