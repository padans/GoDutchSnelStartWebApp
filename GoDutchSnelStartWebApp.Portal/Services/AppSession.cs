namespace GoDutchSnelStartWebApp.Portal.Services;

public sealed class AppSession
{
    public bool IsLoggedIn { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;

    public event Action? OnChange;

    public void Login(string username, string module)
    {
        Username = username;
        Module = module;
        IsLoggedIn = true;
        NotifyChanged();
    }

    public void Logout()
    {
        Username = string.Empty;
        Module = string.Empty;
        IsLoggedIn = false;
        NotifyChanged();
    }

    private void NotifyChanged() => OnChange?.Invoke();
}
