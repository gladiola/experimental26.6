# Cartão de Referência Rápida — Modelo Razor Pages

## Início rápido (5 minutos)

```powershell
# 1) Executar o script de configuração
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2) Compilar e executar
dotnet build
dotnet run
```

## Ficheiros de configuração

| Ficheiro | Finalidade | É versionado? |
|---|---|---|
| `appsettings.template.json` | Modelo com placeholders | ✅ Sim |
| `appsettings.json` | Configuração real | ❌ Não (ignorado pelo git) |
| User Secrets | Valores sensíveis | ❌ Não (apenas local) |

## Feature Flags (ativar/desativar rapidamente)

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

## Segredos comuns por funcionalidade

### Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### CSP / Nonce
```powershell
# Primeiro: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## Scripts úteis

| Script | Finalidade |
|---|---|
| `SetupFromTemplate.ps1` | Configuração inicial |
| `RenameNamespace.ps1` | Alterar namespace |
| `IVandKeySampleGenerator.ps1` | Gerar chave/IV |
| `HashInlineScriptPowerShell.ps1` | Calcular hashes CSP |

## Resolução rápida de problemas

### Erros de compilação
```powershell
dotnet clean
dotnet restore
dotnet build
```

### Configuração em falta
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### Segredo em falta
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Loop de autenticação / erros 401
1. Verificar se o redirect URI no Azure AD está correto
2. Confirmar `EnableAzureAd: true`
3. Confirmar o segredo de cliente em User Secrets
4. Limpar cookies do navegador

## Checklist de segurança (produção)

- [ ] Segredos fora do repositório
- [ ] `appsettings.json` ignorado
- [ ] Cabeçalhos de segurança ativos
- [ ] CSP com nonce ativo
- [ ] HTTPS forçado
- [ ] Autenticação/autorização ativas

---

**Versão do modelo:** 1.0  
**ASP.NET Core:** 9.0  
**Última atualização:** 2024-12-20
