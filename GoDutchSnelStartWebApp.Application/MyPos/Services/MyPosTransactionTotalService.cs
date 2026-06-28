using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities.MyPos;

namespace GoDutchSnelStartWebApp.Application.MyPos.Services;

public sealed class MyPosTransactionTotalService : IMyPosTransactionTotalService
{
    private readonly IMyPosRawTransactionRepository _rawTransactionRepository;
    private readonly IMyPosTransactionTypeMappingRepository _mappingRepository;

    public MyPosTransactionTotalService(
        IMyPosRawTransactionRepository rawTransactionRepository,
        IMyPosTransactionTypeMappingRepository mappingRepository)
    {
        _rawTransactionRepository = rawTransactionRepository;
        _mappingRepository = mappingRepository;
    }

    public async Task<IReadOnlyList<MyPosTransactionTotalDto>> GetTotalsAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        bool includeExported = false,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (toUtc <= fromUtc)
        {
            throw new ArgumentException("ToUtc must be later than FromUtc.", nameof(toUtc));
        }

        var transactions = includeExported
            ? await _rawTransactionRepository.GetByTenantAsync(tenantId, fromUtc, toUtc, cancellationToken)
            : await _rawTransactionRepository.GetUnexportedAsync(tenantId, fromUtc, toUtc, cancellationToken);

        var mappings = await _mappingRepository.GetByTenantAsync(tenantId, cancellationToken);
        var mappingByCode = mappings
            .GroupBy(x => NormalizeCode(x.TransactionCode), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return transactions
            .GroupBy(x => new
            {
                TransactionType = NormalizeCode(x.TransactionType),
                Currency = string.IsNullOrWhiteSpace(x.TransactionCurrency) ? "EUR" : x.TransactionCurrency.Trim().ToUpperInvariant()
            })
            .Select(group => BuildTotal(tenantId, group.Key.TransactionType, group.Key.Currency, group, mappingByCode))
            .OrderBy(x => x.TransactionType)
            .ThenBy(x => x.Currency)
            .ToList();
    }

