# Проверка Безопасности — WebAppExperimental266

**Дата:** 2026-05-07
**Область:** Полный статический анализ кодовой базы (продолжение проверки от 2026-05-06)
**Проверяющий:** Автоматизированная Проверка Безопасности

---

## Резюме для руководства

Данная продолжающая проверка подтверждает, что 3 из 5 уязвимостей, выявленных в ходе проверки безопасности от 2026-05-06, были полностью устранены, 1 остаётся частично устранённой. В ходе проверки также выявлено 4 новых находки. Общий уровень безопасности приложения продолжает улучшаться.

---

## Статус предыдущих находок (2026-05-06)

| # | Находка | Серьёзность | Статус |
|---|---------|----------|--------|
| 20 | NonceRefresherService сохраняет неиспользуемые зависимости конструктора Key Vault | 🟠 Высокая | ✅ Исправлено |
| 21 | Внутренний кэш OcspValidationService использует непотокобезопасный Dictionary | 🟡 Средняя | ✅ Исправлено |
| 22 | Заглушка валидации OCSP всё ещё присутствует — завершается с ошибкой в закрытом режиме, но не реализована | 🔵 Низкая | ⚠️ Принято (по дизайну) |
| 23 | mTLS с пустым AllowedIssuers отклоняет все сертификаты (fail-closed, не задокументировано) | 🔵 Низкая | ✅ Исправлено |
| 24 | OcspSettings.ServerUnavailableBehavior по умолчанию равен "Warn" (разрешает сквозной режим при ошибке) | 🔵 Низкая | ⚠️ Частично исправлено |

---

## Подробный статус предыдущих находок

### ✅ 20. NonceRefresherService Неиспользуемые DI-зависимости — Исправлено

**Файл:** `Services/NonceRefresherService.cs`

Конструктор `NonceRefresherService` теперь объявляет только `ILogger<NonceRefresherService>`, `ILoggerFactory` и `INonceCatalogService`. Четыре ранее неиспользуемые зависимости (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) были удалены. Это устраняет риск отказа в обслуживании, который не позволял запустить приложение при `EnableKeyVault = false` (по умолчанию) и `EnableNonceServices = true` (по умолчанию).

---

### ✅ 21. Непотокобезопасный кэш OcspValidationService — Исправлено

**Файл:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` был заменён на `ConcurrentDictionary<string, CachedOcspResponse>`. Вызов `_cache.Remove` обновлён до `_cache.TryRemove`. Кэш теперь безопасен для параллельного доступа.

---

### ⚠️ 22. Заглушка валидации OCSP — Принято (По дизайну)

**Файл:** `Services/OcspValidationService.cs`

Заглушка остаётся, но корректно завершается с ошибкой в закрытом режиме. Поскольку `EnableOcspValidation` по умолчанию равен `false`, это не оказывает влияния на продакшен. Принято в качестве информационной находки до полной реализации OCSP.

---

### ✅ 23. mTLS Пустой AllowedIssuers — Исправлено

**Файл:** `Extensions/ServiceCollectionExtensions.cs`

При запуске теперь регистрируется предупреждение, если `ValidateClientCertificateIssuer = true` и `AllowedIssuers` пуст:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Это обеспечивает чёткое руководство для операторов, столкнувшихся с поведением fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Частично исправлено

**Файлы:** `appsettings.template.json` (исправлено), `Models/Settings/OcspSettings.cs` (ещё не исправлено)

Шаблон теперь правильно указывает `"ServerUnavailableBehavior": "Fail"`. Однако значение по умолчанию класса C# в `OcspSettings.cs` (строка 39) остаётся `"Warn"`. Если оператор включит OCSP и опустит `ServerUnavailableBehavior` в своём файле конфигурации, значение по умолчанию класса `"Warn"` применяется молча, допуская сквозной режим при сбоях сервера OCSP. Значение по умолчанию класса должно быть изменено в соответствии с рекомендацией шаблона.

---

## Новые находки

| # | Область | Серьёзность |
|---|------|----------|
| 25 | Значение по умолчанию класса OcspSettings ("Warn") отличается от шаблона ("Fail") | 🔵 Низкая |
| 26 | Единый общий ключ nonce в NonceCatalogService допускает коллизию nonce между запросами | 🟡 Средняя |
| 27 | Статические счётчики OptimizedNonceMiddleware используют знаковые 32-битные целые (риск переполнения) | 🔵 Низкая |
| 28 | Program.cs регистрирует пустой синглтон ILoggerFactory, скрывая фреймворковый логгер | 🟡 Средняя |

---

## 🟡 Средняя

### 26. Общий ключ Nonce в NonceCatalogService допускает коллизию Nonce между запросами

**Файлы:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

Каталог nonce хранит все nonce под единым общим ключом `"CSPNonce"`. При параллельной нагрузке возможно следующее состояние гонки:

1. Запрос A вызывает `RefreshNonceAsync()` — nonce A1 сохраняется как `_nonceCollection["CSPNonce"]`.
2. Запрос B вызывает `RefreshNonceAsync()` — nonce B1 перезаписывает `_nonceCollection["CSPNonce"]`.
3. Запрос A вызывает `GetANonce("CSPNonce")` — получает B1, а не A1.
4. Заголовок CSP и nonce макета запроса A содержат B1.
5. Запрос B также содержит B1.

Два параллельных ответа совместно используют один и тот же nonce. Хотя оба значения по-прежнему криптографически случайны и непредсказуемы (нет захардкоженной строки), одно и то же значение nonce появляется в нескольких одновременных ответах, ослабляя гарантию уникальности на запрос, требуемую спецификацией CSP. Злоумышленник, способный наблюдать nonce одного ответа, получает действительный nonce хотя бы для одного другого параллельного ответа.

**Рекомендация:** Генерировать nonce непосредственно внутри middleware для каждого запроса (например, `Nonce.GenerateSecureNonce()`) и хранить его только в `HttpContext.Items["Nonce"]`, минуя общий каталог для nonce на запрос. Общий каталог потребуется только в том случае, если nonce необходимо совместно использовать между слоями middleware в рамках одного запроса, что `HttpContext.Items` уже обрабатывает нативно.

---

### 28. Program.cs Регистрирует Пустой Синглтон ILoggerFactory

**Файл:** `Program.cs` (строка 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core автоматически регистрирует полностью настроенный `ILoggerFactory` (со всеми провайдерами логирования из конфигурации `builder.Logging`) при вызове `WebApplication.CreateBuilder`. Эта явная регистрация `AddSingleton` добавляет второй, ненастроенный экземпляр `LoggerFactory` без провайдеров. Поскольку `GetRequiredService<ILoggerFactory>()` возвращает последнюю зарегистрированную реализацию, сервисы, получающие `ILoggerFactory` через внедрение зависимостей (например, `NonceRefresherService`), будут использовать эту пустую фабрику и не будут производить никакого вывода в лог через `_loggerFactory.CreateLogger<T>()`.

**Риск:** Тихое логирование в `NonceRefresherService` — успехи и сбои генерации nonce не передаются ни в один настроенный приёмник логирования. Это снижает наблюдаемость приложения во время операций, чувствительных к безопасности, без влияния на функциональность.

**Рекомендация:** Удалить явную регистрацию `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. Настроенный `ILoggerFactory` фреймворка (с консолью и другими провайдерами) будет корректно разрешён сервисами, зависящими от него.

