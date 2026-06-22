# Corrección de seguridad: Nonces de respaldo hardcodeados (Crítico #3)

**Corregido en:** `Services/OptimizedNonceMiddleware.cs`  
**Pruebas:** `WebAppExperimental266.Tests/Services/NonceHardcodedFallbackTests.cs`

---

## Qué estaba mal

`OptimizedNonceMiddleware` tenía nonces literales hardcodeados para rutas de error/arranque:
- `"bootstrap-nonce-placeholder"`
- `"fallback-nonce"`
- `"error-fallback-nonce"`

## Por qué es crítico

Un nonce hardcodeado es predecible y conocido en el código fuente. Justo cuando hay fallos (por ejemplo, dependencias no disponibles), la app degradaba a un nonce explotable.

## Qué se corrigió

Todos los caminos de respaldo ahora generan nonce aleatorio con `Nonce.GenerateSecureNonce()` (CSPRNG + Base64, 16 bytes).

Esto elimina nonces predecibles incluso en rutas de excepción.

## Cómo mantenerlo corregido

1. Nunca introducir nonces literales hardcodeados.
2. Toda asignación a `context.Items["Nonce"]` debe ser aleatoria criptográficamente.
3. No reutilizar un nonce entre solicitudes.
4. Revisar especialmente ramas de error y fallback.

## Pruebas que blindan la corrección

| Prueba | Qué detecta |
|---|---|
| `OptimizedNonceMiddleware_WhenCatalogEmpty_UsesFreshRandomNonce` | Reintroducción de fallback fijo inicial |
| `OptimizedNonceMiddleware_WhenNonceGenerationReturnsEmpty_UsesFreshRandomNonce` | Reintroducción de fallback fijo por vacío |
| `OptimizedNonceMiddleware_WhenNonceGenerationThrows_UsesFreshRandomNonce` | Reintroducción de fallback fijo por excepción |
| `OptimizedNonceMiddleware_ErrorFallbackNonces_AreUnique` | Reutilización/constancia de nonce de respaldo |
