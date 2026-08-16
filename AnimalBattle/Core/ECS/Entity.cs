using System;

namespace AnimalBattle.Core.ECS;
/// <summary>
/// Bevy エンジン (<c>bevy_ecs/entity</c>) に準拠した C# 軽量エンティティ識別子構造体。
/// 64ビット (Index 32bit + Generation 32bit) でリソース管理とメモリ再利用の安全性を担保します。
/// </summary>
public readonly struct Entity(uint index, uint generation) : IEquatable<Entity>
{
    /// <summary>無効な Null エンティティ定数</summary>
    public static readonly Entity Null = new(0, 0);

    /// <summary>エンティティのインデックス</summary>
    public uint Index { get; } = index;

    /// <summary>エンティティの世代（再利用検出用カウンタ）</summary>
    public uint Generation { get; } = generation;

    /// <summary>エンティティが有効であるか判定します</summary>
    public bool IsValid => Index != 0 || Generation != 0;

    public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}
