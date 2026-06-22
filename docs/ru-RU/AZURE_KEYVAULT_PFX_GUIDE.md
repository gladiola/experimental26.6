# Руководство по сертификатам PFX в Azure Key Vault

## Дата: 2024-12-20

## Обзор

Данное руководство описывает **правильный подход** к хранению и извлечению полных PFX-сертификатов
(с закрытыми ключами) в Azure Key Vault на основе опыта, полученного в production-реализации.

---

## ⚠️ **Распространённые ошибки, которых следует избегать**

### ❌ **НЕВЕРНО: Хранить PFX как Base64-секрет**

```powershell
# НЕ ДЕЛАЙТЕ ТАК — это не работает!
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
$base64Pfx = [Convert]::ToBase64String($pfxBytes)
$secureSecret = ConvertTo-SecureString -String $base64Pfx -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secureSecret
```

**Почему это не работает:**
1. **Ограничение размера**: секреты Key Vault имеют лимит 25 КБ — PFX-файлы часто превышают его
2. **Проблемы кодирования**: Base64-кодирование может вносить переносы строк и искажения
3. **Несоответствие типа**: секреты предназначены для простых строк, а не бинарных данных сертификата
4. **Нет метаданных**: теряются даты истечения, сведения о субъекте и т.д.

---

## ✅ **ВЕРНО: Использовать специализированные API для сертификатов**

### **Метод 1: Импорт сертификата напрямую (рекомендуется)**

Это **наилучший подход** и именно он применяется в коде проекта.

#### Загрузить сертификат (PowerShell)

```powershell
# Определить переменные
$vaultName = "your-keyvault-name"
$certificateName = "server-cert"
$pfxFilePath = "C:\путь\к\сертификату.pfx"
$plainPassword = "ваш-пароль-pfx"

# Преобразовать пароль в SecureString
$securePassword = ConvertTo-SecureString $plainPassword -AsPlainText -Force

# Импортировать сертификат в Key Vault
Import-AzKeyVaultCertificate `
    -VaultName $vaultName `
    -Name $certificateName `
    -FilePath $pfxFilePath `
    -Password $securePassword
```

**Преимущества:**
- ✅ Обрабатывает сертификаты любого размера
- ✅ Сохраняет все метаданные сертификата
- ✅ Автоматически создаёт секретную версию с закрытым ключом
- ✅ Поддерживает ротацию сертификатов
- ✅ Интегрируется с Azure RBAC и политиками доступа

