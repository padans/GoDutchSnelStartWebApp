using System.Text.RegularExpressions;

namespace GoDutchSnelStartWebApp.Application.BankAccounts.Helpers;

public static class IbanValidator
{
    private static readonly Regex IbanFormat = new(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}$", RegexOptions.Compiled);

    public static bool IsValid(string iban)
    {
        var normalized = iban.Replace(" ", string.Empty).ToUpperInvariant();

        if (!IbanFormat.IsMatch(normalized))
        {
            return false;
        }

        // Move first 4 chars to end, convert letters to digits, verify mod-97 == 1
        var rearranged = normalized[4..] + normalized[..4];

        var numeric = string.Concat(rearranged.Select(c => char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        var remainder = 0;
        foreach (var ch in numeric)
        {
            remainder = (remainder * 10 + (ch - '0')) % 97;
        }

        return remainder == 1;
    }
}
