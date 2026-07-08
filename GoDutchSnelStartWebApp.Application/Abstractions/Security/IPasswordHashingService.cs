namespace GoDutchSnelStartWebApp.Application.Abstractions.Security;

public interface IPasswordHashingService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