    private static MyPosTransactionTotalDto BuildTotal(
     Guid tenantId,
     string transactionType,
     string currency,
     IEnumerable<MyPosRawTransaction> transactions,
     IReadOnlyDictionary<string, MyPosTransactionTypeMapping> mappingByCode)
    {
        var rows = transactions.ToList();

        mappingByCode.TryGetValue(transactionType, out var mapping);

        var hasMapping = mapping is not null;
        var hasActiveMapping = mapping?.IsActive == true;

        var hasGrootboek =
            mapping?.SnelStartGrootboek is not null &&
            !string.IsNullOrWhiteSpace(mapping.SnelStartGrootboek.Nummer);

        var grossAmount = rows.Sum(x => x.TransactionAmount);

        var btwBerekening = string.IsNullOrWhiteSpace(mapping?.BtwBerekening)
            ? "Geen"
            : mapping.BtwBerekening;

        var btwSoort = mapping?.BtwSoort;
        var btwPercentage = mapping?.BtwPercentage;

        var hasVat =
            !string.Equals(btwBerekening, "Geen", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(btwSoort) &&
            btwPercentage.HasValue &&
            btwPercentage.Value > 0;

        var netAmount = grossAmount;
        var vatAmount = 0m;

        if (hasVat)
        {
            if (string.Equals(btwBerekening, "InclusiefBtw", StringComparison.OrdinalIgnoreCase))
            {
                var factor = 1m + (btwPercentage!.Value / 100m);

                netAmount = Math.Round(
                    grossAmount / factor,
                    2,
                    MidpointRounding.AwayFromZero);

                vatAmount = grossAmount - netAmount;
            }
            else if (string.Equals(btwBerekening, "ExclusiefBtw", StringComparison.OrdinalIgnoreCase))
            {
                netAmount = grossAmount;

                vatAmount = Math.Round(
                    netAmount * (btwPercentage!.Value / 100m),
                    2,
                    MidpointRounding.AwayFromZero);

                grossAmount = netAmount + vatAmount;
            }
        }

        return new MyPosTransactionTotalDto
        {
            TenantId = tenantId,
            TransactionType = transactionType,

            Description = !string.IsNullOrWhiteSpace(mapping?.Description)
                ? mapping.Description
                : rows.Select(x => x.Description).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? transactionType,

            TransactionCount = rows.Count,

            // Belangrijk: TotalAmount blijft bestaan voor bestaande batch/exportlogica.
            TotalAmount = grossAmount,

            
            // Nederlandse controlevelden voor de Portal.
            BrutoControleBedrag = grossAmount,
            NettoControleBedrag = netAmount,
            BtwControleBedrag = vatAmount,

            // Engelse velden laten we ook gevuld voor bestaande/nieuwe code.
            GrossAmount = grossAmount,
            NetAmount = netAmount,
            VatAmount = vatAmount,

            Currency = currency,

            FirstTransactionUtc = rows.Min(x => x.TransactionUtc),
            LastTransactionUtc = rows.Max(x => x.TransactionUtc),

            SnelStartGrootboekId = mapping?.SnelStartGrootboek?.Id,
            SnelStartGrootboekNummer = mapping?.SnelStartGrootboek?.Nummer,
            SnelStartGrootboekNaam = mapping?.SnelStartGrootboek?.Naam,

            BtwBerekening = btwBerekening,
            BtwSoort = btwSoort,
            BtwPercentage = btwPercentage,

            HasMapping = hasMapping,
            HasActiveMapping = hasActiveMapping,
            IsReadyForExport = hasActiveMapping && hasGrootboek,
            MappingWarning = BuildWarning(hasMapping, hasActiveMapping, hasGrootboek)
        };
    }

    private static string? BuildWarning(
    bool hasMapping,
    bool hasActiveMapping,
    bool hasGrootboek)
    {
        if (!hasMapping)
        {
            return "Geen mapping gevonden.";
        }

        if (!hasActiveMapping)
        {
            return "Mapping is niet actief.";
        }

        if (!hasGrootboek)
        {
            return "Geen SnelStart grootboek gekoppeld.";
        }

        return null;
    }

    private static string NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
    }
    private static (decimal Bruto, decimal Netto, decimal Btw) CalculateControlAmounts(
    decimal amount,
    string btwBerekening,
    decimal? btwPercentage)
    {
        var percentage = btwPercentage ?? 0m;

        if (percentage <= 0m ||
            string.Equals(btwBerekening, "Geen", StringComparison.OrdinalIgnoreCase))
        {
            return (
                Bruto: RoundMoney(amount),
                Netto: RoundMoney(amount),
                Btw: 0m);
        }

        if (string.Equals(btwBerekening, "InclusiefBtw", StringComparison.OrdinalIgnoreCase))
        {
            var factor = 1m + (percentage / 100m);
            var netto = RoundMoney(amount / factor);
            var btw = RoundMoney(amount - netto);

            return (
                Bruto: RoundMoney(amount),
                Netto: netto,
                Btw: btw);
        }

        if (string.Equals(btwBerekening, "ExclusiefBtw", StringComparison.OrdinalIgnoreCase))
        {
            var netto = RoundMoney(amount);
            var btw = RoundMoney(netto * percentage / 100m);
            var bruto = RoundMoney(netto + btw);

            return (
                Bruto: bruto,
                Netto: netto,
                Btw: btw);
        }

        return (
            Bruto: RoundMoney(amount),
            Netto: RoundMoney(amount),
            Btw: 0m);
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeBtwBerekening(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Geen";
        }

        return value.Trim() switch
        {
            "InclusiefBtw" => "InclusiefBtw",
            "ExclusiefBtw" => "ExclusiefBtw",
            _ => "Geen"
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
