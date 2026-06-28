namespace GoDutchSnelStartWebApp.Application.GoDutchAccounts.Dtos;

public sealed class GoDutchAccountLookupDto
{
    public string GoDutchAccountId { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    public string? AccountName { get; set; }

    public string? AccountHolderName { get; set; }

    public bool AlreadyExists { get; set; }

    public Guid? ExistingBankAccountId { get; set; }

    public string DisplayName
    {
        get
        {
            var name = !string.IsNullOrWhiteSpace(AccountName)
                ? AccountName
                : AccountHolderName;

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(Iban))
            {
                return $"{name} - {Iban}";
            }

            if (!string.IsNullOrWhiteSpace(Iban))
            {
                return Iban;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return GoDutchAccountId;
        }
    }
}