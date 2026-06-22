# Corrección de seguridad: Reutilización de IV AES-GCM en generación de nonce (Crítico #1)

**Corregido en:** `Models/Main_Objects/Nonce.cs`, `Services/NonceRefresherService.cs`, `Services/NonceCatalogService.cs`  
**Pruebas:** `WebAppExperimental266.Tests/Services/NonceSecurityTests.cs`, `WebAppExperimental266.Tests/Services/NonceCatalogServiceTests.cs`

---

## Qué estaba mal

La clase `Nonce` usaba AES-GCM con un IV fijo reutilizado. Reutilizar IV con la misma clave en GCM es un fallo criptográfico crítico: rompe confidencialidad e integridad.

Además, cifrar el nonce CSP no aporta seguridad en este caso. El nonce solo necesita ser:
- impredecible
- único por solicitud

## Qué se corrigió

`Nonce.GenerateSecureNonce()` ahora usa `RandomNumberGenerator.Fill(byte[])` para generar 16 bytes aleatorios y luego Base64.

Consecuencias:
- Sin dependencias de IV/clave desde Key Vault para nonce
- Sin AES-GCM para este flujo
- Constructor de `Nonce` simplificado

También se corrigió una condición de carrera en `NonceCatalogService.GetANonce`: se reemplazó patrón no atómico por `TryGetValue` con `out`.

## Cómo mantenerlo corregido

1. No volver a introducir IV/clave de Key Vault en generación de nonce.
2. No sustituir por esquemas con IV/contador reutilizado.
3. Mantener nonce de al menos 16 bytes (128 bits).
4. No reemplazar CSPRNG por `Random()`.
5. Mantener acceso seguro en `NonceCatalogService` con `TryGetValue(out ...)`.

## Pruebas que blindan la corrección

| Prueba | Qué detecta |
|---|---|
| `GenerateSecureNonce_Returns16ByteBase64` | Reducción de longitud/entropía |
| `Nonce_SuccessiveGenerations_AreUnique` | Repetición por reutilización |
| `Nonce_HasSufficientEntropy` | Fuente no criptográfica |
| `NonceCatalogService_ShouldHandleConcurrentAccess_WithoutExceptions` | Reintroducción de carrera |
