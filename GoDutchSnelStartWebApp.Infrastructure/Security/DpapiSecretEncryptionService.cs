using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using GoDutchSnelStartWebApp.Application.Abstractions.Security;

namespace GoDutchSnelStartWebApp.Infrastructure.Security;

[SupportedOSPlatform("windows")]
public sealed class DpapiSecretEncryptionService : ISecretEncryptionService
{
    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText.Trim());
        var protectedBytes = ProtectedData.Protect(
            plainBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.LocalMachine);

        return Convert.ToBase64String(protectedBytes);
    }

    public string? Decrypt(string? encryptedText)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return null;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(
                protectedBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Stored secret is not a valid Base64 value.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Stored secret is not encrypted with the current DPAPI configuration.", ex);
        }
    }
}