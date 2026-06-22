# Руководство по реализации OCSP (Online Certificate Status Protocol)

## Обзор

Данный проект включает **шаблонную поддержку** проверки сертификатов по протоколу OCSP.
OCSP позволяет в реальном времени проверять статус отзыва сертификата перед обработкой веб-запросов.

## Что такое OCSP?

OCSP — альтернатива спискам отзыва сертификатов (CRL) для проверки, не был ли сертификат отозван:

- **Проверка в реальном времени**: немедленно проверяет статус сертификата
- **Эффективность**: запрашивает статус только конкретного сертификата
- **Лёгкость**: ответы значительно меньше полных CRL
- **Актуальность**: всегда содержит актуальную информацию об отзыве

## Конфигурация

### 1. Feature Flag

Включите проверку OCSP в `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Настройки OCSP

Настройте поведение OCSP в `appsettings.json`:

```json
{
  "OcspSettings": {
    "EnableOcspValidation": true,
    "OcspServerUrl": "https://ocsp.yourcompany.com",
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "CacheDurationMinutes": 60,
    "ServerUnavailableBehavior": "Warn",
    "EnableDetailedLogging": false,
    "SkipValidationInDevelopment": true
  }
}
```

### Параметры конфигурации

| Параметр | Тип | По умолчанию | Описание |
|----------|-----|--------------|----------|
| `EnableOcspValidation` | bool | `false` | Включить/выключить проверку OCSP |
| `OcspServerUrl` | string | `null` | URL OCSP-сервера |
| `RequestTimeoutSeconds` | int | `30` | Таймаут OCSP-запроса |
| `MaxRetryAttempts` | int | `3` | Количество повторов при ошибке |
| `CacheDurationMinutes` | int | `60` | Время кэширования ответов OCSP |
| `ServerUnavailableBehavior` | string | `"Warn"` | Поведение при недоступности сервера: `"Fail"`, `"Allow"` или `"Warn"` |
| `EnableDetailedLogging` | bool | `false` | Подробное логирование |
| `SkipValidationInDevelopment` | bool | `true` | Пропускать OCSP в режиме разработки |

---

## Шаблонная реализация

Текущая реализация — это **шаблон**, демонстрирующий структуру и дизайн API. Для использования OCSP в production необходимо:

### 1. Реализовать протокол OCSP

Замените шаблонный метод `PerformOcspValidationAsync` в `OcspValidationService.cs` реальной реализацией протокола OCSP:

```csharp
private async Task<OcspValidationResult> PerformOcspValidationAsync(X509Certificate2 certificate)
{
    // TODO: Реализовать протокол OCSP
    // 1. Сформировать OCSP-запрос
    // 2. Отправить на OCSP-сервер
    // 3. Разобрать ответ OCSP
    // 4. Проверить подпись ответа
    // 5. Вернуть статус сертификата
}
```

### 2. Развернуть OCSP-сервер

Необходим отдельный OCSP-отвечатель, который:
- Принимает OCSP-запросы (формат RFC 6960)
- Проверяет статус сертификата в базе данных CA
- Возвращает подписанные OCSP-ответы

**Варианты:**
- Использовать коммерческий сервис OCSP (DigiCert, Let's Encrypt)
- Построить собственный OCSP-отвечатель на базе:
  - **OpenSSL** — библиотека C/C++ с поддержкой OCSP
  - **BouncyCastle** — библиотека .NET для OCSP
  - **Python** — библиотека `cryptography` с поддержкой OCSP

---

## Примеры использования

### Простая проверка сертификата

```csharp
public class MyCertificateHandler
{
    private readonly IOcspValidationService _ocspService;

    public MyCertificateHandler(IOcspValidationService ocspService)
    {
        _ocspService = ocspService;
    }

