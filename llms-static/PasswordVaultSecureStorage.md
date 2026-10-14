# PasswordVaultSecureStorage

The Windows implementation of `ISecureStorage` using the operating system credential vault.

## Overview

`PasswordVaultSecureStorage` wraps `Windows.Security.Credentials.PasswordVault` and stores Windows credentials in Credential Manager.
If the vault reports an unsupported operation, the implementation falls back to `ApplicationData.Current.LocalSettings`.
That fallback is not a secure credential vault.

## Performance and Security

- **Encryption**: Data is encrypted at rest using OS-level keys.
- **Isolation**: Each entry is keyed by `resource` and `key`, ensuring isolation between different integrations.
- **Exceptions**: The implementation internally handles cases where keys are missing, returning `null` instead of throwing exceptions for expected "not found" scenarios.

## Quick Start

```csharp
using Flowery.Services;

// Create the storage instance
ISecureStorage secureStorage = new PasswordVaultSecureStorage();

// Store a secret
secureStorage.SetValue("App.Integrations", "ApiKey", "12345-Secret-Value");

// Retrieve it
string? token = secureStorage.GetValue("App.Integrations", "ApiKey");
```

## API Reference

### Methods

| Method | Description |
| -------- | ----------- |
| `SetValue` | Adds or updates a credential in the vault. Automatically handles resource cleanup to avoid duplicates. |
| `GetValue` | Retrieves the password for the given resource and key. Returns null if not found or if access is denied. |
| `RemoveValue` | Safely removes the credential if it exists. No-op if it doesn't exist. |

## Implementation Details

The class uses `PasswordCredential` as the underlying transport. It calls `.RetrievePassword()` internally before returning the string value, as some platforms only return metadata until the password is explicitly requested.

## See Also

- [ISecureStorage](ISecureStorage.md)
- [GitHubUserProvider](GitHubUserProvider.md)
- [PlatformCompatibility](../Flowery.WinUI/Services/PlatformCompatibility.cs)
