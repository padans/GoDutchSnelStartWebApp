using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Serilog;
using System.Globalization;
using System.Text;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.Generators;

public sealed class Mt940Generator : IMt940Generator
{
    private static readonly ILogger Log = Serilog.Log.ForContext<Mt940Generator>();

    private const string Block1 = "{1:F01SWNBNL22AXXX0000000000}";
    private const string Block2 = "{2:I940SWNBNL22XXXN}";

    public string Generate(Mt940Request request)
    {
        if (request == null)
        {
            Log.Error("MT940 genereren mislukt: request is null.");
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            var ccy = string.IsNullOrWhiteSpace(request.Currency)
                ? "EUR"
                : request.Currency.Trim().ToUpperInvariant();

            var iban = (request.Iban ?? string.Empty).Replace(" ", "").Trim();
            var accountAndCurrency = $"{iban}{ccy}";
            var transactions = request.Transactions ?? Enumerable.Empty<BankTransactionDto>();
            var transactionList = transactions.ToList();

            Log.Information(
                "MT940 generatie gestart. IBAN aanwezig: {HasIban}, valuta: {Currency}, periode: {FromDate:dd-MM-yyyy} t/m {ToDate:dd-MM-yyyy}, transacties: {TransactionCount}, statementnummer aanwezig: {HasStatementNumber}.",
                !string.IsNullOrWhiteSpace(iban),
                ccy,
                request.PeriodFrom,
                request.PeriodTo,
                transactionList.Count,
                !string.IsNullOrWhiteSpace(request.StatementNumber));

            var sb = new StringBuilder();

            sb.AppendLine(Block1);
            sb.AppendLine(Block2);
            sb.AppendLine("{4:");

            sb.AppendLine($":20:{request.StatementReference}");
            sb.AppendLine($":25:{accountAndCurrency}");
            sb.AppendLine($":28C:{request.StatementNumber}");

            var openingDate = request.PeriodFrom.AddDays(-1);
            var closingDate = request.PeriodTo;

            sb.AppendLine($":60F:{FormatBalance(request.OpeningBalance, openingDate, ccy)}");

            var writtenTransactions = 0;

            foreach (var t in transactionList
                         .Where(x => x.BookingDate.HasValue && x.BookingDate.Value != DateTime.MinValue)
                         .OrderBy(x => x.BookingDate!.Value))
            {
                var bookingDate = t.BookingDate!.Value.Date;

                sb.AppendLine($":61:{Format61(bookingDate, t.Amount, t.Id)}");
                sb.AppendLine("/TRCD/00000/");

                foreach (var line in Format86Lines(t.Description))
                {
                    sb.AppendLine(line);
                }

                writtenTransactions++;
            }

            sb.AppendLine($":62F:{FormatBalance(request.ClosingBalance, closingDate, ccy)}");
            sb.AppendLine($":64:{FormatBalance(request.ClosingBalance, closingDate, ccy)}");
            sb.AppendLine($":65:{FormatBalance(request.ClosingBalance, closingDate, ccy)}");

            sb.AppendLine("-}");

            var result = sb.ToString();

            Log.Information(
                "MT940 generatie succesvol afgerond. Lengte bericht: {MessageLength}, geschreven transacties: {WrittenTransactionCount}.",
                result.Length,
                writtenTransactions);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MT940 generatie geëindigd met een fout.");
            throw;
        }
    }

    private static string Format61(DateTime bookingDate, decimal amount, string? reference)
    {
        var valueDate = bookingDate.ToString("yyMMdd", CultureInfo.InvariantCulture);
        var entryDate = bookingDate.ToString("MMdd", CultureInfo.InvariantCulture);

        var dc = amount >= 0 ? "C" : "D";
        var abs = Math.Abs(amount);
        var amt = abs.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

        var refPart = string.IsNullOrWhiteSpace(reference) ? "GODUTCH" : reference.Replace(" ", "");
        if (refPart.Length > 20)
        {
            refPart = refPart[..20];
        }

        return $"{valueDate}{entryDate}{dc}{amt}NTRFNONREF//{refPart}";
    }

    private static string FormatBalance(decimal balance, DateTime date, string currency)
    {
        var dc = balance >= 0 ? "C" : "D";
        var amt = Math.Abs(balance).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
        var dt = date.ToString("yyMMdd", CultureInfo.InvariantCulture);

        return $"{dc}{dt}{currency}{amt}";
    }

    private static string[] Format86Lines(string description)
    {
        var text = (description ?? string.Empty)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            text = "GoDutch transactie";
        }

        text = $"GoDutch {text}";

        return new[]
        {
            $":86:/REMI/USTD//{text}"
        };
    }
}