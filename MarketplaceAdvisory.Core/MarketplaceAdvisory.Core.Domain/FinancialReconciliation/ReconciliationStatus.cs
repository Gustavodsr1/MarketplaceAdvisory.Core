namespace MarketplaceAdvisory.Core.Domain.FinancialReconciliation;

/// <summary>
/// Final reconciliation status for a sale/package timeline.
/// Names mirror the documented business taxonomy.
/// </summary>
public enum ReconciliationStatus
{
    Conciliado = 1,
    RecebeuAMenos = 2,
    RecebeuAMais = 3,
    EmDisputa = 4,
    MediacaoEncerrada = 5,
    Reembolsado = 6,
    ReembolsadoIndenizado = 7,
    ReembolsoParcial = 8,
    Aguardando = 9,
    NaoIdentificado = 10
}