#### Получить сертификат (C#)

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public async Task<X509Certificate2?> GetCertificateFromKeyVaultAsync(
    string tenantId,
    string clientId,
    string clientSecret,
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // Создать учётные данные
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // Инициализировать клиент сертификата
        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);

        // Получить сертификат (открытый ключ и метаданные)
        KeyVaultCertificateWithPolicy certificate =
            await certificateClient.GetCertificateAsync(certificateName);

        // Получить секрет, содержащий закрытый ключ
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        // Значение секрета — это Base64-encoded PKCS12 (PFX) с закрытым ключом
        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        // Создать X509Certificate2 с закрытым ключом
        return new X509Certificate2(
            pfxBytes,
            (string?)null, // Пароль не нужен — Key Vault выполняет дешифрование
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Error loading PFX certificate from Key Vault");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error retrieving certificate");
        return null;
    }
}
```

---

### **Метод 2: Использование управляемого удостоверения (production)**

Для production-сред используйте **Managed Identity** вместо клиентских секретов.

```csharp
public async Task<X509Certificate2?> GetCertificateWithManagedIdentityAsync(
    string keyVaultUrl,
    string certificateName)
{
    try
    {
        // DefaultAzureCredential автоматически использует Managed Identity в Azure
        var credential = new DefaultAzureCredential();

        var certificateClient = new CertificateClient(new Uri(keyVaultUrl), credential);
        var certificate = await certificateClient.GetCertificateAsync(certificateName);

        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        var secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        return new X509Certificate2(
            pfxBytes,
            (string?)null,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving certificate with Managed Identity");
        return null;
    }
}
```

---

## 🔧 **Реализация в WebAppExperimental266**

### Текущий статус реализации

**Расположение:** `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

**Статус:** ⏳ Шаблонная реализация — требует production-кода

**Текущий код (шаблон):**
```csharp
public async Task<X509Certificate2?> GetCertificateFromKeyVault(
    string tenantId,
    string clientId,
    string keyVaultURL,
    string certificateName,
    string certPasswordName)
{
    // Шаблонная реализация — пользователи должны заменить этот код для production
    _logger.LogWarning("GetCertificateFromKeyVault called - implement this method for production use");

    return await Task.FromResult<X509Certificate2?>(null);
}
```

### Рекомендуемое обновление

Замените шаблон готовой к production реализацией:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System.Security.Cryptography.X509Certificates;

public class AzureKeyVaultCertificateOperations : IAzureKeyVaultCertificateOperations
{
    private readonly ILogger<AzureKeyVaultCertificateOperations> _logger;

    public AzureKeyVaultCertificateOperations(ILogger<AzureKeyVaultCertificateOperations> logger)
    {
        _logger = logger;
    }

    public async Task<X509Certificate2?> GetCertificateFromKeyVault(
        string tenantId,
        string clientId,
        string keyVaultURL,
        string certificateName,
        string certPasswordName)
    {
        try
        {
            _logger.LogInformation("Retrieving certificate '{CertName}' from Key Vault", certificateName);

            // Вариант 1: DefaultAzureCredential (рекомендуется для production)
            var credential = new DefaultAzureCredential();

            // Вариант 2: ClientSecretCredential (при наличии клиентского секрета)
            // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var certificateClient = new CertificateClient(new Uri(keyVaultURL), credential);
            KeyVaultCertificateWithPolicy certificate =
                await certificateClient.GetCertificateAsync(certificateName);

            _logger.LogDebug("Certificate found. Thumbprint: {Thumbprint}, Expires: {Expiry}",
                certificate.Properties.Thumbprint, certificate.Properties.ExpiresOn);

            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);
            KeyVaultSecret secret = await secretClient.GetSecretAsync(certificate.SecretId.Name);

            byte[] pfxBytes = Convert.FromBase64String(secret.Value);

            var x509Certificate = new X509Certificate2(
                pfxBytes,
                (string?)null,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.PersistKeySet);

            _logger.LogInformation("Successfully loaded certificate with private key");

            return x509Certificate;
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error loading certificate '{CertName}'", certificateName);
            return null;
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault error: {StatusCode} - {Message}",
                ex.Status, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving certificate");
            return null;
        }
    }

    public async Task<KeyVaultSecret> GetSecretFromKeyVault(
        string tenantId,
        string clientId,
        string clientSecret,
        string keyVaultURL,
        string secretName)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var secretClient = new SecretClient(new Uri(keyVaultURL), credential);

            return await secretClient.GetSecretAsync(secretName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}'", secretName);
            throw;
        }
    }
}
```

---

## 📦 **Необходимые NuGet-пакеты**

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

> **Примечание:** Пакеты уже установлены в проекте WebAppExperimental266.

---

## ⚙️ **Конфигурация**

### appsettings.json

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "{{USE_USER_SECRETS}}",
    "KeyVaultPassName": "server-cert"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "ClientCertificateName": "client-cert"
  }
}
```

### User Secrets

```powershell
# Для аутентификации с клиентским секретом
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "your-client-secret"

# Для Managed Identity (production)
# Секреты не нужны — удостоверение управляется Azure
```

---

## 🔑 **Политики доступа Azure Key Vault**

### Необходимые права

Для удостоверения приложения (Service Principal или Managed Identity):

**Права на сертификаты:**
- ✅ Get
- ✅ List

**Права на секреты:**
- ✅ Get
- ✅ List

**Почему нужны и права на сертификаты, и на секреты?**
- Права на сертификаты — для получения метаданных
- Права на секреты — для получения закрытого ключа

### Настройка через Azure Portal

