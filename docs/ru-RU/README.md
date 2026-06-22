# WebAppExperimental266

Веб-приложение ASP.NET Core 9 Razor Pages с аутентификацией Azure AD, взаимным TLS (mTLS), управлением сертификатами через Azure Key Vault, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore и усиленным уровнем безопасности HTTP с политикой безопасности контента на основе nonce.

---

## Содержание

- [Возможности](#возможности)
- [Флаги функций](#флаги-функций)
- [Предварительные условия](#предварительные-условия)
- [Установка – Windows Azure (App Service)](#установка--windows-azure-app-service)
- [Установка – Сервер OpenBSD, взаимодействующий с Azure](#установка--сервер-openbsd-взаимодействующий-с-azure)
- [Справочник по конфигурации](#справочник-по-конфигурации)
- [Вспомогательные скрипты](#вспомогательные-скрипты)
- [Примечания по безопасности](#примечания-по-безопасности)

---

## Возможности

### Аутентификация Azure AD (OpenID Connect)
Приложение аутентифицирует пользователей через **Microsoft Identity Platform** с использованием протокола OpenID Connect (через `Microsoft.Identity.Web`). Все маршруты в `/Experimental` требуют аутентифицированного удостоверения Azure AD. Страницы `/Privacy`, `/Error` и `/About` доступны публично.

### mTLS аутентификация с клиентским сертификатом
При включении клиенты должны предоставить действительный сертификат X.509. Параметры в `MtlsSettings` управляют разрешёнными типами сертификатов (цепочечные, самоподписанные или оба), проверкой отзыва сертификатов и допустимыми издателями сертификатов.

### Интеграция с Azure Key Vault
Приложение при запуске получает TLS **сертификат сервера** из Azure Key Vault. Загруженный `X509Certificate2` напрямую вставляется в настройки HTTPS Kestrel, поэтому PFX-файл на диске не требуется.

### Политика безопасности контента с nonce на каждый запрос
При включении каждый HTTP-ответ содержит заголовок `Content-Security-Policy`, директива `script-src` которого включает **криптографически случайный nonce** на каждый запрос. CSP также поддерживает белые списки на основе хэшей SHA-256 для встроенных скриптов.

### Стандартные заголовки безопасности HTTP
`UseStandardSecurityHeaders` добавляет к каждому ответу: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy`, а также удаляет заголовки `Server`, `X-Powered-By` и `X-AspNetMvc-Version`.

### Azure Blob Storage
При включении `BlobSettingsService` предоставляет Scoped-сервис, поддерживаемый строкой подключения и настраиваемым максимальным количеством вложений.

### Azure Cosmos DB
При включении приложение проверяет подключение к Cosmos DB при запуске, вызывая `database.ReadAsync()`.

### AWS Secrets Manager
При включении `AwsSecretsManagerOperationsService` получает секреты и сертификаты из AWS Secrets Manager. Конфигурация в разделе `AwsSecretsManager` с параметрами `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` и учётными данными `AccessKeyId`/`SecretAccessKey`.

### Amazon DynamoDB
При включении `AwsDynamoDbService` проверяет подключение к таблице DynamoDB при запуске. Конфигурация в разделе `AwsDynamoDb` с параметрами `Region`, `TableName` и учётными данными `AccessKeyId`/`SecretAccessKey`.

### GCP Secret Manager
При включении `GcpSecretManagerOperationsService` получает секреты из Google Cloud Secret Manager. Конфигурация в разделе `GcpSecretManager` с параметрами `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` и `CredentialFilePath` (необязательно, использует ADC если пусто).

### GCP Firestore
При включении `GcpFirestoreService` создаёт клиент Firestore при запуске. Конфигурация в разделе `GcpFirestore` с параметрами `ProjectId`, `DatabaseId` (по умолчанию: "(default)"), `CollectionName` и `CredentialFilePath` (необязательно).

### Управление идентификацией AWS Cognito
При включении `AddAwsCognitoAuthentication` настраивает аутентификацию OpenID Connect против **Amazon Cognito User Pool** — аналога Microsoft Entra ID / Azure AD в AWS. Конечная точка обнаружения OIDC:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
Конфигурация в разделе `AwsCognito`: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret` (хранить в User Secrets) и `Domain` (домен хостируемого интерфейса Cognito).

### GCP Identity Platform
При включении `AddGcpIdentityAuthentication` настраивает аутентификацию OpenID Connect через **Google OAuth 2.0 / OIDC** — аналог Microsoft Entra ID / Azure AD в GCP. Конечная точка обнаружения OIDC:
`https://accounts.google.com/.well-known/openid-configuration`
Конфигурация в разделе `GcpIdentity`: `ClientId`, `ClientSecret` (хранить в User Secrets) и необязательный `ProjectId`. Получите учётные данные в **Google Cloud Console → API и сервисы → Учётные данные**.

### Безопасное управление сеансами
Сеансы используют распределённый кэш в памяти с **30-минутным таймаутом простоя**. Куки сеанса настроены как `HttpOnly`, `Secure = Always` и `SameSite = Strict`.

### Локализация
Приложение поддерживает **11 языков**: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU и ar-SA. Для арабского языка предусмотрено автоматическое переключение на RTL-макет.

### Безопасное журналирование персональных данных
`LoggingHelper` хэширует персонально идентифицируемую информацию в выводе журнала с использованием HMAC-SHA256. Стабильный 32-байтный ключ может быть предоставлен через `Logging:PiiHmacKey`.

---

## Флаги функций

Все основные подсистемы управляются булевыми флагами функций в `appsettings.json`.

| Флаг | По умолчанию | Описание |
|---|---|---|
| `EnableSession` | `true` | Серверный сеанс и куки сеанса |
| `EnableLocalization` | `true` | Многоязычная поддержка (11 языков) |
| `EnableAzureAd` | `true` | Аутентификация Azure AD / OpenID Connect |
| `EnableAuthorization` | `true` | Политики авторизации на уровне маршрутов |
| `EnableKeyVault` | `false` | Загрузка TLS-сертификата сервера из Azure Key Vault |
| `EnableNonceServices` | `false` | Генерация CSP nonce на каждый запрос |
| `EnableCSP` | `false` | Добавление заголовка `Content-Security-Policy` |
| `EnableSecurityHeaders` | `true` | Добавление стандартных заголовков безопасности HTTP |
| `EnableBlobStorage` | `false` | Сервис Azure Blob Storage |
| `EnableCosmosDb` | `false` | Сервис Azure Cosmos DB |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager (заглушка) |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | Управление идентификацией AWS Cognito (OpenID Connect) |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager (заглушка) |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform (Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | Требование клиентских TLS-сертификатов |
| `EnableOcspValidation` | `false` | Проверка отзыва сертификатов OCSP (заглушка) |

---

## Предварительные условия

1. **Регистрация приложения Azure AD** — с URI перенаправления, секретом клиента или учётными данными сертификата.
2. **Azure Key Vault** — содержащий PFX-сертификат сервера в качестве секрета.
3. **Учётная запись Azure Cosmos DB** (необязательно).
4. **Учётная запись Azure Blob Storage** (необязательно).
5. **.NET 9 SDK / Runtime** — версии 9.0 или выше.
6. **Учётные данные AWS** (пользователь/роль IAM с разрешениями `secretsmanager` и `dynamodb`) — необходимы при включении `EnableAwsSecretsManager` или `EnableAwsDynamoDb`.
7. **Сервисный аккаунт GCP или ADC** (с разрешениями `secretmanager` и `datastore`) — необходим при включении `EnableGcpSecretManager` или `EnableGcpFirestore`.

---

## Установка – Windows Azure (App Service)

### 1. Создание ресурсов Azure

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

### 2. Регистрация приложения Azure AD

На [портале Azure](https://portal.azure.com):
1. Перейдите в **Microsoft Entra ID → Регистрации приложений → Новая регистрация**.
2. Укажите URI перенаправления `https://<your-app>.azurewebsites.net/signin-oidc`.
3. В разделе **Сертификаты и секреты** создайте секрет клиента и скопируйте его значение.
4. Запишите **Идентификатор клиента** и **Идентификатор арендатора** из панели обзора.

### 3. Создание Azure Key Vault и загрузка серверного сертификата

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

### 4. Настройка параметров приложения

Скопируйте `appsettings.template.json` в `appsettings.json` и заполните значения заполнителей. Секреты **не должны** храниться в системе контроля версий — задайте их как параметры приложения App Service или через User Secrets локально:

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

### 5. Развёртывание приложения

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. Включение HTTPS и пользовательского домена (рекомендуется)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Включение mTLS на Azure App Service (необязательно)

Azure App Service поддерживает клиентские сертификаты через портал:
1. Перейдите в **App Service → Параметры TLS/SSL → Клиентские сертификаты**.
2. Установите **Входящие клиентские сертификаты** в значение **Требуется**.

Затем задайте `FeatureFlags__EnableMtls=true` в параметрах приложения.

---

## Установка – Сервер OpenBSD, взаимодействующий с Azure

> **Важно:** .NET 9 **не имеет** официальной сборки Microsoft для OpenBSD. Приведённые ниже инструкции используют **контейнер, совместимый с Linux** (через [Podman](https://podman.io/), доступный в дереве пакетов OpenBSD), для запуска приложения ASP.NET Core 9 на OpenBSD при взаимодействии с сервисами Azure по HTTPS.

### 1. Установка необходимых компонентов на OpenBSD

```sh
# As root
pkg_add podman
pkg_add curl git
```

Если для вашей версии OpenBSD недоступны ни Podman, ни Docker, рассмотрите запуск приложения в **виртуальной машине Linux** (например, vmm(4) с гостевой Debian/Ubuntu) и следуйте стандартному пути развёртывания Linux внутри этой ВМ.

### 2. Загрузка образа среды выполнения ASP.NET Core 9

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. Сборка приложения (на машине сборки Linux или Windows)

На машине с установленным .NET 9 SDK опубликуйте самодостаточную сборку для Linux x64:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

Перенесите каталог `publish/` на хост OpenBSD (например, через `scp` или общий том).

### 4. Создание файла конфигурации

На хосте OpenBSD создайте `/etc/webappexp26/appsettings.json` с вашими производственными значениями (без секретов в файле; используйте переменные окружения):

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

Секреты будут переданы через переменные окружения на следующем шаге.

### 5. Запуск контейнера

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

### 6. Настройка брандмауэра OpenBSD Packet Filter (pf)

Добавьте в `/etc/pf.conf` для разрешения входящего HTTPS и исходящих подключений к конечным точкам Azure:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

Перезагрузите набор правил:

```sh
pfctl -f /etc/pf.conf
```

### 7. Настройка DNS и TLS-сертификатов

Убедитесь, что имя хоста в `AllowedHosts` разрешается в публичный IP-адрес сервера OpenBSD. Azure AD требует, чтобы URI перенаправления (`/signin-oidc`) был доступен по HTTPS, поэтому серверный сертификат должен быть доверенным. Используйте сертификат от публичного CA (например, Let's Encrypt через `acme-client(1)`) или загрузите сертификат, подписанный CA, в Azure Key Vault и включите `EnableKeyVault`.

### 8. Исходящее подключение к сервисам Azure

Следующие конечные точки сервисов Azure должны быть доступны с хоста OpenBSD по TCP 443:

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

Проверьте подключение перед запуском контейнера:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## Справочник по конфигурации

Скопируйте `appsettings.template.json` в `appsettings.json` и замените все значения `{{PLACEHOLDER}}`.

| Раздел | Ключ | Описание |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Регистрация приложения Azure AD |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault и имя сертификата |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | Политика клиентского сертификата mTLS |
| `NonceEncryption` | `Key`, `IV` | 32-байтный ключ и 16-байтный IV для шифрования nonce (base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Подключение Blob Storage |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Подключение Cosmos DB |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | Проверка OCSP (заглушка) |
| `Logging` | `PiiHmacKey` | 32-байтный base64-ключ HMAC для хэширования персональных данных в журналах |

Генерируйте ключи шифрования и векторы инициализации с помощью включённого скрипта PowerShell:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

Храните все секреты в **.NET User Secrets** для локальной разработки:

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

> Для GCP задайте переменную среды `GOOGLE_APPLICATION_CREDENTIALS`, указав путь к файлу JSON сервисного аккаунта, или выполните `gcloud auth application-default login` для локальной разработки.

---

## Вспомогательные скрипты

Каталог `SupportingScripts/` содержит утилиты PowerShell:

| Скрипт | Назначение |
|---|---|
| `IVandKeySampleGenerator.ps1` | Генерация случайного 32-байтного ключа AES и 16-байтного IV (base64) |
| `HashInlineScriptPowerShell.ps1` | Вычисление хэшей SHA-256 для встроенных скриптов (для белого списка CSP) |
| `HashInlineScriptPowerShellBase64Output.ps1` | То же, что и выше, выводит хэши в формате base64 |
| `CertificateUploaderToAzureExample.ps1` | Загрузка PFX-сертификата в Azure Key Vault |
| `CheckRoles.ps1` | Проверка назначений ролей RBAC Azure для приложения |
| `ExportResourceGroups.ps1` | Экспорт конфигураций групп ресурсов Azure |
| `TroubleshootingCosmosDBInfo.ps1` | Диагностика подключения Cosmos DB |
| `SetupFromTemplate.ps1` | Автоматизация начальной настройки из `appsettings.template.json` |

---

## Примечания по безопасности

- **Никогда не фиксируйте секреты в системе контроля версий.**
- Реализация проверки OCSP является **заглушкой**, которая отклоняет все сертификаты. Замените `PerformOcspValidationAsync` перед включением `EnableOcspValidation` в production.
- Значения nonce **никогда не журналируются**.
- Заголовок ответа `Server` замаскирован значением `webserver`.
- **Никогда не храните учётные данные AWS или GCP в системе контроля версий.** Используйте переменные среды или менеджер секретов.
- Реализации AWS и GCP являются **заглушками**, требующими полноценной реализации перед использованием в production.
- Для AWS предпочитайте роли IAM жёстко закодированным ключам доступа там, где это возможно.
- Для GCP предпочитайте Application Default Credentials (ADC) явным файлам сервисного аккаунта.
