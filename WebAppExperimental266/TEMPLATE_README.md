# Razor Pages Enterprise Template

A production-ready ASP.NET Core 9 Razor Pages template with modular Azure integration, security features, and incremental adoption.

## ?? Quick Start

### Option 1: Automated Setup (Recommended)

```powershell
# Run the setup script
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# Follow the prompts to configure your application
```

### Option 2: Manual Setup

```powershell
# 1. Copy template to active config
Copy-Item appsettings.template.json appsettings.json

# 2. Initialize User Secrets
dotnet user-secrets init

# 3. Store sensitive values
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here"

# 4. Enable features in appsettings.json
# Edit FeatureFlags section

# 5. Run the application
dotnet run
```

## ?? Features

### Core Features (Always Available)
- ? Session management with configurable timeout
- ? English-only localization (extensible)
- ? OWASP security headers
- ? Comprehensive logging infrastructure
- ? Modular service architecture

### Optional Features (Enable via Configuration)
| Feature | Purpose | Phase |
|---------|---------|-------|
| ?? Azure AD | Enterprise authentication | Phase 2 |
| ??? CSP + Nonce | Content Security Policy | Phase 2 |
| ??? Azure Key Vault | Certificate management | Phase 3 |
| ?? Blob Storage | File attachments | Phase 3 |
| ?? Cosmos DB | NoSQL database | Phase 3 |
| ?? Advanced Security | Extended headers | Phase 2 |

## ?? Development Phases

### Phase 1: Basic Razor Pages (MVP)
**Goal**: Working application without Azure dependencies

```json
"FeatureFlags": {
  "EnableSession": true,
  "EnableLocalization": true,
  "EnableAzureAd": false,
  "EnableAuthorization": false,
  "EnableSecurityHeaders": true
}
```

**What works**:
- Static pages
- Basic routing
- Session state
- Security headers

**Setup time**: 5 minutes

---

### Phase 2: Authentication & Security
**Goal**: Add user authentication and advanced security

```json
"FeatureFlags": {
  "EnableAzureAd": true,
  "EnableAuthorization": true,
  "EnableNonceServices": true,
  "EnableCSP": true
}
```

**Required configuration**:
1. Register app in Azure AD
2. Generate nonce encryption keys
3. Configure authorized folders

**Setup time**: 30 minutes

---

### Phase 3: Full Azure Integration
**Goal**: Production-ready with all features

```json
"FeatureFlags": {
  "EnableKeyVault": true,
  "EnableBlobStorage": true,
  "EnableCosmosDb": true
}
```

**Required resources**:
- Azure Key Vault
- Storage Account
- Cosmos DB account

**Setup time**: 1-2 hours

## ?? Configuration Guide

### Feature Flags Reference

| Flag | Default | Description | Dependencies |
|------|---------|-------------|--------------|
| `EnableSession` | `true` | Session state management | None |
| `EnableLocalization` | `true` | Culture/language support | None |
| `EnableAzureAd` | `false` | Azure AD authentication | Azure AD app registration |
| `EnableAuthorization` | `false` | Page-level authorization | `EnableAzureAd` |
| `EnableKeyVault` | `false` | Certificate from Key Vault | Azure Key Vault, Azure AD |
| `EnableNonceServices` | `false` | CSP nonce generation | Encryption keys |
| `EnableBlobStorage` | `false` | File upload/download | Storage Account |
| `EnableCosmosDb` | `false` | NoSQL database | Cosmos DB |
| `EnableSecurityHeaders` | `true` | OWASP security headers | None |
| `EnableCSP` | `false` | Content Security Policy | `EnableNonceServices` |

### Azure AD Configuration

**1. Register application in Azure Portal**
- Navigate to Azure AD ? App registrations
- Create new registration
- Note: Client ID, Tenant ID, Domain

**2. Configure authentication**
- Add redirect URI: `https://localhost:7xxx/signin-oidc`
- Enable ID tokens
- Generate client secret

**3. Update configuration**

In `appsettings.json`:
```json
"AzureAd": {
  "TenantId": "your-tenant-id",
  "ClientId": "your-client-id"
}
```

In User Secrets:
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
```

### Nonce/CSP Configuration

**1. Generate encryption keys**
```powershell
.\SupportingScripts\IVandKeySampleGenerator.ps1
```

**2. Store in User Secrets**
```powershell
dotnet user-secrets set "NonceEncryption:Key" "generated-key"
dotnet user-secrets set "NonceEncryption:IV" "generated-iv"
```

**3. Enable features**
```json
"FeatureFlags": {
  "EnableNonceServices": true,
  "EnableCSP": true
}
```

### Cosmos DB Configuration

**1. Create Cosmos DB account**
- Create account in Azure Portal
- Create database and container
- Copy connection string

**2. Configure settings**

In `appsettings.json`:
```json
"CosmosDb": {
  "AccountEndpoint": "https://your-account.documents.azure.com:443/",
  "DatabaseName": "YourDatabase",
  "ContainerName": "YourContainer"
}
```

In User Secrets:
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
```

