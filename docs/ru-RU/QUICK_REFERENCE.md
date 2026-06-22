# Карточка быстрого старта — шаблон Razor Pages

## 🚀 Начало работы (5 минут)

```powershell
# 1. Запустить скрипт начальной настройки
.\SupportingScripts\SetupFromTemplate.ps1 -GenerateKeys

# 2. Сборка и запуск
dotnet build
dotnet run
```

## 📁 Файлы конфигурации

| Файл | Назначение | Коммитится? |
|------|------------|-------------|
| `appsettings.template.json` | Шаблон с заполнителями | ✅ Да |
| `appsettings.json` | Ваша фактическая конфигурация | ❌ Нет (в `.gitignore`) |
| User Secrets | Чувствительные значения | ❌ Нет (только локально) |

## ⚙️ Feature Flags (быстро включить/выключить)

Отредактируйте секцию `FeatureFlags` в `appsettings.json`:

```json
"FeatureFlags": {
  "EnableAzureAd": false,
  "EnableNonceServices": false,
  "EnableCosmosDb": false,
  "EnableBlobStorage": false
}
```

## 🔐 Команды User Secrets

```powershell
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-secret"
dotnet user-secrets list
dotnet user-secrets remove "AzureAd:ClientSecret"
dotnet user-secrets clear
```

## 🧩 Обязательные секреты по функциям

### Azure AD Authentication
```powershell
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
dotnet user-secrets set "AzureAD:ClientCredentials:0:ClientSecret" "your-client-secret"
```

### Nonce/CSP
```powershell
# Сначала: .\SupportingScripts\IVandKeySampleGenerator.ps1
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

## 🛠 Полезные скрипты

| Скрипт | Назначение | Использование |
|--------|------------|---------------|
| `SetupFromTemplate.ps1` | Первичная настройка | `.\SetupFromTemplate.ps1 -GenerateKeys` |
| `RenameNamespace.ps1` | Изменить namespace | `.\RenameNamespace.ps1 -NewNamespace "MyApp"` |
| `IVandKeySampleGenerator.ps1` | Сгенерировать ключи | `.\IVandKeySampleGenerator.ps1` |
| `HashInlineScriptPowerShell.ps1` | Рассчитать CSP-хэши | `.\HashInlineScriptPowerShell.ps1` |

## 🧪 Этапы разработки

### Этап 1: Базовый
- ✅ Session
- ✅ Localization
- ✅ Security headers
- ✅ Без auth
- ✅ Без базы данных

**Конфигурация:** все флаги `false`, кроме `EnableSession`, `EnableLocalization`, `EnableSecurityHeaders`

### Этап 2: + Authentication
- ✅ Возможности этапа 1
- ✅ Azure AD
- ✅ Authorization
- ✅ CSP + Nonce
- ✅ Без базы данных

**Конфигурация:** включить `EnableAzureAd`, `EnableAuthorization`, `EnableNonceServices`, `EnableCSP`

### Этап 3: + Azure Services
- ✅ Возможности этапа 2
- ✅ Cosmos DB
- ✅ Blob Storage
- ✅ Key Vault

**Конфигурация:** включить `EnableCosmosDb`, `EnableBlobStorage`, `EnableKeyVault`

## 🧯 Быстрое устранение проблем

### Ошибки сборки
```powershell
dotnet clean
dotnet build
dotnet restore
```

### "Configuration not found"
```powershell
Test-Path appsettings.json
Copy-Item appsettings.template.json appsettings.json
```

### "Secret not found"
```powershell
dotnet user-secrets list
.\SupportingScripts\SetupFromTemplate.ps1
```

### Цикл входа / 401
1. Проверьте Redirect URI в Azure AD.
2. Проверьте `EnableAzureAd: true` в `appsettings.json`.
3. Проверьте client secret в User Secrets.
4. Очистите cookies браузера.

### Ошибки CSP
1. Проверьте `EnableNonceServices: true`.
2. Проверьте, что ключи шифрования заданы.
3. Проверьте ошибки CSP в консоли браузера.
4. Для теста временно отключите CSP: `EnableCSP: false`.

## 📚 Документация

- **Полная документация:** `TEMPLATE_README.md`
- **Конфигурация:** `appsettings.template.json`
- **Переименование namespace:** `.\RenameNamespace.ps1 -NewNamespace "YourNamespace"`

## ✅ Чеклист безопасности

- [ ] Все секреты в Azure Key Vault или User Secrets
- [ ] `appsettings.json` не коммитится
- [ ] Включены заголовки безопасности
- [ ] CSP настроена с nonce
- [ ] HTTPS принудительно включён
- [ ] Для защищённых страниц включена аутентификация
- [ ] Секреты ротированы

## 💡 Советы

- Начинайте с этапа 1 и добавляйте функции постепенно.
- Используйте `-WhatIf` для теста скриптов.
- Для диагностики включайте `"Default": "Debug"` в `Logging:LogLevel`.
- Проверяйте секреты через `dotnet user-secrets list`.

## 🆘 Помощь

1. Прочитайте `TEMPLATE_README.md`
2. Проверьте комментарии в `appsettings.template.json`
3. Выполните `dotnet user-secrets list`
4. Включите debug-логирование
5. Проверьте статус ресурсов в Azure Portal

---

**Версия шаблона:** 1.0  
**ASP.NET Core:** 9.0  
**Последнее обновление:** 2026-05-06
