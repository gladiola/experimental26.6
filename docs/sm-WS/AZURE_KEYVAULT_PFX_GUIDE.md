# Taʻiala Azure Key Vault mo Tusi Faamaonia PFX

## Aso Faafou: 2026-05-07

## Aotelega

O lenei taiala e faʻamatalaina le auala **saʻo** e teu ai ma toe aumai ai tusi faamaonia PFX atoa (faatasi ai ma private key) i Azure Key Vault.

---

## Mea sese masani e tatau ona aloese ai

### ❌ SESE: Teuina PFX e pei o le Base64 Secret

Aua le teuina faila PFX tele e fai ma string secret masani. E mafai ona iʻu ai i:

1. tapulaa tele (`25 KB`) mo secrets,
2. faafitauli encoding,
3. leiloa metadata o tusi faamaonia,
4. faiga faigata e pulea private key.

---

## ✅ SAʻO: Faʻaoga API o Tusi Faamaonia

### Metotia 1: Import saʻo le certificate (fautuaina)

Faʻaoga `Import-AzKeyVaultCertificate` ina ia:

- lagolago faila tele,
- teu metadata atoa,
- gaosia secret version ma private key,
- lagolago certificate rotation.

### Metotia 2: Managed Identity i gaosiga

I siosiomaga gaosiga, sili ona lelei `DefaultAzureCredential` nai lo client secret tumau.

---

## Faiga o le retrieval i C#

I retrieval saʻo, e masani ona:

1. fai `CertificateClient` e aumai metadata,
2. fai `SecretClient` e aumai secret e iai private key,
3. decode Base64 PKCS#12,
4. fau `X509Certificate2` ma key-storage flags talafeagai.

---

## Faiga i totonu o WebAppExperimental266

**Nofoaga:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Tulaga o le faila sa avea ma template. I gaosiga, e tatau ona i ai:

- logging manino i manuia/sese,
- handling mo `CryptographicException`,
- handling mo `Azure.RequestFailedException`,
- fallback saogalemu (aua le toe faafoʻi cert le atoatoa).

---

## NuGet manaʻomia

- `Azure.Identity`
- `Azure.Security.KeyVault.Certificates`
- `Azure.Security.KeyVault.Secrets`

---

## Faatulagaga

Ia mautinoa i `appsettings.json`:

- `FeatureFlags:EnableKeyVault = true`
- `AzureKeyVault:KeyVaultURL` ua saʻo
- igoa cert/secret ua fetaui ma mea i Key Vault

I dev e mafai ona teu secret i User Secrets. I gaosiga, faaaoga Managed Identity pe Key Vault references.

---

## Access policy manaʻomia

Mo identity o le app:

- **Certificate permissions:** Get, List
- **Secret permissions:** Get, List

A manaʻomia permissions uma e lua: metadata mai certificate API, private key mai secret API.

---

## Troubleshooting puʻupuʻu

### "Certificate not found"
- siaki igoa certificate,
- siaki pe i ai i Key Vault,
- siaki permission.

### "Access denied"
- siaki principal/managed identity,
- siaki ua tuuina atu Get/List i cert + secret.

### "No private key"
- ia mautinoa ua aumai foi le secret version, e le na o le certificate metadata.

### "CryptographicException"
- siaki format PFX/PKCS#12,
- siaki decode Base64,
- siaki integrity o le secret value.

---

## Lisi Migration

- [ ] Faapipii NuGet packages manaʻomia
- [ ] Faafou `AzureKeyVaultCertificateOperations.cs` mo gaosiga
- [ ] Import cert i Key Vault i metotia saʻo
- [ ] Faapipiʻi permissions (cert + secret)
- [ ] Faafou config ma user secrets/identity
- [ ] Suʻe retrieval ma faamaonia private key
- [ ] Faatulaga rotation policy

---

## Aotelega

### Fai:
- Import PFX e ala i certificate APIs
- Aumai cert + secret pe a manaʻomia private key
- Faaaoga Managed Identity i gaosiga

### Aua le faia:
- teu PFX tele e fai ma secret string masani
- faʻaalia secrets i source control
- tuʻua permissions e le atoatoa

---

**Tulaga:** ✅ Taʻiala ua saunia  
**Toe faafou mulimuli:** 2026-05-07  
**Poloketi:** WebAppExperimental266
