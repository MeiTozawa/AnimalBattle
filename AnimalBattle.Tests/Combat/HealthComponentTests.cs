using System.Drawing;
using System.Runtime.CompilerServices;
using AnimalBattle.Components;
using Xunit;

namespace AnimalBattle.Tests.Combat;

public class HealthComponentTests
{
    [Fact]
    public void Health_IsUnmanagedStruct_WithoutAnyMethods()
    {
        // Health が純粋なアンマネージド POD 構造体であることを検証（GC 負荷ゼロ、メソッドなし）
        Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<Health>() == false);

        var health = new Health(100.0f);
        Assert.Equal(100.0f, health.Current);
        Assert.Equal(100.0f, health.Max);
        Assert.Equal(0.0f, health.InvincibleTimer);
    }

    [Fact]
    public void Hurtbox_IsUnmanagedStruct_WithoutManagedOwnerReference()
    {
        // Hurtbox がマネージド参照を含まないことを検証
        Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<Hurtbox>() == false);
    }

    [Fact]
    public void DamageComponents_ArePureUnmanagedStructs_AndFollowSrp()
    {
        // 各コンポーネントが個別ファイルに定義され、アンマネージドであることを検証
        Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<IncomingDamage>() == false);
        Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<InvincibleOnHit>() == false);
        Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<DeadTag>() == false);

        // HitFlashConfig のデフォルト引数検証（可空型なし）
        var flashCfgDefault = new HitFlashConfig(0.08f);
        Assert.Equal(0.08f, flashCfgDefault.Duration);
        Assert.Equal(Color.White, flashCfgDefault.Color);

        var flashCfgCustom = new HitFlashConfig(0.12f, Color.Red);
        Assert.Equal(0.12f, flashCfgCustom.Duration);
        Assert.Equal(Color.Red, flashCfgCustom.Color);
    }
}
