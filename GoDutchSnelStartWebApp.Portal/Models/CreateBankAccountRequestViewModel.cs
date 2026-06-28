namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class CreateBankAccountRequestViewModel
{
    public string Iban { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}