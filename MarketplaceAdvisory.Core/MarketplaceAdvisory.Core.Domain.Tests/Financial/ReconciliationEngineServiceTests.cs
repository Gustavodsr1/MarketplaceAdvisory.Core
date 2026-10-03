using FluentAssertions;
using MarketplaceAdvisory.Core.Domain.FinancialReconciliation;

namespace MarketplaceAdvisory.Core.Domain.Tests.Financial;

public sealed class ReconciliationEngineServiceTests
{
    private readonly ReconciliationEngineService _engine = new();

    [Fact]
    public void Reconcile_WhenExpectedMatchesFinal_ReturnsConciliado()
    {
        var input = new ReconciliationInput(
            ExpectedCreditFromSales: 100m,
            ExpectedCreditFromLiberation: 100m,
            ReserveReleaseEvents:
            [
                new ReserveReleaseRawEvent("payment", NetCreditAmount: 100m, NetDebitAmount: 0m)
            ]);

        var result = _engine.Reconcile(input);

        result.Status.Should().Be(ReconciliationStatus.Conciliado);
        result.ExpectedCredit.Amount.Should().Be(100m);
        result.Outcome.EffectiveCredit.Should().Be(100m);
        result.Outcome.FinalAmount.Should().Be(100m);
    }

    [Fact]
    public void Reconcile_WhenFinalLowerThanExpected_ReturnsRecebeuAMenos()
    {
        var input = new ReconciliationInput(
            ExpectedCreditFromSales: 100m,
            ExpectedCreditFromLiberation: null,
            ReserveReleaseEvents:
            [
                new ReserveReleaseRawEvent("payment", NetCreditAmount: 90m, NetDebitAmount: 0m)
            ]);

        var result = _engine.Reconcile(input);

        result.Status.Should().Be(ReconciliationStatus.RecebeuAMenos);
    }

    [Fact]
    public void Reconcile_WhenMediationReserveOutstanding_ReturnsEmDisputa()
    {
        var input = new ReconciliationInput(
            ExpectedCreditFromSales: 100m,
            ExpectedCreditFromLiberation: null,
            ReserveReleaseEvents:
            [
                new ReserveReleaseRawEvent("payment", NetCreditAmount: 100m, NetDebitAmount: 0m),
                new ReserveReleaseRawEvent("mediation", NetCreditAmount: 0m, NetDebitAmount: 0m),
                new ReserveReleaseRawEvent("reserve_for_dispute", NetCreditAmount: 0m, NetDebitAmount: 40m)
            ]);

        var result = _engine.Reconcile(input);

        result.Status.Should().Be(ReconciliationStatus.EmDisputa);
    }

    [Fact]
    public void Reconcile_WhenRefundAndCashback_ReturnsReembolsadoIndenizado()
    {
        var input = new ReconciliationInput(
            ExpectedCreditFromSales: 100m,
            ExpectedCreditFromLiberation: null,
            ReserveReleaseEvents:
            [
                new ReserveReleaseRawEvent("payment", NetCreditAmount: 100m, NetDebitAmount: 0m),
                new ReserveReleaseRawEvent("refund", NetCreditAmount: 0m, NetDebitAmount: 100m),
                new ReserveReleaseRawEvent("cashback", NetCreditAmount: 100m, NetDebitAmount: 0m)
            ]);

        var result = _engine.Reconcile(input);

        result.Status.Should().Be(ReconciliationStatus.ReembolsadoIndenizado);
    }

    [Fact]
    public void ResolveExpectedCredit_NeverIncreasesExpectedFromSales()
    {
        var expected = ReconciliationEngineService.ResolveExpectedCredit(
            expectedCreditFromSales: 100m,
            expectedCreditFromLiberation: 120m);

        expected.Amount.Should().Be(100m);
        expected.Rule.Should().Be("sales");
    }

    [Fact]
    public void ResolveExpectedCredit_ReducesExpectedWhenLiberationIsLower()
    {
        var expected = ReconciliationEngineService.ResolveExpectedCredit(
            expectedCreditFromSales: 120m,
            expectedCreditFromLiberation: 100m);

        expected.Amount.Should().Be(100m);
        expected.Rule.Should().Be("sales-reduced-by-liberation");
    }
}
