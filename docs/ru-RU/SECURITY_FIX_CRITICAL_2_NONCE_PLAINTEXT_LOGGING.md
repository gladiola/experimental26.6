# Исправление безопасности: значения nonce записывались в журнал в открытом виде (Критическая #2)

**Исправлено в:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Тесты:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Что было не так

В двух местах фактическое значение CSP nonce записывалось в журнал дословно:

**`Services/NonceMiddleware.cs` (строка 31):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Info,
    $"Nonce: {nonce}");
```

**`Services/NonceRefresherService.cs` (строка 82):**
```csharp
LoggingHelper.LogDataProcessingStatusServiceWork(_logger, caller, "", DataProcessingStatus.Success,
    $"Generated Nonce: {CSPNonce}");
```

### Почему это критично

CSP nonce — это *единственный* механизм, предотвращающий инъекцию inline-скриптов при включённой CSP.
Его безопасность полностью зависит от того, что значение **секретно в пределах жизненного цикла одного ответа**.

Логи приложения в облачной/корпоративной среде обычно доступны:
* операционным командам;
* системам агрегации логов (например, Azure Monitor, Splunk, ELK);
* любой учётной записи с правами чтения целевого хранилища логов.

Любой, кто видит строку вида `Nonce: <value>`, может вставить inline-скрипт с этим nonce
и обойти CSP. Даже если nonce ротируется на каждый запрос, атакующий с доступом к «живым» логам
может атаковать в рамках того же окна запроса.

---

## Что было исправлено

Обе записи в логах заменены на сообщения, подтверждающие *статус* генерации nonce,
без раскрытия самого значения:

**`NonceMiddleware.cs`:**
```csharp
// ДО (уязвимо):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Nonce: {nonce}");

// ПОСЛЕ (безопасно):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce retrieved for request.");
```

**`NonceRefresherService.cs`:**
```csharp
// ДО (уязвимо):
LoggingHelper.LogDataProcessingStatusServiceWork(..., $"Generated Nonce: {CSPNonce}");

// ПОСЛЕ (безопасно):
LoggingHelper.LogDataProcessingStatusServiceWork(..., "Nonce generated successfully.");
```

---

## Как сохранить исправление

1. **Никогда не логируйте значение nonce.** Можно логировать факт генерации/получения nonce
   (успех/ошибка), но не саму строку nonce ни в одном параметре журнала, структурированном поле
   или строковой интерполяции.

2. **Проверяйте любые новые log-вызовы в коде, связанном с nonce** (`NonceMiddleware`,
   `OptimizedNonceMiddleware`, `NonceRefresherService`, `NonceCatalogService`), чтобы убедиться,
   что значение nonce не попадает в журнал.

3. **Не передавайте nonce в телеметрию, метрики или распределённые трассировки** по тем же причинам.
   Атрибуты трассировок и теги спанов часто уходят в бэкенды логирования.

4. **Nonce должен рассматриваться как секрет уровня одного запроса.** Его можно хранить в
   `HttpContext.Items` для использования внутри конвейера рендеринга одного запроса,
   но он не должен покидать процесс по наблюдаемым каналам, кроме HTTP-заголовка ответа
   и атрибута `nonce="..."` в защищаемом HTML.

### Тесты, фиксирующие это исправление

| Тест | Что он предотвращает |
|------|----------------------|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Падает, если строка nonce снова появляется в любом сообщении лога в `NonceRefresherService` |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Падает, если строка nonce снова появляется в любом сообщении лога в `NonceMiddleware` |
