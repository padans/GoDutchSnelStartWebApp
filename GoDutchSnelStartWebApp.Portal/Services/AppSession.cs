namespace GoDutchSnelStartWebApp.Portal.Services;

public sealed class AppSession
{
    public bool IsLoggedIn { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string TenantName { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public bool RequirePasswordChange { get; private set; }
    public Guid? TenantId { get; private set; }
    public bool SnelStartKeyExpired { get; private set; }
    public bool SnelStartKeyExpiringSoon { get; private set; }
    public int? SnelStartKeyExpiresInDays { get; private set; }

    public event Action? OnChange;

    public void Login(string username, string module, Guid userId = default, bool requirePasswordChange = false, Guid? tenantId = null)
    {
        Username = username;
        Module = module;
        UserId = userId;
        RequirePasswordChange = requirePasswordChange;
        TenantId = tenantId;
        IsLoggedIn = true;
        NotifyChanged();
    }

    public void SetTenantName(string tenantName)
    {
        TenantName = tenantName;
        NotifyChanged();
    }

    public void SetSnelStartKeyExpiry(DateTime? keyExpiresUtc)
    {
        var now = DateTime.UtcNow;
        SnelStartKeyExpired = keyExpiresUtc.HasValue && keyExpiresUtc.Value < now;
        SnelStartKeyExpiringSoon = keyExpiresUtc.HasValue && keyExpiresUtc.Value < now.AddDays(7) && !SnelStartKeyExpired;
        SnelStartKeyExpiresInDays = keyExpiresUtc.HasValue ? (int)Math.Ceiling((keyExpiresUtc.Value - now).TotalDays) : null;
        NotifyChanged();
    }

    public void ClearPasswordChangeRequired()
    {
        RequirePasswordChange = false;
        NotifyChanged();
    }

    public void Logout()
    {
        Username = string.Empty;
        Module = string.Empty;
        TenantName = string.Empty;
        UserId = Guid.Empty;
        RequirePasswordChange = false;
        TenantId = null;
        SnelStartKeyExpired = false;
        SnelStartKeyExpiringSoon = false;
        SnelStartKeyExpiresInDays = null;
        IsLoggedIn = false;
        NotifyChanged();
    }

    private void NotifyChanged() => OnChange?.Invoke();
}
