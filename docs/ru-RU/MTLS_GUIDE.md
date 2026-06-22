# Руководство по аутентификации mTLS (Mutual TLS) с клиентскими сертификатами

## Обзор

Данный проект поддерживает **взаимный TLS (mTLS)**, при котором и сервер, и клиент предъявляют валидные сертификаты. Это обеспечивает усиленную безопасность за счёт двусторонней аутентификации.

## Что такое mTLS?

mTLS расширяет стандартный TLS, добавляя:
1. **Сертификат сервера**: сервер предъявляет сертификат для подтверждения своей идентичности (стандартный HTTPS)
2. **Сертификат клиента**: клиент также предъявляет сертификат для подтверждения своей идентичности (дополнение mTLS)

## Конфигурация

### 1. Feature Flag

Включите mTLS в `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Настройки mTLS

Настройте поведение mTLS в `appsettings.json`:

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

#### Параметры конфигурации

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `RequireClientCertificate` | bool | `true` | Если `true`, клиентский сертификат обязателен |
| `AllowCertificateChains` | bool | `true` | Разрешить цепочечные (CA-подписанные) сертификаты |
| `AllowSelfSignedCertificates` | bool | `false` | Разрешить самоподписанные сертификаты (только для разработки) |
| `CheckCertificateRevocation` | bool | `false` | Выполнять онлайн-проверку отзыва |
| `ClientCertificateName` | string | null | Имя сертификата в Azure Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Проверять издателя сертификата |

### 3. Сертификат сервера (Azure Key Vault)

Сертификат сервера извлекается из Azure Key Vault:

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Инструкции по настройке

### Предварительные условия

1. Azure Key Vault с необходимыми правами доступа
2. Сертификат сервера, сохранённый в Azure Key Vault как секрет (формат PFX)
3. Клиентские сертификаты (можно сгенерировать или получить от CA)

### Шаг 1: Загрузка серверного сертификата в Key Vault

```bash
# Конвертировать сертификат в PFX при необходимости
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt

# Загрузить в Key Vault через Azure CLI
az keyvault secret set --vault-name "your-keyvault" --name "server-cert" --file server.pfx --encoding base64

# Сохранить пароль как отдельный секрет
az keyvault secret set --vault-name "your-keyvault" --name "server-cert-password" --value "your-password"
```

### Шаг 2: Генерация клиентских сертификатов

#### Вариант A: Самоподписанный (только для разработки)

```powershell
# Сгенерировать клиентский сертификат
$cert = New-SelfSignedCertificate `
    -Subject "CN=MyClient" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 2048 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(2)

# Экспортировать в PFX
$password = ConvertTo-SecureString -String "ClientCertPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "client.pfx" -Password $password
```

#### Вариант B: Подписанный CA (production)

Обратитесь к своему Удостоверяющему Центру (CA) для получения клиентских сертификатов.

### Шаг 3: Настройка приложения

Обновите `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert",
    "KeyVaultPassName": "server-cert-password"
  },
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false
  }
}
```

### Шаг 4: Тестирование с клиентским сертификатом

#### С использованием cURL:

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

#### С использованием PowerShell:

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

#### Через браузер:

1. Импортируйте клиентский сертификат в хранилище сертификатов браузера.
2. Перейдите на адрес вашего приложения.
3. Браузер предложит выбрать клиентский сертификат.

## Поведение в зависимости от среды

### Разработка
- Сертификат сервера загружается из Key Vault (при наличии)
- Клиентские сертификаты **необязательны** (режим `AllowCertificate`)
- Можно разрешить самоподписанные сертификаты

### Production
- Сертификат сервера загружается из Key Vault
- Клиентские сертификаты **обязательны** при `EnableMtls = true`
- Рекомендуются только цепочечные сертификаты

## Рекомендации по безопасности

### ✅ ДЕЛАЙТЕ:
- Используйте CA-подписанные сертификаты в production
- Храните сертификаты в Azure Key Vault
- Включайте проверку отзыва сертификатов в production
- Проверяйте издателя сертификата
- Используйте надёжные пароли для PFX-файлов
- Регулярно ротируйте сертификаты

### ❌ НЕ ДЕЛАЙТЕ:
- Не используйте самоподписанные сертификаты в production
- Не коммитьте сертификаты в систему контроля версий
- Не используйте один клиентский сертификат для нескольких пользователей
- Не отключайте проверку сертификатов в production

## Устранение неполадок

### Ошибка: «No client certificate provided»

**Причина:** Клиент не отправил сертификат  
**Решение:**
- Убедитесь, что клиентский сертификат установлен
- Проверьте параметр `RequireClientCertificate`
- Убедитесь, что сертификат доверен системе

### Ошибка: «Certificate chain validation failed»

**Причина:** Сертификат не доверен  
**Решение:**
- Установите корневой CA-сертификат
- Для тестирования установите `AllowSelfSignedCertificates = true`
- Убедитесь, что срок действия сертификата не истёк

### Ошибка: «Server certificate not retrieved from Key Vault»

**Причина:** Проблема доступа к Azure Key Vault  
**Решение:**
- Проверьте права доступа к Key Vault
- Проверьте учётные данные Azure AD
- Убедитесь, что управляемое удостоверение настроено

## Логирование

События аутентификации mTLS записываются в журнал:

```
[Information] mTLS enabled - Client certificates REQUIRED
[Information] mTLS Authentication SUCCEEDED for certificate: CN=MyClient
[Error] mTLS Authentication FAILED: Certificate validation failed
```

## Интеграция с существующей аутентификацией

mTLS работает совместно с аутентификацией Azure AD:

1. **Проверка клиентского сертификата** — происходит сначала (на транспортном уровне)
2. **Аутентификация Azure AD** — происходит следом (на уровне приложения)

Оба механизма можно включить одновременно для эшелонированной защиты (defense-in-depth).

## Ссылки

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)

## Пример кода

Реализация находится в следующих файлах:
- `Models/Settings/MtlsSettings.cs` — модель конфигурации
- `Models/Settings/FeatureFlags.cs` — флаг функции
- `Extensions/ServiceCollectionExtensions.cs` — регистрация сервисов
- `Program.cs` — запуск приложения

## Дополнительные ресурсы

Примеры загрузки сертификатов приведены в `SupportingScripts/CertificateUploaderToAzureExample.ps1`.
