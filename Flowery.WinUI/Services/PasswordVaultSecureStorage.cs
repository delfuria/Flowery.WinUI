using System;
using Windows.Security.Credentials;

namespace Flowery.Services;

/// <summary>
/// Implementation of ISecureStorage using the native Windows credential vault.
/// </summary>
public class PasswordVaultSecureStorage : ISecureStorage
{
    private readonly Lazy<PasswordVault> _vault = new(() => new PasswordVault());

    public void SetValue(string resource, string key, string value)
    {
        if (string.IsNullOrEmpty(resource)) throw new ArgumentNullException(nameof(resource));
        if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

        var vault = _vault.Value;
        using var scope = PasswordVaultOperationScope.Begin();
        try
        {
            var existing = vault.Retrieve(resource, key);
            vault.Remove(existing);
        }
        catch (Exception)
        {
            // Ignore missing credential.
        }

        if (value != null)
        {
            vault.Add(new PasswordCredential(resource, key, value));
        }
    }

    public string? GetValue(string resource, string key)
    {
        if (string.IsNullOrEmpty(resource)) return null;
        if (string.IsNullOrEmpty(key)) return null;

        var vault = _vault.Value;
        using var scope = PasswordVaultOperationScope.Begin();
        try
        {
            var credential = vault.Retrieve(resource, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (Exception)
        {
            // PasswordVault throws if the resource/user combination is not found.
            return null;
        }
    }

    public void RemoveValue(string resource, string key)
    {
        if (string.IsNullOrEmpty(resource)) return;
        if (string.IsNullOrEmpty(key)) return;

        var vault = _vault.Value;
        using var scope = PasswordVaultOperationScope.Begin();
        try
        {
            var credential = vault.Retrieve(resource, key);
            vault.Remove(credential);
        }
        catch (Exception)
        {
            // Ignore missing credential.
        }
    }
}