    public async Task<bool> ValidateCertificateAsync(X509Certificate2 clientCert)
    {
        // Простая булева проверка
        return await _ocspService.ValidateCertificateAsync(clientCert);
    }
}
```

### Детальная проверка с получением статуса

```csharp
public async Task<OcspValidationResult> ValidateWithDetailsAsync(X509Certificate2 cert)
{
    var result = await _ocspService.ValidateCertificateWithDetailsAsync(cert);

    switch (result.Status)
    {
        case OcspStatus.Good:
            logger.LogInformation("Сертификат действителен");
            return result;

        case OcspStatus.Revoked:
            logger.LogError("Сертификат был отозван!");
            throw new SecurityException("Certificate revoked");

        case OcspStatus.Unknown:
            logger.LogWarning("Статус сертификата неизвестен");
            break;

        case OcspStatus.ServerUnavailable:
            logger.LogWarning("OCSP-сервер недоступен");
            break;
    }

    return result;
}
```

---

## Интеграция с mTLS

OCSP работает совместно с аутентификацией по клиентским сертификатам mTLS:

```csharp
// В ServiceCollectionExtensions.cs
services.AddMtlsAuthentication(configuration, logger, enabled: true);
services.AddOcspValidation(configuration, logger, enabled: true);

// В обработчике события проверки сертификата
options.Events = new CertificateAuthenticationEvents
{
    OnCertificateValidated = async context =>
    {
        var ocspService = context.HttpContext.RequestServices
            .GetRequiredService<IOcspValidationService>();

        var isValid = await ocspService.ValidateCertificateAsync(
            context.ClientCertificate);

        if (!isValid)
        {
            context.Fail("Certificate validation failed via OCSP");
        }
    }
};
```

---

## Режимы поведения при недоступности сервера

### «Fail» — строгая безопасность

```json
"ServerUnavailableBehavior": "Fail"
```

- Отклоняет запросы, если OCSP-сервер недоступен
- Наиболее безопасный вариант
- Может снижать доступность

**Используйте когда:** требуется максимальная безопасность, проверка сертификата критична.

### «Allow» — высокая доступность

```json
"ServerUnavailableBehavior": "Allow"
```

- Пропускает запросы при недоступности OCSP-сервера
- Приоритет доступности над безопасностью
- Записывает предупреждения в журнал

**Используйте когда:** доступность сервиса важнее проверки в реальном времени.

### «Warn» — сбалансированный (по умолчанию)

```json
"ServerUnavailableBehavior": "Warn"
```

- Пропускает запросы, но записывает предупреждения
- Сбалансированный подход
- Позволяет мониторить и получать оповещения

**Используйте когда:** нужно отслеживать проблемы OCSP, не блокируя трафик.

---

## Кэширование

Ответы OCSP кэшируются для снижения нагрузки на сервер:

```json
"CacheDurationMinutes": 60
```

**Преимущества:**
- Снижение числа запросов к OCSP-серверу
- Улучшение производительности
- Устойчивость при кратковременных сбоях

**Инвалидация кэша:**
- Автоматически по истечении времени кэширования
- Принудительно: перезапуск приложения

---

## Рекомендации по безопасности

### ✅ ДЕЛАЙТЕ:

- Используйте HTTPS для URL OCSP-сервера
- Проверяйте подписи OCSP-ответов
- Устанавливайте адекватное время кэширования (баланс актуальности и производительности)
- В высокозащищённых средах используйте режим `"Fail"`
- Мониторьте доступность OCSP-сервера
- Реализуйте логику повторных попыток для временных сбоев
- Логируйте все ошибки проверки OCSP

### ❌ НЕ ДЕЛАЙТЕ:

- Не используйте HTTP для OCSP в production
- Не пропускайте проверку подписи OCSP-ответа
- Не кэшируйте ответы слишком долго (более 24 часов)
- Не игнорируйте молча сбои OCSP-сервера
- Не отключайте OCSP в production без обоснования

---

## Реализация OCSP-сервера

### Вариант 1: OCSP-отвечатель OpenSSL

```bash
# Запустить OCSP-отвечатель OpenSSL
openssl ocsp -port 8080 \
    -index ca_index.txt \
    -CA ca_cert.pem \
    -rkey ocsp_key.pem \
    -rsigner ocsp_cert.pem \
    -text
```

### Вариант 2: BouncyCastle (.NET)

```csharp
using Org.BouncyCastle.Ocsp;

