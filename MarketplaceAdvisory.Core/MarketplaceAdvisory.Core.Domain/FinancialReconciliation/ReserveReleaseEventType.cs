namespace MarketplaceAdvisory.Core.Domain.FinancialReconciliation;

/// <summary>
/// Normalized event types derived from reserve-release description values.
/// </summary>
public enum ReserveReleaseEventType
{
    Pagamento = 1,
    CreditoEnvio = 2,
    Reserva = 3,
    Mediacao = 4,
    Desbloqueio = 5,
    BppShippingReturn = 6,
    Reembolso = 7,
    Indenizacao = 8,
    Antecipacao = 9,
    Outro = 10
}
