namespace GoDutchSnelStartWebApp.Application.Abstractions.Security;

public interface ISecretEncryptionService
{
    string? Encrypt(string? plainText);
    string? Decrypt(string? encryptedText);
}