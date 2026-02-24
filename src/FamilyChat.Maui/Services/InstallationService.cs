namespace FamilyChat.Maui.Services;

public sealed class InstallationService
{
    private const string Key = "familychat_installation_id";

    public string GetInstallationId()
    {
        var existing = Preferences.Default.Get(Key, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var value = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(Key, value);
        return value;
    }
}
