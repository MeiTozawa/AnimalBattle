using System;
using System.Collections.Generic;

namespace AnimalBattle.Inputs;

public partial class InputManager
{
    /// <summary>
    /// 現在最上位のアクティブな入力コンテキストを取得します。
    /// </summary>
    public InputContext CurrentContext => _contextStack.Count > 0 ? _contextStack[^1] : InputContext.InGame;

    /// <summary>
    /// 新しい入力コンテキストをスタックの最上位にプッシュします。
    /// </summary>
    public void PushContext(InputContext context)
    {
        if (_contextStack.Count == 0 || _contextStack[^1] != context)
        {
            _contextStack.Add(context);
        }
    }

    /// <summary>
    /// 最上位の入力コンテキストをポップし、1つ前のコンテキストに戻します。
    /// </summary>
    public void PopContext()
    {
        if (_contextStack.Count > 1)
        {
            _contextStack.RemoveAt(_contextStack.Count - 1);
        }
    }

    /// <summary>
    /// コンテキストスタックをクリアし、指定されたコンテキストを単一でセットします。
    /// </summary>
    public void ClearAndSetContext(InputContext context)
    {
        _contextStack.Clear();
        _contextStack.Add(context);
    }
}
