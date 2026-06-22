# Corrección de seguridad: Nonce registrado en texto plano (Crítico #2)

**Corregido en:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`  
**Pruebas:** `WebAppExperimental266.Tests/Services/NoncePlaintextLoggingTests.cs`

---

## Qué estaba mal

Se registraba el valor real del nonce CSP en logs (`"Nonce: {nonce}"` y `"Generated Nonce: {CSPNonce}"`).

## Por qué es crítico

El nonce CSP es un secreto por respuesta. Si aparece en logs, cualquier actor con acceso al sistema de logs puede reutilizarlo para inyectar script inline y eludir CSP.

## Qué se corrigió

Se sustituyeron los logs vulnerables por mensajes de estado sin exponer el valor del nonce:
- “Nonce retrieved for request.”
- “Nonce generated successfully.”

## Cómo mantenerlo corregido

1. Nunca registrar el valor del nonce.
2. Revisar logs nuevos en middleware/servicios de nonce.
3. No incluir nonce en métricas, trazas o telemetría.
4. Tratar nonce como secreto por solicitud (`HttpContext.Items` dentro del ciclo de render).

## Pruebas que blindan la corrección

| Prueba | Qué detecta |
|---|---|
| `NonceRefresherService_DoesNotLogNonceValue_OnSuccess` | Reintroducción del nonce en logs del refresher |
| `NonceMiddleware_DoesNotLogNonceValue_InInvokeAsync` | Reintroducción del nonce en logs del middleware |
