# Guía de Implementación de OCSP (Online Certificate Status Protocol)

## Resumen

El proyecto incluye soporte **plantilla** para validación OCSP. OCSP permite verificar en tiempo real si un certificado fue revocado antes de procesar solicitudes.

## ¿Qué es OCSP?

OCSP es alternativa a CRL:
- Validación en tiempo real
- Consultas por certificado específico
- Respuestas ligeras
- Información de revocación actualizada

## Configuración

### 1. Feature Flag

```json
{
  "FeatureFlags": {
    "EnableOcspValidation": true
  }
}
```

### 2. Ajustes OCSP

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

| Ajuste | Tipo | Predeterminado | Descripción |
|---|---|---|---|
| `EnableOcspValidation` | bool | `false` | Activa/desactiva validación OCSP |
| `OcspServerUrl` | string | `null` | URL del responder OCSP |
| `RequestTimeoutSeconds` | int | `30` | Timeout de consulta |
| `MaxRetryAttempts` | int | `3` | Reintentos en error |
| `CacheDurationMinutes` | int | `60` | Duración de caché de respuestas |
| `ServerUnavailableBehavior` | string | `"Warn"` | `"Fail"`, `"Allow"` o `"Warn"` |

## Estado actual

La implementación en `OcspValidationService.cs` es una **plantilla**. Para producción, debe:
1. Implementar RFC 6960 (crear request, enviar, validar firma, parsear respuesta)
2. Operar un servidor OCSP o usar uno gestionado
3. Integrarlo con mTLS en `OnCertificateValidated`

## Comportamiento cuando OCSP no está disponible

- **Fail**: rechaza solicitudes (máxima seguridad)
- **Allow**: permite solicitudes (máxima disponibilidad)
- **Warn**: permite y registra advertencias (equilibrado)

## Caché

`CacheDurationMinutes` reduce carga y latencia, pero debe equilibrarse con frescura de datos.

## Buenas prácticas

### Hacer
- Usar HTTPS para OCSP
- Validar firma de respuestas OCSP
- Registrar fallos de validación
- Ajustar timeout/reintentos para producción

### No hacer
- Usar HTTP en producción
- Omitir validación de firma
- Mantener caché excesiva (>24h)
- Ignorar silenciosamente caídas del servidor

## Pruebas

```powershell
dotnet test --filter "FullyQualifiedName~Ocsp"
```

## Referencias

- [RFC 6960](https://tools.ietf.org/html/rfc6960)
- [OpenSSL OCSP](https://www.openssl.org/docs/man1.1.1/man1/ocsp.html)
- [BouncyCastle](https://www.bouncycastle.org/csharp/)