public class OcspResponderService
{
    public byte[] GenerateOcspResponse(OcspReq request)
    {
        // 1. Разобрать запрос
        // 2. Проверить статус сертификата в базе данных
        // 3. Сформировать ответ
        // 4. Подписать ответ
        // 5. Вернуть подписанный ответ
    }
}
```

### Вариант 3: Коммерческий OCSP-сервис

- **DigiCert** — управляемый OCSP-сервис
- **Let's Encrypt** — бесплатный OCSP для своих сертификатов
- **GlobalSign** — корпоративные OCSP-решения

---

## Мониторинг и логирование

### Включение подробного логирования

```json
{
  "OcspSettings": {
    "EnableDetailedLogging": true
  },
  "Logging": {
    "LogLevel": {
      "WebAppExperimental266.Services.OcspValidationService": "Debug"
    }
  }
}
```

### Сообщения журнала

```
[Info] OCSP validation is disabled
[Info] Validating certificate CN=Test against OCSP server https://ocsp.example.com
[Info] Using cached OCSP response for certificate ABC123
[Warning] OCSP server unavailable - Warning only: OCSP server URL is not configured
[Error] OCSP server unavailable - Rejecting request: Connection timeout
```

---

## Тестирование

### Модульные тесты

Запуск OCSP-тестов:

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

### Ручное тестирование

1. **Отключить OCSP** — убедиться, что приложение работает без OCSP
2. **Неверный URL** — проверить настройки `ServerUnavailableBehavior`
3. **Действительный сертификат** — должен возвращать `OcspStatus.Good`
4. **Кэшированный ответ** — убедиться, что кэш работает

---

## Производительность

### Конфигурация кэша

```json
"CacheDurationMinutes": 60
```

**Компромисы:**
- **Короткое время (5–15 мин)**: более свежие данные, выше нагрузка на OCSP
- **Длинное время (60–120 мин)**: лучшая производительность, риск устаревших данных

### Настройки таймаута

```json
"RequestTimeoutSeconds": 30,
"MaxRetryAttempts": 3
```

**Рекомендации:**
- Таймаут: 10–30 секунд для production
- Повторы: 2–3 попытки при временных сбоях

---

## Устранение неполадок

### Проблема: OCSP-сервер всегда недоступен

**Решения:**
1. Проверьте правильность `OcspServerUrl`
2. Убедитесь, что брандмауэр разрешает исходящий HTTPS
3. Проверьте, запущен ли OCSP-сервер
4. Проверьте журналы на ошибки таймаута

### Проблема: Все сертификаты не проходят проверку

**Решения:**
1. Убедитесь, что OCSP-сервер имеет данные о статусе сертификатов
2. Проверьте полноту цепочки сертификатов
3. Убедитесь, что подпись OCSP-ответа действительна
4. Просмотрите журналы OCSP-сервера

### Проблема: Кэш не работает

**Решения:**
1. Убедитесь, что `CacheDurationMinutes > 0`
2. Проверьте, что используется один и тот же thumbprint сертификата
3. Перезапустите приложение для очистки кэша

---

## Следующие шаги

Для полноценной работы OCSP:

1. ✅ **Конфигурация готова** — настройки подготовлены
2. ✅ **Интерфейс сервиса готов** — API определён
3. ✅ **Тесты готовы** — включено 30+ модульных тестов
4. ⏳ **Протокол OCSP** — необходимо реализовать RFC 6960
5. ⏳ **OCSP-сервер** — необходимо развернуть OCSP-отвечатель
6. ⏳ **Интеграция** — подключить к аутентификации mTLS

---

## Ссылки

- [RFC 6960](https://tools.ietf.org/html/rfc6960) — спецификация OCSP
- [Документация BouncyCastle](https://www.bouncycastle.org/csharp/)
- [OCSP в OpenSSL](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [Microsoft Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)

---

**Статус:** ✅ Шаблон готов  
**Протокол OCSP:** ⏳ Требует реализации  
**OCSP-сервер:** ⏳ Требует развёртывания  
**Тесты:** ✅ 30+ тестов включено