**3. Enable feature**
```json
"FeatureFlags": {
  "EnableCosmosDb": true
}
```

### Blob Storage Configuration

**1. Create Storage Account**
- Create in Azure Portal
- Copy connection string

**2. Store connection string**
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

**3. Configure max attachments**
```json
"BlobSettings": {
  "MaxAttachments": 10
}
```

**4. Enable feature**
```json
"FeatureFlags": {
  "EnableBlobStorage": true
}
```

### Azure Key Vault Configuration

**1. Create Key Vault**
- Create in Azure Portal
- Upload certificate
- Grant app access (via Managed Identity or Service Principal)

**2. Configure settings**

In `appsettings.json`:
```json
"AzureKeyVault": {
  "KeyVaultURL": "https://your-vault.vault.azure.net/",
  "KeyVaultPassName": "your-cert-name"
}
```

In User Secrets:
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## ??? Project Structure

```
WebAppExperimental266/
??? Extensions/
?   ??? ServiceCollectionExtensions.cs   # Service registration by feature
?   ??? ApplicationBuilderExtensions.cs  # Middleware configuration
?
??? Models/
?   ??? Settings/                        # Configuration POCOs
?   ?   ??? FeatureFlags.cs
?   ?   ??? AzureADSettings.cs
?   ?   ??? CosmosDbSettings.cs
?   ?   ??? ...
?   ??? Main_Objects/                    # Core domain models
?   ??? Storage/                         # Data models
?   ??? User/                            # User-related models
?
??? Services/                            # Business logic
?   ??? LoggingHelper.cs                 # Centralized logging
?   ??? *Middleware.cs                   # Custom middleware
?   ??? *Service.cs                      # Feature services
?
??? Pages/                               # Razor Pages
??? Controllers/                         # MVC controllers (if needed)
??? Views/                              # Shared views
?
??? SupportingScripts/                   # Setup & maintenance
?   ??? SetupFromTemplate.ps1            # Initial configuration
?   ??? IVandKeySampleGenerator.ps1      # Generate encryption keys
?   ??? HashInlineScriptPowerShell.ps1   # Calculate CSP hashes
?   ??? RenameNamespace.ps1              # Customize namespace
?
??? appsettings.template.json            # Configuration template
??? appsettings.json                     # Your config (git-ignored)
??? Program.cs                           # Application entry point
??? TEMPLATE_README.md                   # This file
```

## ?? Security Best Practices

### 1. Secrets Management

**DO**:
- ? Use User Secrets for local development
- ? Use Azure Key Vault for production
- ? Use Managed Identity when possible
- ? Rotate secrets regularly

**DON'T**:
- ? Commit secrets to source control
- ? Hard-code connection strings
- ? Share secrets via email/chat
- ? Store secrets in appsettings.json

### 2. Git Configuration

Ensure `.gitignore` includes:
```gitignore
appsettings.json
appsettings.Development.json
appsettings.*.json
!appsettings.template.json
ConfigurationsAndCodeChecks/
secrets.json
```

### 3. Security Headers

When `EnableSecurityHeaders: true`, the following headers are set:
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security`
- `Content-Security-Policy` (when CSP enabled)
- `Cross-Origin-*` headers
- `Permissions-Policy`

### 4. Content Security Policy

The CSP implementation uses:
- **Nonces** for inline scripts (cryptographically secure)
- **Hash allowlist** for static scripts
- **Strict directives** by default

Calculate hashes:
```powershell
.\SupportingScripts\HashInlineScriptPowerShell.ps1
```

## ??? Customization

### Rename Namespace

Current namespace: `WebAppExperimental266`

To customize (e.g., `YourCompany.YourApp`):

```powershell
.\SupportingScripts\RenameNamespace.ps1 -NewNamespace "YourCompany.YourApp"
```

This updates all `.cs` files automatically.

### Add Custom Service

1. **Create service class**
```csharp
// Services/MyCustomService.cs
namespace YourCompany.YourApp.Services
{
    public interface IMyCustomService
    {
        Task DoSomethingAsync();
    }
    
