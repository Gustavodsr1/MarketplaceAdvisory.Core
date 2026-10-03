namespace MarketplaceAdvisory.Core.Domain.FinancialReconciliation;

/// <summary>
/// Pure reconciliation engine for package-level financial outcomes.
/// The engine deliberately separates expected credit from final outcome.
/// </summary>
public sealed class ReconciliationEngineService
{
    private const decimal Tolerance = 0.01m;

    public ReconciliationResult Reconcile(ReconciliationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var normalizedEvents = input.ReserveReleaseEvents
            .Select(raw => new ReserveReleaseEvent(raw, Classify(raw.Description)))
            .ToArray();

        var summary = Summarize(normalizedEvents);
        var expected = ResolveExpectedCredit(input.ExpectedCreditFromSales, input.ExpectedCreditFromLiberation);
        var outcome = DeriveOutcome(summary);
        var status = ClassifyOutcome(expected.Amount, summary, outcome.FinalAmount);

        return new ReconciliationResult(expected, summary, outcome, status, normalizedEvents);
    }

    public static ReconciliationExpectedCredit ResolveExpectedCredit(
        decimal expectedCreditFromSales,
        decimal? expectedCreditFromLiberation)
    {
        if (expectedCreditFromSales < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCreditFromSales));
        }

        if (expectedCreditFromLiberation is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedCreditFromLiberation));
        }

        if (expectedCreditFromLiberation is null)
        {
            return new ReconciliationExpectedCredit(expectedCreditFromSales, "sales");
        }

        if (expectedCreditFromSales == 0m)
        {
            return new ReconciliationExpectedCredit(expectedCreditFromLiberation.Value, "liberation-fallback");
        }

        // Unidirectional rule: liberation may reduce expected credit, never increase it.
        if (expectedCreditFromLiberation.Value < expectedCreditFromSales)
        {
            return new ReconciliationExpectedCredit(expectedCreditFromLiberation.Value, "sales-reduced-by-liberation");
        }

        return new ReconciliationExpectedCredit(expectedCreditFromSales, "sales");
    }

    public static ReserveReleaseEventType Classify(string description)
    {
        var normalized = (description ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "payment" => ReserveReleaseEventType.Pagamento,
            "shipping" => ReserveReleaseEventType.CreditoEnvio,
            "reserve_for_dispute" or "reserve_for_refund" => ReserveReleaseEventType.Reserva,
            "mediation" => ReserveReleaseEventType.Mediacao,
            "mediation_cancel" => ReserveReleaseEventType.Desbloqueio,
            "reserve_for_bpp_shipping_return" => ReserveReleaseEventType.BppShippingReturn,
            "refund" => ReserveReleaseEventType.Reembolso,
            "cashback" => ReserveReleaseEventType.Indenizacao,
            "fee-release_in_advance" => ReserveReleaseEventType.Antecipacao,
            _ => ReserveReleaseEventType.Outro
        };
    }

    private static ReconciliationSummary Summarize(IReadOnlyList<ReserveReleaseEvent> events)
    {
        decimal Credits(params ReserveReleaseEventType[] types) =>
            events.Where(item => types.Contains(item.Type)).Sum(item => item.Raw.NetCreditAmount);

        decimal Debits(params ReserveReleaseEventType[] types) =>
            events.Where(item => types.Contains(item.Type)).Sum(item => item.Raw.NetDebitAmount);

        var paymentCredits = Credits(ReserveReleaseEventType.Pagamento);
        var shippingCredits = Credits(ReserveReleaseEventType.CreditoEnvio);
        var reserveDebits = Debits(ReserveReleaseEventType.Reserva, ReserveReleaseEventType.BppShippingReturn);
        var releaseCredits = Credits(ReserveReleaseEventType.Desbloqueio);
        var refundDebits = Debits(ReserveReleaseEventType.Reembolso);
        var indemnityCredits = Credits(ReserveReleaseEventType.Indenizacao);
        var advanceFeeDebits = Debits(ReserveReleaseEventType.Antecipacao);

        return new ReconciliationSummary(
            PaymentCredits: paymentCredits,
            ShippingCredits: shippingCredits,
            ReserveDebits: reserveDebits,
            ReserveReleaseCredits: releaseCredits,
            RefundDebits: refundDebits,
            IndemnityCredits: indemnityCredits,
            AdvanceFeeDebits: advanceFeeDebits,
            HasMediation: events.Any(item => item.Type is ReserveReleaseEventType.Mediacao),
            HasMediationCancel: events.Any(item => item.Type is ReserveReleaseEventType.Desbloqueio));
    }

    private static ReconciliationOutcome DeriveOutcome(ReconciliationSummary summary)
    {
        var effectiveCredit = summary.PaymentCredits + summary.ShippingCredits;

        var finalAmount =
            effectiveCredit
            + summary.ReserveReleaseCredits
            + summary.IndemnityCredits
            - summary.ReserveDebits
            - summary.RefundDebits
            - summary.AdvanceFeeDebits;

        return new ReconciliationOutcome(effectiveCredit, finalAmount);
    }

    private static ReconciliationStatus ClassifyOutcome(
        decimal expectedCredit,
        ReconciliationSummary summary,
        decimal finalAmount)
    {
        var outstandingReserve = summary.ReserveDebits - summary.ReserveReleaseCredits;

        if (summary.HasMediation && outstandingReserve > Tolerance)
        {
            return ReconciliationStatus.EmDisputa;
        }

        if (summary.HasMediationCancel && outstandingReserve <= Tolerance)
        {
            return ReconciliationStatus.MediacaoEncerrada;
        }

        if (summary.RefundDebits > Tolerance)
        {
            if (summary.IndemnityCredits > Tolerance)
            {
                return ReconciliationStatus.ReembolsadoIndenizado;
            }

            return summary.RefundDebits + Tolerance < expectedCredit
                ? ReconciliationStatus.ReembolsoParcial
                : ReconciliationStatus.Reembolsado;
        }

        if (expectedCredit <= Tolerance && Math.Abs(finalAmount) <= Tolerance)
        {
            return ReconciliationStatus.Aguardando;
        }

        var diff = finalAmount - expectedCredit;
        if (Math.Abs(diff) <= Tolerance)
        {
            return ReconciliationStatus.Conciliado;
        }

        return diff < 0m ? ReconciliationStatus.RecebeuAMenos : ReconciliationStatus.RecebeuAMais;
    }
}

