# Исправление безопасности: жёстко закодированные резервные nonce (Критическая #3)

**Исправлено в:** `Services/OptimizedNonceMiddleware.cs`  
**Тесты:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Что было не так

`OptimizedNonceMiddleware` содержал три жёстко закодированные строки, которые использовались как
резервные значения nonce, когда обычная генерация nonce не срабатывала или ещё не запускалась:

| Место | Жёстко заданное значение |
|-------|---------------------------|
| `InvokeAsync` — первый запрос, каталог пуст | `bootstrap-nonce-placeholder` |
| `InvokeAsync` — генерация вернула пустую строку | `fallback-nonce` |
| `InvokeAsync` — путь обработки исключения | `error-fallback-nonce` |

### Почему это критично

**Nonce безопасен только тогда, когда его нельзя предсказать атакующему.** Жёстко заданные литералы
попадают в систему контроля версий и становятся известны любому, кто имеет доступ к репозиторию
(включая атакующего, получившего исходники или декомпилировавшего бинарный файл).

Особенно опасно то, что эти резервные ветки активируются при **ошибочных состояниях** — именно тех,
которые атакующий чаще всего пытается спровоцировать (например, временная недоступность Key Vault из-за
rate limiting или сетевых сбоев). Когда приложение деградирует до предсказуемого nonce, CSP становится
декоративной: атакующему достаточно внедрить `<script nonce="fallback-nonce">`, и браузер его выполнит.

### Код первопричины (до исправления)

```csharp
// Первый запрос до генерации nonce
existingNonce = "bootstrap-nonce-placeholder";

// Генерация nonce вернула пустую строку
nonce = "fallback-nonce";

// Путь обработки исключения
context.Items["Nonce"] = "error-fallback-nonce";
```

---

## Что было исправлено

Все три резервные ветки теперь вызывают `Nonce.GenerateSecureNonce()` для генерации нового,
непредсказуемого 16-байтного случайного nonce во время выполнения:

```csharp
// ДО (уязвимо):
existingNonce = "bootstrap-nonce-placeholder";
// ПОСЛЕ (безопасно):
existingNonce = Nonce.GenerateSecureNonce();

// ДО (уязвимо):
nonce = "fallback-nonce";
// ПОСЛЕ (безопасно):
nonce = Nonce.GenerateSecureNonce();

// ДО (уязвимо):
context.Items["Nonce"] = "error-fallback-nonce";
// ПОСЛЕ (безопасно):
context.Items["Nonce"] = Nonce.GenerateSecureNonce();
```

`Nonce.GenerateSecureNonce()` использует `RandomNumberGenerator.Fill` (CSPRNG) для генерации 16
криптографически случайных байт с кодированием Base64. Поскольку это статический метод без
зависимости от Key Vault, его безопасно вызывать даже при недоступности Key Vault — именно в том
сценарии, где раньше и появлялся предсказуемый резервный nonce.

---

## Как сохранить исправление

1. **Никогда не добавляйте жёстко закодированный литерал nonce** нигде в кодовой базе,
   независимо от контекста (резервный путь, тест, placeholder, пример в комментарии и т.д.).

2. **Каждая ветка, устанавливающая `context.Items["Nonce"]`, должна использовать
   криптографически случайное значение.** Используйте `Nonce.GenerateSecureNonce()` или
   `RandomNumberGenerator.GetBytes(16)` + Base64.

3. **Не кэшируйте один nonce между запросами.** Каждый запрос должен получать собственный nonce.

4. **Пути ошибок наиболее опасны.** Если генерация nonce не удалась, ответ всё равно должен получить
   случайный nonce, а не предсказуемое резервное значение.

5. **Проверяйте любые будущие изменения `OptimizedNonceMiddleware`**, особенно три ветки,
   где устанавливается nonce: ветка пустого каталога, ветка пустого результата генерации
   и ветка обработчика исключений.

### Тесты, фиксирующие это исправление

| Тест | Что он предотвращает |
|------|----------------------|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Падает, если в первой ветке снова появится `bootstrap-nonce-placeholder` |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Падает, если в ветке пустого результата снова появится `fallback-nonce` |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Падает, если в обработчике исключений снова появится `error-fallback-nonce` |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Падает, если любой резервный путь выдаёт одинаковый nonce дважды за 50 последовательных вызовов (что происходит с любым жёстко заданным литералом) |