1. Перейдите в Key Vault → Политики доступа
2. Нажмите «Добавить политику доступа»
3. Выберите права на сертификаты: Get, List
4. Выберите права на секреты: Get, List
5. Выберите субъект (ваше приложение или управляемое удостоверение)
6. Сохраните

### Настройка через Azure CLI

```bash
# Получить Object ID приложения или управляемого удостоверения
APP_OBJECT_ID=$(az ad sp show --id <app-id> --query id -o tsv)

# Выдать права
az keyvault set-policy \
  --name your-keyvault \
  --object-id $APP_OBJECT_ID \
  --certificate-permissions get list \
  --secret-permissions get list
```

---

## 🧪 **Тестирование реализации**

### Пример модульного теста

```csharp
[Fact]
public async Task GetCertificateFromKeyVault_ReturnsValidCertificate()
{
    var operations = new AzureKeyVaultCertificateOperations(_mockLogger.Object);

    var certificate = await operations.GetCertificateFromKeyVault(
        tenantId: "your-tenant-id",
        clientId: "your-client-id",
        keyVaultURL: "https://your-kv.vault.azure.net/",
        certificateName: "server-cert",
        certPasswordName: "not-used");

    certificate.Should().NotBeNull();
    certificate!.HasPrivateKey.Should().BeTrue();
    certificate.Subject.Should().NotBeNullOrEmpty();
}
```

### Интеграционный тест

```csharp
[Fact]
public async Task LoadCertificateFromActualKeyVault_Works()
{
    var keyVaultUrl = TestConfiguration["AzureKeyVault:KeyVaultURL"];
    var certName = TestConfiguration["AzureKeyVault:CertificateName"];

    var operations = new AzureKeyVaultCertificateOperations(_logger);

    var cert = await operations.GetCertificateFromKeyVault(
        tenantId: TestConfiguration["AzureAd:TenantId"],
        clientId: TestConfiguration["AzureAd:ClientId"],
        keyVaultURL: keyVaultUrl,
        certificateName: certName,
        certPasswordName: "");

    Assert.NotNull(cert);
    Assert.True(cert.HasPrivateKey, "Certificate must have private key");
}
```

---

## 🔗 **Использование в mTLS**

### Интеграция с аутентификацией по сертификату

```csharp
// В Program.cs
if (featureFlags.EnableMtls && featureFlags.EnableKeyVault)
{
    var keyVaultService = app.Services.GetRequiredService<IAzureKeyVaultOperationsService>();
    var serverCertificate = await keyVaultService.FetchCertificateServer();

    if (serverCertificate != null)
    {
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ServerCertificate = serverCertificate;
            });
        });

        logger.LogInformation("mTLS enabled with Key Vault certificate");
    }
}
```

---

## 📊 **Сравнение: Хранение как секрет vs. как сертификат**

| Функциональность | Хранить как секрет | Хранить как сертификат |
|------------------|--------------------|------------------------|
| **Ограничение размера** | 25 КБ | Без ограничений |
| **Закрытый ключ** | ❌ Ручная обработка | ✅ Автоматически |
| **Метаданные** | ❌ Отсутствуют | ✅ Полные данные |
| **Ротация** | ❌ Вручную | ✅ Встроенная |
| **Истечение срока** | ❌ Отслеживать вручную | ✅ Автоматически |
| **RBAC** | Базовый | Для сертификатов |
| **Сложность** | Высокая | Низкая |
| **Рекомендация** | ❌ Не использовать | ✅ **Использовать** |

---

## 🔄 **Ротация сертификатов**

### Автоматическая ротация

Сертификаты Key Vault поддерживают автоматическую ротацию:

```powershell
az keyvault certificate set-policy `
    --vault-name your-keyvault `
    --name server-cert `
    --policy @policy.json
```

policy.json:
```json
{
  "lifetimeActions": [
    {
      "trigger": {
        "daysBeforeExpiry": 30
      },
      "action": {
        "actionType": "AutoRenew"
      }
    }
  ]
}
```

### Код приложения

Приложение автоматически получает последнюю версию:

```csharp
// Всегда получает текущую версию
var certificate = await certificateClient.GetCertificateAsync(certificateName);
```

Для получения конкретной версии:
```csharp
var certificate = await certificateClient.GetCertificateAsync(
    certificateName,
    version: "specific-version-id");
