using System.Globalization;
using System.Security;
using System.Text;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Serilog;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.Generators;

public sealed class Camt053Generator : ICamt053Generator
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<Camt053Generator>();

    public string Generate(Mt940Request request)
    {
        if (request == null)
        {
            Log.Error("CAMT053 genereren mislukt: request is null.");
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            var currency = string.IsNullOrWhiteSpace(request.Currency)
                ? "EUR"
                : request.Currency.Trim().ToUpperInvariant();

            var now = DateTime.UtcNow;

            var msgId = $"GODUTCH-{now:yyyyMMddHHmmss}";
            var creDtTm = now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

            var stmtId = !string.IsNullOrWhiteSpace(request.StatementReference)
                ? request.StatementReference.Trim()
                : $"GODUTCH{DateTime.Now:yyyyMMddHHmmss}";

            var elctrncSeqNb = ParseStatementNumber(request.StatementNumber);

            var fromDate = request.PeriodFrom.Date;
            var toDate = request.PeriodTo.Date;

            var openingBalance = request.OpeningBalance;
            var closingBalance = request.ClosingBalance;

            var entries = request.Transactions ?? Enumerable.Empty<BankTransactionDto>();
            var entryList = entries.ToList();

            Log.Information(
                "CAMT053 generatie gestart. IBAN aanwezig: {HasIban}, valuta: {Currency}, periode: {FromDate:dd-MM-yyyy} t/m {ToDate:dd-MM-yyyy}, transacties: {TransactionCount}, statementnummer: {StatementNumber}.",
                !string.IsNullOrWhiteSpace(request.Iban),
                currency,
                fromDate,
                toDate,
                entryList.Count,
                elctrncSeqNb);

            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:camt.053.001.02\">");
            sb.AppendLine("  <BkToCstmrStmt>");
            sb.AppendLine("    <GrpHdr>");
            sb.AppendLine($"      <MsgId>{Xml(msgId)}</MsgId>");
            sb.AppendLine($"      <CreDtTm>{creDtTm}</CreDtTm>");
            sb.AppendLine("    </GrpHdr>");

            sb.AppendLine("    <Stmt>");
            sb.AppendLine($"      <Id>{Xml(stmtId)}</Id>");
            sb.AppendLine($"      <ElctrncSeqNb>{elctrncSeqNb}</ElctrncSeqNb>");
            sb.AppendLine($"      <CreDtTm>{creDtTm}</CreDtTm>");

            sb.AppendLine("      <Acct>");
            sb.AppendLine("        <Id>");
            sb.AppendLine($"          <IBAN>{Xml(request.Iban)}</IBAN>");
            sb.AppendLine("        </Id>");
            sb.AppendLine($"        <Ccy>{Xml(currency)}</Ccy>");
            sb.AppendLine("      </Acct>");

            sb.AppendLine("      <FrToDt>");
            sb.AppendLine($"        <FrDtTm>{fromDate:yyyy-MM-dd}T00:00:00</FrDtTm>");
            sb.AppendLine($"        <ToDtTm>{toDate:yyyy-MM-dd}T23:59:59</ToDtTm>");
            sb.AppendLine("      </FrToDt>");

            AppendBalance(
                sb,
                code: "OPBD",
                amount: openingBalance,
                currency: currency,
                date: fromDate);

            foreach (var t in entryList)
            {
                var bookingDate = (t.BookingDate ?? request.StatementDate).Date;
                var amountAbs = Math.Abs(t.Amount);
                var cdtDbt = t.Amount >= 0m ? "CRDT" : "DBIT";
                var txId = string.IsNullOrWhiteSpace(t.Id) ? Guid.NewGuid().ToString("N") : t.Id.Trim();
                var description = string.IsNullOrWhiteSpace(t.Description) ? "Transactie" : t.Description.Trim();

                sb.AppendLine("      <Ntry>");
                sb.AppendLine($"        <Amt Ccy=\"{Xml(currency)}\">{ToAmount(amountAbs)}</Amt>");
                sb.AppendLine($"        <CdtDbtInd>{cdtDbt}</CdtDbtInd>");
                sb.AppendLine("        <Sts>BOOK</Sts>");
                sb.AppendLine($"        <BookgDt><Dt>{bookingDate:yyyy-MM-dd}</Dt></BookgDt>");
                sb.AppendLine($"        <ValDt><Dt>{bookingDate:yyyy-MM-dd}</Dt></ValDt>");
                sb.AppendLine($"        <AcctSvcrRef>{Xml(txId)}</AcctSvcrRef>");
                sb.AppendLine("        <NtryDtls>");
                sb.AppendLine("          <TxDtls>");
                sb.AppendLine($"            <Refs><AcctSvcrRef>{Xml(txId)}</AcctSvcrRef></Refs>");
                sb.AppendLine($"            <RmtInf><Ustrd>{Xml(description)}</Ustrd></RmtInf>");
                sb.AppendLine("          </TxDtls>");
                sb.AppendLine("        </NtryDtls>");
                sb.AppendLine("      </Ntry>");
            }

            AppendBalance(
                sb,
                code: "CLBD",
                amount: closingBalance,
                currency: currency,
                date: toDate);

            sb.AppendLine("    </Stmt>");
            sb.AppendLine("  </BkToCstmrStmt>");
            sb.AppendLine("</Document>");

            var result = sb.ToString();

            Log.Information(
                "CAMT053 generatie succesvol afgerond. Lengte XML: {XmlLength}, transacties: {TransactionCount}.",
                result.Length,
                entryList.Count);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "CAMT053 generatie geëindigd met een fout.");
            throw;
        }
    }

    private static void AppendBalance(
        StringBuilder sb,
        string code,
        decimal amount,
        string currency,
        DateTime date)
    {
        var cdtDbt = amount >= 0m ? "CRDT" : "DBIT";
        var abs = Math.Abs(amount);

        sb.AppendLine("      <Bal>");
        sb.AppendLine($"        <Tp><CdOrPrtry><Cd>{Xml(code)}</Cd></CdOrPrtry></Tp>");
        sb.AppendLine($"        <Amt Ccy=\"{Xml(currency)}\">{ToAmount(abs)}</Amt>");
        sb.AppendLine($"        <CdtDbtInd>{cdtDbt}</CdtDbtInd>");
        sb.AppendLine($"        <Dt><Dt>{date:yyyy-MM-dd}</Dt></Dt>");
        sb.AppendLine("      </Bal>");
    }

    private static int ParseStatementNumber(string? statementNumber)
    {
        if (string.IsNullOrWhiteSpace(statementNumber))
        {
            return 1;
        }

        var digits = new string(statementNumber.Where(char.IsDigit).ToArray());

        if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0)
        {
            return number;
        }

        return 1;
    }

    private static string ToAmount(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string Xml(string? value)
    {
        return SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
    }
}