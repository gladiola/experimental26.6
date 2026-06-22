# Tarjeta de Referencia Rápida - Plantilla Razor Pages

## Inicio rápido (5 minutos)

```powershell
# 1) Script de configuración
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2) Compilar y ejecutar
dotnet build
dotnet run
```

## Archivos de configuración

| Archivo | Propósito | ¿Se confirma en git? |
|---|---|---|
| `appsettings.template.json` | Plantilla con placeholders | Sí |
| `appsettings.json` | Configuración real | No (ignorado) |
| User Secrets | Valores sensibles | No (local) |

## Feature Flags (rápido)

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## Comandos de User Secrets

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## Secretos comunes por funcionalidad

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### CSP / Nonce
```powershell
# Primero: .\SupportingScripts\IVandKeySampleGenerator.ps1
dotnet user-secrets set "NonceEncryption:Key" "your-32-byte-base64-key"
dotnet user-secrets set "NonceEncryption:IV" "your-16-byte-base64-iv"
```

### Cosmos DB
```powershell
dotnet user-secrets set "CosmosDb:CosmosConnectionString" "your-connection-string"
dotnet user-secrets set "CosmosDb:AccountKey" "your-account-key"
```

### Blob Storage
```powershell
dotnet user-secrets set "BlobSettings:BlobConnectionString" "your-connection-string"
```

### Key Vault
```powershell
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-secret"
```

## Scripts útiles

| Script | Propósito |
|---|---|
| `SetupFromTemplate.ps1` | Configuración inicial |
| `RenameNamespace.ps1` | Cambio de namespace |
| `IVandKeySampleGenerator.ps1` | Generación de clave/IV |
| `HashInlineScriptPowerShell.ps1` | Hashes CSP |

## Solución rápida de problemas

### Error de compilación
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Falta configuración
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### Falta secreto
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Bucle de autenticación / 401
1. Revisar redirect URI en Azure AD
2. Verificar `EnableAzureAd: true`
3. Confirmar secreto en User Secrets
4. Limpiar cookies del navegador

## Checklist de seguridad (producción)

- [ ] Secretos fuera del repositorio
- [ ] `appsettings.json` ignorado
- [ ] Encabezados de seguridad habilitados
- [ ] CSP con nonce habilitado
- [ ] HTTPS forzado
- [ ] Autenticación/autorización activadas
