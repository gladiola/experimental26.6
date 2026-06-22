# Cárta Tagartha Tapa - Teimpléad Razor Pages

## 🚀 Tosú (5 nóiméad)

```powershell
# 1. Rith script socraithe
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Tóg & rith
dotnet build
dotnet run
```

## 📁 Comhaid Chumraíochta

| Comhad | Cuspóir | Geallta? |
|------|---------|------------|
| `appsettings.template.json` | Teimpléad le háitsealbhóirí | ✅ Sea |
| `appsettings.json` | Do chumraíocht fhíor | ❌ Níl (git-ignored) |
| User Secrets | Luachanna íogaire | ❌ Níl (áitiúil amháin) |

## 🎛️ Bratacha Gné (Cumasaigh/Díchumasaigh go tapa)

Cuir in eagar rannán `FeatureFlags` i `appsettings.json`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,        // 🔐 Cuir ar siúl le haghaidh auth
  "EnableNonceServices": false,  // 🛡️ Cuir ar siúl le haghaidh CSP
  "EnableCosmosDb": false,       // 🗄️ Cuir ar siúl le haghaidh bunachar
  "EnableBlobStorage": false     // 📦 Cuir ar siúl le haghaidh comhad
}
```

## 🔑 Orduithe User Secrets

```powershell
# Tús
 dotnet user-secrets init

# Socraigh rún
 dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"

# Liostaigh gach rún
 dotnet user-secrets list

# Bain rún
 dotnet user-secrets remove "AzureAd:ClientSecret"

# Glan gach rún
 dotnet user-secrets clear
```

## 🧩 Rúin riachtanacha de réir gné

### Fíordheimhniú Azure AD
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Gin ar dtús: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## 🛠️ Scripteanna Úsáideacha

| Script | Cuspóir | Úsáid |
|--------|---------|-------|
| `SetupFromTemplate.ps1` | Socrú tosaigh | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Athraigh namespace | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Gin eochracha | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Ríomh hais CSP | `.\HashInlineScriptPowerShell.ps1` |

## 📈 Céimeanna Forbartha

### Céim 1: Bunúsach (5 nóiméad socraithe)
- ✅ Seisiún
- ✅ Logánú
- ✅ Ceanntásca slándála
- ❌ Gan auth
- ❌ Gan bunachar

**Cumraíocht**: gach bratach `false` ach amháin `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Céim 2: + Fíordheimhniú (30 nóiméad socraithe)
- ✅ Gnéithe Céim 1
- ✅ Azure AD
- ✅ Údarú
- ✅ CSP + Nonce
- ❌ Gan bunachar

**Cumraíocht**: cumasaigh `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

**Teastaíonn**:
- Clárú app Azure AD
- Eochracha criptithe ginte

### Céim 3: + Seirbhísí Azure (1-2 uair an chloig socraithe)
- ✅ Gnéithe Céim 2
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**Cumraíocht**: cumasaigh `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

**Teastaíonn**:
- Acmhainní Azure cruthaithe
- Sreanga ceangail i User Secrets

## 🧯 Fabhtcheartú Tapa

### Earráidí Tógála
```powershell
# Glan agus atóg
dotnet clean
dotnet build

# Seiceáil pacáistí in easnamh
dotnet restore
```

### "Configuration not found"
```powershell
# Deimhnigh go bhfuil an comhad ann
Test-Path appsettings.json

# Má tá sé in easnamh, cóipeáil ón teimpléad
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
# Liostaigh rúin
dotnet user-secrets list

# Rith an socrú arís
.\SupportingScripts\SetupFromTemplate.ps1
```

### Lúb auth / earráidí 401
1. Seiceáil go meaitseálann URI atreoraithe Azure AD
2. Deimhnigh `EnableAzureAd: true` in appsettings.json
3. Seiceáil rún cliaint i User Secrets
4. Glan fianáin an bhrabhsálaí

### Sáruithe CSP
1. Deimhnigh `EnableNonceServices: true`
2. Seiceáil go bhfuil eochracha criptithe socraithe
3. Athbhreithnigh consól an bhrabhsálaí le haghaidh earráidí CSP
4. Díchumasaigh CSP go sealadach chun tástáil: `EnableCSP: false`

## 📚 Doiciméadú

- **Doiciméadú iomlán**: `TEMPLATE_README.md`
- **Cumraíocht**: `appsettings.template.json`
- **Namespace**: rith `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## 🔒 Seicliosta Slándála

Sula n-imscarfar chuig táirgeadh:

- [ ] Gach rún in Azure Key Vault nó User Secrets
- [ ] `appsettings.json` faoi git-ignore
- [ ] `.gitignore` san áireamh do neamhaird teimpléid
- [ ] Ceanntásca slándála cumasaithe
- [ ] CSP cumraithe le nonceanna
- [ ] HTTPS forfheidhmithe
- [ ] Fíordheimhniú cumasaithe do leathanaigh chosanta
- [ ] Rúin rothlaithe ó réamhshocruithe

## 💡 Leideanna

- **Tosaigh simplí**: tosaigh le Céim 1 agus cuir gnéithe leis de réir a chéile
- **Úsáid WhatIf**: tástáil scripteanna le `-WhatIf` sula gcuirtear i bhfeidhm
- **Seiceáil logaí**: cumasaigh `"Default": "Debug"` i `Logging:LogLevel` chun fabhtcheartú
- **Fíoraigh rúin**: rith `dotnet user-secrets list` chun socruithe a fheiceáil
- **Tógálacha glana**: má tá earráidí aisteacha ann, bain triail as `dotnet clean && dotnet build`

## 🆘 Cabhair

1. Léigh `TEMPLATE_README.md`
2. Seiceáil nótaí in `appsettings.template.json`
3. Rith `dotnet user-secrets list`
4. Cumasaigh logáil dífhabhtaithe
5. Seiceáil stádas acmhainní in Azure Portal

---

**Leagan Teimpléid**: 1.0  
**ASP.NET Core**: 9.0  
**Nuashonraithe Deireanach**: 2024-12-20
