using System.Runtime.InteropServices;
using AnimalBattle.Core;

namespace AnimalBattle.Compute;

/// <summary>
/// C++ 原生拡張 (AnimalBattleExtension.dll) とバイナリ互換なイージング一括計算バッチ要素構造体 (12 bytes)。
/// </summary>
/// <remarks>
/// イージング計算バッチ要素を初期化します。
/// </remarks>
/// <param name="easeType">イージング関数種別。</param>
/// <param name="progress">進捗割合 (0.0f～1.0f)。</param>
[StructLayout(LayoutKind.Sequential)]
public struct EaseBatchItem(EaseType easeType, float progress)
{
    /// <summary>評価対象のイージング関数種別。</summary>
    public EaseType EaseType = easeType;

    /// <summary>正規化進捗割合 (0.0f～1.0f)。</summary>
    public float Progress = progress;

    /// <summary>C++ 原生拡張によって計算・出力されるイージング補間結果値。</summary>
    public float Result = 0.0f;
}