public sealed record ReconciliationInput(
    decimal ExpectedCreditFromSales,
    decimal? ExpectedCreditFromLiberation,
    IReadOnlyList<ReserveReleaseRawEvent> ReserveReleaseEvents);

public sealed record ReserveReleaseRawEvent(
    string Description,
    decimal NetCreditAmount,
    decimal NetDebitAmount);

public sealed record ReserveReleaseEvent(ReserveReleaseRawEvent Raw, ReserveReleaseEventType Type);

public sealed record ReconciliationExpectedCredit(decimal Amount, string Rule);

public sealed record ReconciliationSummary(
    decimal PaymentCredits,
    decimal ShippingCredits,
    decimal ReserveDebits,
    decimal ReserveReleaseCredits,
    decimal RefundDebits,
    decimal IndemnityCredits,
    decimal AdvanceFeeDebits,
    bool HasMediation,
    bool HasMediationCancel);

public sealed record ReconciliationOutcome(decimal EffectiveCredit, decimal FinalAmount);

public sealed record ReconciliationResult(
    ReconciliationExpectedCredit ExpectedCredit,
    ReconciliationSummary Summary,
    ReconciliationOutcome Outcome,
    ReconciliationStatus Status,
    IReadOnlyList<ReserveReleaseEvent> Events);
