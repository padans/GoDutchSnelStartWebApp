namespace GoDutchSnelStartWebApp.Portal.Models;

public sealed class TenantGoDutchConnectionViewModel
{
    public Guid? Id { get; set; }
    public Guid TenantId { get; set; }

    public string ApiBaseUrl { get; set; } = "https://backend.godutch.com/";
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Alleen gevuld bij opslaan. De backend retourneert dit veld nooit.
    /// Laat leeg bij wijzigen om het bestaande versleutelde wachtwoord te behouden.
    /// </summary>
    public string? Password { get; set; }

    public bool HasPassword { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}