---

## 🔵 Низкая / Информационная

### 25. Значение по умолчанию класса OcspSettings отличается от шаблона

**Файл:** `Models/Settings/OcspSettings.cs` (строка 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

Шаблон (`appsettings.template.json`) указывает `"ServerUnavailableBehavior": "Fail"`, но значение по умолчанию класса C# — `"Warn"`. Если `ServerUnavailableBehavior` отсутствует в активном файле конфигурации, значение по умолчанию класса применяется молча вместо рекомендации шаблона. Это остаток от находки #24.

**Рекомендация:** Изменить значение по умолчанию класса с `"Warn"` на `"Fail"`, чтобы привести его в соответствие с шаблоном и принципом наименьших привилегий.

---

### 27. Статические счётчики OptimizedNonceMiddleware могут переполниться

**Файл:** `Services/OptimizedNonceMiddleware.cs` (строки 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Эти знаковые 32-битные счётчики атомарно инкрементируются через `Interlocked.Increment`. После приблизительно 2,1 млрд инкрементов они обернутся в `int.MinValue` (−2 147 483 648), что приведёт к тому, что расчёт эффективности `(total - generated) * 100.0 / total` будет давать неверные или бессмысленные результаты. При 1 000 запросов в секунду переполнение наступит приблизительно через 24,8 дня непрерывной работы.

**Рекомендация:** Изменить типы полей счётчиков с `int` на `long` и использовать перегрузку `long` метода `Interlocked.Increment` для предотвращения переполнения.

---

## Оценка заголовков безопасности (текущее состояние)

Следующие заголовки применяются через `UseStandardSecurityHeaders` — без изменений по сравнению с предыдущей проверкой:

| Заголовок | Значение | Оценка |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Хорошо |
| `X-XSS-Protection` | `0` | ✅ Хорошо (отключает устаревший аудитор) |
| `X-Content-Type-Options` | `nosniff` | ✅ Хорошо |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Хорошо |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Хорошо |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Хорошо |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Хорошо |
| `Permissions-Policy` | геолокация, камера, микрофон, interest-cohort отключены | ✅ Хорошо |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Хорошо |
| `Content-Security-Policy` | На основе nonce, применяется при включённом CSP | ✅ Хорошо |
| `Server` | Скрыт как `"webserver"` | ✅ Хорошо |
| `X-Powered-By` | Удалён | ✅ Хорошо |

---

## Общая оценка

Все находки высокой серьёзности из предыдущих проверок были устранены. Текущие находки ограничены двумя проблемами средней серьёзности (#26 общий ключ nonce, #28 пустой ILoggerFactory) и двумя информационными пунктами низкой серьёзности (#25 несоответствие значения по умолчанию класса, #27 переполнение целого в счётчиках). Рекомендуется немедленно уделить внимание находке #28 (пустой синглтон ILoggerFactory), поскольку она молча подавляет диагностическое логирование, связанное с безопасностью, во время операций с nonce. Находка #26 (общий ключ nonce) должна быть устранена для восстановления гарантии уникальности nonce на запрос, требуемой спецификацией CSP.