```

---

## 🔍 **Устранение неполадок**

### Ошибка: «Certificate not found»

**Проверьте:**
1. Имя сертификата указано верно
2. Сертификат существует в Key Vault
3. Политики доступа настроены корректно

```bash
az keyvault certificate list --vault-name your-keyvault
```

### Ошибка: «Access denied»

**Проверьте:**
1. У Service Principal есть нужные права
2. Выданы права и на сертификаты, и на секреты
3. Включено управляемое удостоверение (если используется)

```bash
az keyvault show --name your-keyvault --query properties.accessPolicies
```

### Ошибка: «Certificate has no private key»

**Проверьте:**
1. Используется `.GetSecretAsync()`, а не только `.GetCertificateAsync()`
2. Сертификат был импортирован с закрытым ключом
3. Используется правильная версия секрета

```csharp
// НЕВЕРНО — нет закрытого ключа
var cert = await certificateClient.GetCertificateAsync(name);
byte[] derCert = cert.Value.Cer; // Только открытый ключ

// ВЕРНО — есть закрытый ключ
var cert = await certificateClient.GetCertificateAsync(name);
var secret = await secretClient.GetSecretAsync(cert.SecretId.Name);
byte[] pfxBytes = Convert.FromBase64String(secret.Value); // Содержит закрытый ключ
```

### Ошибка: «CryptographicException»

**Частые причины:**
1. Данные PFX повреждены
2. Неверный формат сертификата
3. Неверный пароль (при использовании Key Vault не требуется)

```csharp
try
{
    var cert = new X509Certificate2(pfxBytes);
}
catch (CryptographicException ex)
{
    _logger.LogError("PFX data length: {Length}, First 20 chars: {Preview}",
        pfxBytes.Length,
        Convert.ToBase64String(pfxBytes.Take(20).ToArray()));
    throw;
}
```

---

## ✅ **Чеклист миграции**

- [ ] Установить необходимые NuGet-пакеты
- [ ] Обновить `AzureKeyVaultCertificateOperations.cs` production-кодом
- [ ] Импортировать сертификат в Key Vault через `Import-AzKeyVaultCertificate`
- [ ] Настроить политики доступа (Certificate: Get/List, Secret: Get/List)
- [ ] Обновить конфигурацию в `appsettings.json`
- [ ] Настроить Managed Identity (production) или клиентский секрет (разработка)
- [ ] Протестировать извлечение сертификата
- [ ] Убедиться, что закрытый ключ присутствует
- [ ] Протестировать mTLS с полученным сертификатом
- [ ] Настроить политику ротации сертификатов
- [ ] Задокументировать процедуры управления сертификатами

---

## 📝 **Итоги**

### ✅ ДЕЛАЙТЕ:
- Используйте `Import-AzKeyVaultCertificate` для загрузки PFX
- Используйте `CertificateClient` + `SecretClient` для извлечения
- Используйте Managed Identity в production
- Выдавайте права и на сертификаты, и на секреты
- Проверяйте наличие закрытого ключа в сертификате

### ❌ НЕ ДЕЛАЙТЕ:
- Не храните PFX как Base64-секрет
- Не пытайтесь управлять данными сертификата вручную
- Не используйте клиентские секреты в production
- Не забывайте выдавать права на секреты
- Не игнорируйте даты истечения сертификатов

---

## 📚 **Ссылки**

- [Azure Key Vault Certificates Overview](https://docs.microsoft.com/azure/key-vault/certificates/)
- [Azure.Security.KeyVault.Certificates Package](https://docs.microsoft.com/dotnet/api/azure.security.keyvault.certificates)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [X509Certificate2 Class](https://docs.microsoft.com/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)

---

**Статус:** ✅ Руководство готово  
**Последнее обновление:** 2024-12-20  
**Версия:** 1.0  
**Проект:** WebAppExperimental266
