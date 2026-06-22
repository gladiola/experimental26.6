# Исправление безопасности: повторное использование IV в AES-GCM при генерации nonce (Критическая #1)

**Исправлено в:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`,
`Services/NonceCatalogService.cs`  
**Тесты:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`,
`WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Что было не так

Класс `Nonce` использовал **шифрование AES-GCM с фиксированным IV**, который извлекался из Azure Key Vault при
каждом вызове. Повторное использование одного и того же IV с одним и тем же ключом AES-GCM —
критическая криптографическая ошибка:

* Злоумышленник, наблюдающий два шифртекста с одинаковыми IV и ключом, может XOR-операцией
  восстановить XOR двух открытых текстов.
* Ещё хуже: для тегов аутентичности повторное использование IV позволяет подделывать теги,
  что полностью разрушает гарантию целостности AES-GCM.

Помимо криптографической проблемы, само шифрование **не давало пользы** для данного сценария.
CSP nonce должен иметь только два свойства: быть **непредсказуемым** и **уникальным для каждого запроса**.
Эти свойства уже обеспечивает криптографически стойкий генератор случайных чисел
(`RandomNumberGenerator`). Шифрование добавляло сложность без прироста безопасности.

### Код первопричины (до исправления)

```csharp
// Nonce.cs — один и тот же IV извлекался из Key Vault при каждом вызове
using AesGcm aesGcm = new AesGcm(keyBytes, 16);
aesGcm.Encrypt(ivBytes, randomNumber, ciphertext, tag);
```

```csharp
// NonceRefresherService.cs — IV и ключ извлекались один раз и переиспользовались во всех запросах
var fetchIV  = await _azureKeyVaultOperationsService.FetchSecretIVSecret();
var fetchKey = await _azureKeyVaultOperationsService.FetchSecretNonceKeySecret();
Nonce nonce  = new(nonceLogger, fetchIV, fetchKey);
```

---

## Что было исправлено

`Nonce.GenerateSecureNonce()` теперь напрямую вызывает `RandomNumberGenerator.Fill(byte[])` для генерации
16 байт криптографически случайных данных с последующим преобразованием в Base64:

```csharp
public static string GenerateSecureNonce()
{
    byte[] randomBytes = new byte[16];
    RandomNumberGenerator.Fill(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

* Вызовы Key Vault для IV или ключа шифрования больше не требуются и не выполняются.
* AES-GCM и любое другое шифрование больше не используются.
* Конструктор `Nonce` больше не принимает параметры `KeyVaultSecret`.

Дополнительно исправлена ошибка в `NonceCatalogService.GetANonce`: ранее метод использовал
двухшаговый шаблон check-then-lookup (`TryGetValue`, затем индексатор `[]`), который не является
атомарным и мог выбрасывать `KeyNotFoundException`, если другой поток удалял ключ между вызовами.
Исправление использует `TryGetValue` с параметром `out` для атомарного получения значения.

---

## Как сохранить исправление

1. **Никогда не добавляйте IV или ключ из Key Vault для генерации nonce.** Key Vault можно использовать
   для других секретов, но генерация nonce не должна зависеть от фиксированного IV.

2. **Никогда не заменяйте `GenerateSecureNonce` на схему AES-GCM или CBC/CTR**, где IV или счётчик
   повторно используются между запросами.

3. **Оставляйте длину nonce не менее 16 байт (128 бит).** Уменьшение длины повышает вероятность
   коллизий и снижает энтропию CSP.

4. **Не заменяйте `RandomNumberGenerator.Fill` на `new Random()`** или любой другой не-CSPRNG источник.

5. **Сохраняйте в `NonceCatalogService.GetANonce` использование `TryGetValue` с `out`.**
   Двухэтапный шаблон (`TryGetValue` + индексатор) не является потокобезопасным даже с
   `ConcurrentDictionary`.

### Тесты, фиксирующие это исправление

| Тест | Что он предотвращает |
|------|----------------------|
| `Nonce_Constructor_AcceptsOnlyLogger_NoKeyVaultParameters` | Не компилируется, если конструктор снова примет `KeyVaultSecret` IV + key |
| `Nonce_GetNonceAsString_ReturnsNonEmptyBase64` | Падает, если генерация nonce сломана или возвращает не-Base64 |
| `GenerateSecureNonce_Returns16ByteBase64` | Падает, если длина снижена ниже 16 байт |
| `Nonce_SuccessiveGenerations_AreUnique` | Падает, если из-за повторного IV nonce начинает повторяться |
| `Nonce_HasSufficientEntropy` | Падает при неслучайном источнике энтропии |
| `NonceCatalogService_BackingStore_IsConcurrentDictionary` | Падает, если `ConcurrentDictionary` заменён на `Dictionary` |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Падает при возврате TOCTOU-гонки в `GetANonce` |
