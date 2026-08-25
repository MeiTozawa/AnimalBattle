namespace AnimalBattle.Core;

/// <summary>構造化されたドメイン失敗を共通エラー境界へ変換する契約です。</summary>
public interface IDomainFailure
{
    /// <summary>ドメイン失敗を既存の簡潔なエラー表現へ変換します。</summary>
    /// <returns>診断・エラー境界で利用するエラー情報。</returns>
    SimpleError ToSimpleError();

    /// <summary>ドメイン失敗を既存の簡潔なエラー表現として取得します。</summary>
    SimpleError Error { get; }
}
