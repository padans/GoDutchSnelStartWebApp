namespace GoDutchSnelStartWebApp.Application.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "EmailSettings";

    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "GoDutch SnelStart";
    public string ToAddress { get; set; } = string.Empty;
}
