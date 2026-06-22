# WebAppExperimental266

Uma aplicação web ASP.NET Core 9 Razor Pages com autenticação Azure AD, TLS mútuo (mTLS), gestão de certificados via Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore e uma camada de segurança HTTP reforçada com política de segurança de conteúdo baseada em nonces.

---

## Índice

- [Funcionalidades](#funcionalidades)
- [Feature Flags](#feature-flags)
- [Pré-requisitos](#pré-requisitos)
- [Instalação – Windows Azure (App Service)](#instalação--windows-azure-app-service)
- [Instalação – Servidor OpenBSD com serviços Azure](#instalação--servidor-openbsd-com-serviços-azure)
- [Referência de configuração](#referência-de-configuração)
- [Scripts de suporte](#scripts-de-suporte)
- [Notas de segurança](#notas-de-segurança)

---

## Funcionalidades

### Autenticação Azure AD (OpenID Connect)
A aplicação autentica utilizadores através da **Plataforma de Identidade da Microsoft** usando o protocolo OpenID Connect (via `Microsoft.Identity.Web`). Todas as rotas em `/Experimental` requerem uma identidade Azure AD autenticada. As páginas `/Privacy`, `/Error` e `/About` são acessíveis publicamente.

### Autenticação mTLS com Certificado de Cliente
Quando ativado, os clientes devem apresentar um certificado X.509 válido. As definições em `MtlsSettings` controlam se são permitidos certificados encadeados, auto-assinados ou ambos, a verificação de revogação de certificados e os emissores de certificados permitidos.

### Integração com Azure Key Vault
A aplicação obtém o **certificado do servidor** TLS a partir do Azure Key Vault no arranque. O `X509Certificate2` carregado é injetado diretamente na configuração HTTPS do Kestrel, não sendo necessário nenhum ficheiro PFX em disco.

### Política de Segurança de Conteúdo com Nonces por Pedido
Quando ativado, cada resposta HTTP contém um cabeçalho `Content-Security-Policy` cuja diretiva `script-src` inclui um **nonce aleatório criptograficamente seguro** por pedido. A CSP também suporta listas de permissões baseadas em hashes SHA-256 para scripts inline.

### Cabeçalhos de Segurança HTTP Padrão
`UseStandardSecurityHeaders` acrescenta a cada resposta: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy` e remove os cabeçalhos `Server`, `X-Powered-By` e `X-AspNetMvc-Version`.

### Azure Blob Storage
Quando ativado, `BlobSettingsService` fornece um serviço Scoped suportado por uma cadeia de ligação e um número máximo configurável de anexos.

### Azure Cosmos DB
Quando ativado, a aplicação verifica a ligação ao Cosmos DB no arranque através da chamada `database.ReadAsync()`.

### AWS Secrets Manager
Quando ativado, `AwsSecretsManagerOperationsService` obtém segredos e certificados do AWS Secrets Manager. Configuração na secção `AwsSecretsManager` com os parâmetros `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` e as credenciais `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
Quando ativado, `AwsDynamoDbService` verifica a conectividade à tabela DynamoDB no arranque. Configuração na secção `AwsDynamoDb` com os parâmetros `Region`, `TableName` e as credenciais `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
Quando ativado, `GcpSecretManagerOperationsService` obtém segredos do Google Cloud Secret Manager. Configuração na secção `GcpSecretManager` com os parâmetros `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` e `CredentialFilePath` (opcional, utiliza ADC se vazio).

### GCP Firestore
Quando ativado, `GcpFirestoreService` constrói o cliente Firestore no arranque. Configuração na secção `GcpFirestore` com os parâmetros `ProjectId`, `DatabaseId` (predefinição: "(default)"), `CollectionName` e `CredentialFilePath` (opcional).

### Gestão de identidade AWS Cognito
Quando ativado, `AddAwsCognitoAuthentication` configura a autenticação OpenID Connect contra um **Amazon Cognito User Pool** — o equivalente AWS do Microsoft Entra ID / Azure AD. O endpoint de descoberta OIDC é:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Configuração na secção `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (guardar em Segredos do Utilizador) e `Domain` (domínio da interface alojada Cognito).

### GCP Identity Platform
Quando ativado, `AddGcpIdentityAuthentication` configura a autenticação OpenID Connect usando **Google OAuth 2.0 / OIDC** — o equivalente GCP do Microsoft Entra ID / Azure AD. O endpoint de descoberta OIDC é:
`https://accounts.google.com/.well-known/openid-configuration`
Configuração na secção `GcpIdentity`: `ClientId`, `ClientSecret` (guardar em Segredos do Utilizador) e `ProjectId` opcional. Obter credenciais na **Google Cloud Console → APIs e serviços → Credenciais**.

### Gestão Segura de Sessões
As sessões utilizam uma cache de memória distribuída em processo com um **tempo limite de inatividade de 30 minutos**. Os cookies de sessão são configurados como `HttpOnly`, `Secure = Always` e `SameSite = Strict`.

### Localização
A aplicação suporta **25 idiomas** com ficheiros de recursos `.resx` por vista. A cultura ativa é determinada no pipeline de pedido via `RequestLocalizationOptions` (cabeçalho Accept-Language, query string ou cookie), e os utilizadores podem mudar de idioma a qualquer momento através do seletor de idioma na barra de navegação.

| Etiqueta de cultura | Idioma |
|---|---|
| `en-US` | English (Estados Unidos) — predefinido |
| `de-DE` | Deutsch (alemão) |
| `es-ES` | Español (espanhol) |
| `fr-FR` | Français (francês) |
| `pt-PT` | Português (português) |
| `it-IT` | Italiano |
| `zh-HK` | 廣東話 (cantonês — chinês tradicional de Hong Kong) |
| `ko-KR` | 한국어 (coreano) |
| `hi-IN` | हिन्दी (hindi) |
| `ru-RU` | Русский (russo) |
| `ar-SA` | العربية (árabe — layout da direita para a esquerda) |
| `sw-KE` | Kiswahili (suaíli) |
| `ja-JP` | 日本語 (japonês) |
| `ht-HT` | Kreyòl ayisyen (crioulo haitiano) |
| `haw-US` | ʻŌlelo Hawaiʻi (havaiano) |
| `sm-WS` | Gagana Samoa (samoano) |
| `mi-NZ` | Te Reo Māori (maori) |
| `af-ZA` | Afrikaans |
| `nl-NL` | Nederlands (neerlandês) |
| `ha-NG` | Hausa |
| `am-ET` | አማርኛ (amárico) |
| `yo-NG` | Yorùbá (iorubá) |
| `bn-BD` | বাংলা (bengali) |
| `zh-CN` | 普通话 (chinês mandarim simplificado) |
| `ga-IE` | Gaeilge (irlandês) |

O layout RTL é ativado automaticamente quando o árabe é selecionado: o elemento `<html>` recebe `dir="rtl"` e o atributo `lang` mantém a etiqueta BCP-47 completa (por exemplo, `ar-SA`) para comportamento correto no navegador.

### Registo Seguro para PII
`LoggingHelper` efetua o hash de informações de identificação pessoal na saída de registo usando HMAC-SHA256. Uma chave estável de 32 bytes pode ser fornecida via `Logging:PiiHmacKey`.

---

## Feature Flags

Todos os subsistemas principais são controlados por flags booleanas em `appsettings.json`.

| Flag | Predefinição | Descrição |
|---|---|---|
| `EnableSession` | `true` | Sessão do servidor e cookie de sessão |
| `EnableLocalization` | `true` | Suporte multilingue (en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ga-IE) |
| `EnableAzureAd` | `true` | Autenticação Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Políticas de autorização ao nível da rota |
| `EnableKeyVault` | `false` | Carregar certificado TLS do servidor a partir do Azure Key Vault |
| `EnableNonceServices` | `false` | Geração de nonce CSP por pedido |
| `EnableCSP` | `false` | Anexar cabeçalho `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Anexar cabeçalhos de segurança HTTP padrão |
| `EnableBlobStorage` | `false` | Serviço Azure Blob Storage |
| `EnableCosmosDb` | `false` | Serviço Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | Stub AWS Secrets Manager |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Gestão de identidade AWS Cognito (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | Stub GCP Secret Manager |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Exigir certificados TLS de cliente |
| `EnableOcspValidation` | `false` | Verificação de revogação OCSP (stub) |

---

## Pré-requisitos

1. **Registo de aplicação Azure AD** – com URI de redirecionamento, segredo de cliente ou credencial de certificado.
2. **Azure Key Vault** – com o certificado PFX do servidor como segredo.
3. **Conta Azure Cosmos DB** (opcional).
4. **Conta Azure Blob Storage** (opcional).
5. **.NET 9 SDK / Runtime** – versão 9.0 ou posterior.
6. **Credenciais AWS** (utilizador/função IAM com permissões `secretsmanager` e `dynamodb`) – necessárias quando `EnableAwsSecretsManager` ou `EnableAwsDynamoDb` estão ativados.
7. **Conta de serviço GCP ou ADC** (com permissões `secretmanager` e `datastore`) – necessários quando `EnableGcpSecretManager` ou `EnableGcpFirestore` estão ativados.

---

## Instalação – Windows Azure (App Service)

### 1. Criar recursos Azure

```powershell
# Log in
az login

# Create a resource group
az group create --name MyResourceGroup --location eastus

# Create an App Service plan (Linux or Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Create the web app (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Registar uma aplicação Azure AD

No [Portal Azure](https://portal.azure.com):
1. Navegue para **Microsoft Entra ID → Registos de aplicações → Novo registo**.
2. Defina o URI de redirecionamento para `https://<your-app>.azurewebsites.net/signin-oidc`.
3. Em **Certificados e segredos**, crie um segredo de cliente e copie o valor.
4. Anote o **ID do inquilino** e o **ID do cliente** a partir do painel de Visão geral.

### 3. Criar Azure Key Vault e carregar o certificado do servidor

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Upload your PFX as a Key Vault secret (base64-encoded)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Grant the App Service Managed Identity access
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. Configurar as definições da aplicação

Copie `appsettings.template.json` para `appsettings.json` e preencha os valores de marcador de posição. Os segredos **não devem** ser armazenados no controlo de código fonte — defina-os como Definições de Aplicação do App Service ou através de User Secrets localmente:

```powershell
# In Azure App Service, set secrets as app settings:
az webapp config appsettings set --name MyWebApp26 --resource-group MyResourceGroup --settings \
  "AzureAd__TenantId=<TENANT_ID>" \
  "AzureAd__ClientId=<CLIENT_ID>" \
  "AzureAd__ClientSecret=<CLIENT_SECRET>" \
  "AzureKeyVault__KeyVaultURL=https://MyKeyVault26.vault.azure.net/" \
  "AzureKeyVault__KeyVaultSecret=<KV_SECRET>" \
  "AzureKeyVault__KeyVaultPassName=ServerCert" \
  "FeatureFlags__EnableKeyVault=true" \
  "FeatureFlags__EnableAzureAd=true"
```

### 5. Implementar a aplicação

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Ativar HTTPS e domínio personalizado (recomendado)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Ativar mTLS no Azure App Service (opcional)

O Azure App Service suporta certificados de cliente através do portal:
1. Vá a **App Service → Definições de TLS/SSL → Certificados de cliente**.
2. Defina **Certificados de cliente de entrada** como **Obrigatório**.

Em seguida, defina `FeatureFlags__EnableMtls=true` nas Definições de Aplicação.

---

## Instalação – Servidor OpenBSD com serviços Azure

> **Importante:** O .NET 9 **não** tem uma compilação oficial da Microsoft para OpenBSD. As instruções abaixo utilizam um **contentor compatível com Linux** (através do [Podman](https://podman.io/), disponível na árvore de pacotes do OpenBSD) para executar a aplicação ASP.NET Core 9 no OpenBSD enquanto comunica com os serviços Azure via HTTPS.

### 1. Instalar pré-requisitos no OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

Se nem o Podman nem o Docker estiver disponível para a sua versão do OpenBSD, considere executar a aplicação numa **VM Linux** (p. ex., vmm(4) com um convidado Debian/Ubuntu) e siga o caminho de implementação Linux padrão a partir desse convidado.

### 2. Obter a imagem de runtime do ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Compilar a aplicação (numa máquina de compilação Linux ou Windows)

Numa máquina com o SDK .NET 9 instalado, publique uma compilação autónoma para Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Transfira o diretório `publish/` para o host OpenBSD (p. ex., via `scp` ou um volume partilhado).

### 4. Criar um ficheiro de configuração

No host OpenBSD, crie `/etc/webappexp26/appsettings.json` com os seus valores de produção (sem segredos no ficheiro; use variáveis de ambiente):

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": {
    "EnableAzureAd": true,
    "EnableKeyVault": true,
    "EnableSecurityHeaders": true,
    "EnableMtls": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/",
    "KeyVaultPassName": "ServerCert"
  }
}
```

Os segredos são injetados como variáveis de ambiente no passo seguinte.

### 5. Executar o contentor

```sh
podman run -d \
  --name webappexp26 \
  -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro \
  -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll \
    --contentRoot /app \
    --configDir /app/config
```

### 6. Configurar a firewall OpenBSD Packet Filter (pf)

Adicione a `/etc/pf.conf` para permitir HTTPS de entrada e ligações de saída para os endpoints Azure:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Recarregar o conjunto de regras:

```sh
pfctl -f /etc/pf.conf
```

### 7. Configurar DNS e certificados TLS

Certifique-se de que o nome de host em `AllowedHosts` resolve para o IP público do servidor OpenBSD. O Azure AD exige que o URI de redirecionamento (`/signin-oidc`) seja acessível via HTTPS, pelo que o certificado do servidor deve ser fidedigno. Utilize um certificado de uma CA pública (p. ex., Let's Encrypt através de `acme-client(1)`) ou carregue um certificado assinado por CA para o Azure Key Vault e ative `EnableKeyVault`.

### 8. Conectividade de saída para os serviços Azure

Os seguintes endpoints de serviço Azure devem ser acessíveis a partir do host OpenBSD via TCP 443:

| Service | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

Teste a conectividade antes de iniciar o contentor:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Referência de Configuração

Copie `appsettings.template.json` para `appsettings.json` e substitua todos os valores `{{PLACEHOLDER}}`.

| Secção | Chave | Descrição |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Registo de aplicação Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault e nome do certificado |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Política de certificado de cliente mTLS |
| `NonceEncryption` | `Key`, `IV` | Chave de 32 bytes e IV de 16 bytes para encriptação de nonces (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Ligação do Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Ligação do Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Validação OCSP (stub) |
| `Logging` | `PiiHmacKey` | Chave HMAC base64 de 32 bytes para hashing de PII nos registos |

Gere chaves de encriptação e IVs usando o script PowerShell incluído:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Guarde todos os segredos em **.NET User Secrets** para desenvolvimento local:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
```

> Para o GCP, defina a variável de ambiente `GOOGLE_APPLICATION_CREDENTIALS` para o caminho do ficheiro JSON da conta de serviço ou execute `gcloud auth application-default login` para desenvolvimento local.

---

## Scripts de suporte

O diretório `SupportingScripts/` contém utilitários PowerShell:

| Script | Objetivo |
|---|---|
| `IVandKeySampleGenerator.ps1` | Gerar uma chave AES aleatória de 32 bytes e um IV de 16 bytes (base64) |
| `HashInlineScriptPowerShell.ps1` | Calcular hashes SHA-256 para scripts inline (para listas de permissões CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | Igual ao anterior, gera hashes em formato base64 |
| `CertificateUploaderToAzureExample.ps1` | Carregar um certificado PFX para o Azure Key Vault |
| `CheckRoles.ps1` | Verificar as atribuições de funções RBAC do Azure para a aplicação |
| `ExportResourceGroups.ps1` | Exportar configurações de grupos de recursos do Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Diagnosticar a conectividade do Cosmos DB |
| `SetupFromTemplate.ps1` | Automatizar a configuração inicial a partir de `appsettings.template.json` |

---

## Notas de Segurança

- **Nunca confirme segredos no controlo de versões.**
- A implementação de validação OCSP é um **stub** que rejeita todos os certificados. Substitua `PerformOcspValidationAsync` antes de ativar `EnableOcspValidation` em produção.
- Os valores nonce **nunca são registados**.
- O cabeçalho de resposta `Server` é mascarado para `webserver`.
- **Nunca armazene credenciais AWS ou GCP no controlo de versões.** Utilize variáveis de ambiente ou um gestor de segredos.
- As implementações AWS e GCP são **stubs** que requerem implementação completa antes do uso em produção.
- Para AWS, prefira funções IAM em vez de chaves de acesso codificadas quando possível.
- Para GCP, prefira Application Default Credentials (ADC) em vez de ficheiros de conta de serviço explícitos.
