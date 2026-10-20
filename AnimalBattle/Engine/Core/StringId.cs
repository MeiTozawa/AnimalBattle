using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
///     高速文字列識別子を表す 4 バイトの純粋な非托管（Unmanaged）値型構造体。
///     FNV-1a による大文字小文字を区別しないハッシュ計算、O(1) の単一命令比較、
///     およびデバッグ用の文字列逆引きプールを提供します。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct StringId : IEquatable<StringId>, IComparable<StringId> {
    private static readonly ConcurrentDictionary<uint, string> _stringPool = new();

    /// <summary>空の識別子です。</summary>
    public static readonly StringId Empty = new(0);

    /// <summary>識別子が空（未設定またはハッシュ値 0）であるかどうかを取得します。</summary>
    public readonly bool IsEmpty => Value == 0;

    /// <summary>ハッシュ値（4 バイト）です。</summary>
    public readonly uint Value;

    /// <summary>
    ///     文字列から <see cref="StringId" /> を初期化します。
    /// </summary>
    /// <param name="source">対象文字列。</param>
    public StringId(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            Value = 0;
            return;
        }

        Value = ComputeHash(source.AsSpan());
        RegisterToPool(Value, source);
    }

    /// <summary>
    ///     生のハッシュ値から <see cref="StringId" /> を初期化します。
    /// </summary>
    /// <param name="value">ハッシュ値。</param>
    public StringId(uint value)
    {
        Value = value;
    }

    private static void RegisterToPool(uint hash, string source)
    {
        if (_stringPool.TryGetValue(hash, out var existing))
        {
            if (!string.Equals(existing, source, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Fail($"[StringId Hash Collision] Hash 0x{hash:X8} collision between '{existing}' and '{source}'.");
            }
            return;
        }

        if (!_stringPool.TryAdd(hash, source))
        {
            if (_stringPool.TryGetValue(hash, out var concurrentExisting) &&
                !string.Equals(concurrentExisting, source, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Fail($"[StringId Hash Collision] Hash 0x{hash:X8} collision between '{concurrentExisting}' and '{source}'.");
            }
        }
    }

    /// <summary>
    ///     FNV-1a 32-bit アルゴリズムに基づき、大文字小文字を区別しないハッシュ値を算出します。
    /// </summary>
    /// <param name="text">計算対象の文字列。</param>
    /// <returns>32-bit ハッシュ値。</returns>
    public static uint ComputeHash(string? text)
    {
        return ComputeHash(text.AsSpan());
    }

    /// <summary>
    ///     FNV-1a 32-bit アルゴリズムに基づき、大文字小文字を区別しないハッシュ値を算出します（ゼロアロケーション）。
    /// </summary>
    /// <param name="text">計算対象の文字列スパン。</param>
    /// <returns>32-bit ハッシュ値。</returns>
    public static uint ComputeHash(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return 0;

        uint hash = 2166136261u;
        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToLowerInvariant(text[i]);
            hash = (hash ^ c) * 16777619u;
        }
        return hash;
    }

    /// <summary>文字列から <see cref="StringId" /> への暗黙的型変換を提供します。</summary>
    public static implicit operator StringId(string? source)
    {
        return new StringId(source);
    }

    /// <inheritdoc />
    public bool Equals(StringId other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public int CompareTo(StringId other)
    {
        return Value.CompareTo(other.Value);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (int)Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsEmpty) return "None";
        return _stringPool.TryGetValue(Value, out var text) ? text : $"StringId(0x{Value:X8})";
    }
}