    public class MyCustomService : IMyCustomService
    {
        public Task DoSomethingAsync()
        {
            // Implementation
        }
    }
}
```

2. **Register in extension method**
```csharp
// Extensions/ServiceCollectionExtensions.cs
public static IServiceCollection AddMyCustomService(
    this IServiceCollection services,
    IConfiguration configuration,
    ILogger logger,
    bool enabled = true)
{
    if (!enabled)
    {
        logger.LogWarning("MyCustomService is DISABLED");
        return services;
    }

    services.AddSingleton<IMyCustomService, MyCustomService>();
    logger.LogInformation("MyCustomService configured");
    return services;
}
```

3. **Add feature flag**
```json
"FeatureFlags": {
  "EnableMyCustomFeature": true
}
```

4. **Wire in Program.cs**
```csharp
if (featureFlags.EnableMyCustomFeature)
{
    builder.Services.AddMyCustomService(
        builder.Configuration,
        logger,
        featureFlags.EnableMyCustomFeature);
}
```

### Add Authorization Policies

In `Extensions/ServiceCollectionExtensions.cs`:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("CanEdit", policy =>
        policy.RequireClaim("permission", "edit"));
});
```

Use in Razor Pages:
```csharp
[Authorize(Policy = "AdminOnly")]
public class AdminPageModel : PageModel
{
    // ...
}
```

## ?? Available Scripts

### SetupFromTemplate.ps1
**Purpose**: Initial project configuration  
**Usage**:
```powershell
.\SupportingScripts\SetupFromTemplate.ps1 -ProjectName "MyApp" -GenerateKeys
```

### IVandKeySampleGenerator.ps1
**Purpose**: Generate encryption keys for nonce services  
**Usage**:
```powershell
.\SupportingScripts\IVandKeySampleGenerator.ps1
```

### HashInlineScriptPowerShell.ps1
**Purpose**: Calculate SHA-256 hashes for inline scripts (CSP)  
**Usage**:
```powershell
.\SupportingScripts\HashInlineScriptPowerShell.ps1 -ScriptContent "console.log('hello');"
```

### RenameNamespace.ps1
**Purpose**: Change namespace throughout project  
**Usage**:
```powershell
.\SupportingScripts\RenameNamespace.ps1 -NewNamespace "Contoso.WebApp"
```

### ExportResourceGroups.ps1
**Purpose**: Export Azure resource configurations  
**Usage**:
```powershell
.\SupportingScripts\ExportResourceGroups.ps1
```

## ?? Troubleshooting

### Issue: "AzureADSettings not found"

**Symptom**: Application crashes on startup with configuration error

**Solution**:
1. Verify `appsettings.json` exists and has `AzureAd` section
2. Check placeholders are replaced
3. Verify User Secrets are set: `dotnet user-secrets list`

### Issue: "Nonce encryption fails"

**Symptom**: CSP feature throws encryption exception

**Solution**:
1. Generate new keys: `.\SupportingScripts\IVandKeySampleGenerator.ps1`
2. Store in User Secrets:
```powershell
dotnet user-secrets set "NonceEncryption:Key" "key-from-script"
dotnet user-secrets set "NonceEncryption:IV" "iv-from-script"
```

### Issue: "Cosmos DB connection timeout"

**Symptom**: Database operations fail

**Solution**:
1. Verify Cosmos DB is running: Check Azure Portal
2. Check connection string in User Secrets
3. Verify firewall rules allow your IP
4. Enable verbose logging:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"
  }
}
```

### Issue: Feature not working

**Symptom**: Expected functionality doesn't load

**Solution**:
1. Check feature flag is enabled in `appsettings.json`
2. Verify all required configuration is present
3. Check application logs for warnings
4. Ensure dependencies are enabled (e.g., CSP requires Nonce)

### Issue: Authentication loop

**Symptom**: Redirects to login repeatedly

**Solution**:
1. Verify redirect URI in Azure AD matches application
2. Check `CallbackPath` setting: `/signin-oidc`
3. Ensure cookies are not blocked
4. Clear browser cookies and try again

## ?? Additional Resources

### Official Documentation
- [ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security/)
- [Azure AD Integration](https://docs.microsoft.com/azure/active-directory/develop/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Cosmos DB](https://docs.microsoft.com/azure/cosmos-db/)

### Security References
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP Security Headers](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html)
- [Content Security Policy](https://content-security-policy.com/)

### Support
For issues specific to this template:
1. Check this README
2. Review `appsettings.template.json` comments
3. Enable debug logging
4. Check User Secrets: `dotnet user-secrets list`

## ?? License

[Specify your license here]

## ?? Contributing

This is a template project. Customize as needed for your organization.

---

**Template Version**: 1.0  
**Last Updated**: 2024-12-20  
**ASP.NET Core Version**: 9.0  
**Minimum .NET SDK**: 9.0
