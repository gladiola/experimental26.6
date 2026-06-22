# Revisión de Seguridad — WebAppExperimental266

**Fecha:** 2026-05-07
**Alcance:** Análisis estático completo del código base (seguimiento de la revisión del 2026-05-06)
**Revisor:** Revisión de Seguridad Automatizada

---

## Resumen Ejecutivo

Esta revisión de seguimiento confirma que 3 de las 5 vulnerabilidades identificadas en la revisión de seguridad del 2026-05-06 han sido completamente remediadas, con 1 que permanece parcialmente remediada. La revisión también identifica 4 nuevos hallazgos. La postura de seguridad general de la aplicación continúa mejorando.

---

## Estado de Hallazgos Previos (2026-05-06)

| # | Hallazgo | Gravedad | Estado |
|---|---------|----------|--------|
| 20 | NonceRefresherService retiene dependencias de constructor de Key Vault no utilizadas | 🟠 Alta | ✅ Corregido |
| 21 | La caché interna de OcspValidationService usa Dictionary no seguro para subprocesos | 🟡 Media | ✅ Corregido |
| 22 | El stub de validación OCSP aún presente — falla cerrado pero no implementado | 🔵 Baja | ⚠️ Aceptado (por diseño) |
| 23 | mTLS con AllowedIssuers vacío rechaza todos los certificados (fail-closed, sin documentar) | 🔵 Baja | ✅ Corregido |
| 24 | OcspSettings.ServerUnavailableBehavior tiene por defecto "Warn" (permite paso en caso de error) | 🔵 Baja | ⚠️ Parcialmente corregido |

---

## Estado Detallado de Hallazgos Previos

### ✅ 20. NonceRefresherService Dependencias DI No Utilizadas — Corregido

**Archivo:** `Services/NonceRefresherService.cs`

El constructor de `NonceRefresherService` ahora solo declara `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`. Las cuatro dependencias previamente no utilizadas (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) han sido eliminadas. Esto resuelve el riesgo de denegación de servicio que impedía que la aplicación se iniciara cuando `EnableKeyVault = false` (el valor predeterminado) y `EnableNonceServices = true` (el valor predeterminado).

---

### ✅ 21. Caché No Segura para Subprocesos de OcspValidationService — Corregido

**Archivo:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` ha sido reemplazado por `ConcurrentDictionary<string, CachedOcspResponse>`. La llamada `_cache.Remove` ha sido actualizada a `_cache.TryRemove`. La caché ahora es segura para el acceso concurrente.

---

### ⚠️ 22. Stub de Validación OCSP — Aceptado (Por Diseño)

**Archivo:** `Services/OcspValidationService.cs`

El stub permanece presente pero falla correctamente de forma cerrada. Como `EnableOcspValidation` tiene como valor predeterminado `false`, esto no tiene impacto en producción. Esto se acepta como un hallazgo informativo pendiente de una implementación completa de OCSP.

---

### ✅ 23. mTLS AllowedIssuers Vacío — Corregido

**Archivo:** `Extensions/ServiceCollectionExtensions.cs`

Ahora se registra una advertencia de inicio cuando `ValidateClientCertificateIssuer = true` y `AllowedIssuers` está vacío:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Esto proporciona orientación clara a los operadores que encuentran el comportamiento fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Parcialmente Corregido

**Archivos:** `appsettings.template.json` (corregido), `Models/Settings/OcspSettings.cs` (aún no corregido)

La plantilla ahora especifica correctamente `"ServerUnavailableBehavior": "Fail"`. Sin embargo, el valor predeterminado de la clase C# en `OcspSettings.cs` (línea 39) permanece como `"Warn"`. Si un operador habilita OCSP y omite `ServerUnavailableBehavior` de su archivo de configuración, el valor predeterminado de la clase `"Warn"` se aplica silenciosamente, permitiendo el paso en interrupciones del servidor OCSP. El valor predeterminado de la clase debe cambiarse para coincidir con la recomendación de la plantilla.

---

## Nuevos Hallazgos

| # | Área | Gravedad |
|---|------|----------|
| 25 | El valor predeterminado de la clase OcspSettings ("Warn") difiere de la plantilla ("Fail") | 🔵 Baja |
| 26 | La clave nonce compartida única de NonceCatalogService permite colisión de nonce entre solicitudes | 🟡 Media |
| 27 | Los contadores estáticos de OptimizedNonceMiddleware usan enteros de 32 bits con signo (riesgo de desbordamiento) | 🔵 Baja |
| 28 | Program.cs registra un singleton ILoggerFactory vacío que eclipsa el logger del framework | 🟡 Media |

---

## 🟡 Media

### 26. La Clave Nonce Compartida de NonceCatalogService Permite Colisión de Nonce Entre Solicitudes

**Archivos:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

El catálogo de nonces almacena todos los nonces bajo una única clave compartida `"CSPNonce"`. Bajo carga concurrente, es posible la siguiente condición de carrera:

1. La solicitud A llama a `RefreshNonceAsync()` — el nonce A1 se almacena como `_nonceCollection["CSPNonce"]`.
2. La solicitud B llama a `RefreshNonceAsync()` — el nonce B1 sobrescribe `_nonceCollection["CSPNonce"]`.
3. La solicitud A llama a `GetANonce("CSPNonce")` — recibe B1, no A1.
4. El encabezado CSP y el nonce de diseño de la solicitud A contienen ambos B1.
5. La solicitud B también contiene B1.

Dos respuestas concurrentes comparten el mismo nonce. Si bien ambos valores siguen siendo criptográficamente aleatorios e impredecibles (sin cadena codificada de forma fija), el mismo valor de nonce aparece en múltiples respuestas simultáneas, debilitando la garantía de unicidad por solicitud requerida por la especificación CSP. Un atacante que puede observar el nonce de una respuesta tiene un nonce válido para al menos otra respuesta concurrente.

**Recomendación:** Genere el nonce directamente dentro del middleware por solicitud (por ejemplo, `Nonce.GenerateSecureNonce()`) y almacénelo solo en `HttpContext.Items["Nonce"]`, evitando el catálogo compartido para los nonces por solicitud. El catálogo compartido solo sería necesario si un nonce debe compartirse entre capas de middleware dentro de una sola solicitud, lo cual `HttpContext.Items` ya maneja de forma nativa.

---

### 28. Program.cs Registra un Singleton ILoggerFactory Vacío

**Archivo:** `Program.cs` (línea 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core registra automáticamente un `ILoggerFactory` completamente configurado (con todos los proveedores de registro de la configuración de `builder.Logging`) durante `WebApplication.CreateBuilder`. Este registro explícito de `AddSingleton` agrega una segunda instancia de `LoggerFactory` no configurada sin proveedores. Dado que `GetRequiredService<ILoggerFactory>()` devuelve la implementación registrada más recientemente, los servicios que reciben `ILoggerFactory` a través de la inyección de dependencias (como `NonceRefresherService`) usarán esta fábrica vacía y no producirán ninguna salida de registro a través de `_loggerFactory.CreateLogger<T>()`.

**Riesgo:** Registro silencioso en `NonceRefresherService` — los éxitos y fallos de la generación de nonces no se emiten a ningún receptor de registro configurado. Esto reduce la observabilidad de la aplicación durante las operaciones sensibles a la seguridad sin afectar la funcionalidad.

**Recomendación:** Elimine el registro explícito de `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. El `ILoggerFactory` configurado del framework (con consola y cualquier otro proveedor) será resuelto correctamente por los servicios que dependan de él.

---

## 🔵 Baja / Informativo

### 25. El Valor Predeterminado de la Clase OcspSettings Difiere de la Plantilla

**Archivo:** `Models/Settings/OcspSettings.cs` (línea 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

La plantilla (`appsettings.template.json`) especifica `"ServerUnavailableBehavior": "Fail"`, pero el valor predeterminado de la clase C# es `"Warn"`. Si `ServerUnavailableBehavior` está ausente del archivo de configuración activo, el valor predeterminado de la clase se aplica silenciosamente en lugar de la recomendación de la plantilla. Esto es un residuo del hallazgo #24.

**Recomendación:** Cambie el valor predeterminado de la clase de `"Warn"` a `"Fail"` para alinearlo con la plantilla y el principio de mínimo privilegio.

---

### 27. Los Contadores Estáticos de OptimizedNonceMiddleware Pueden Desbordarse

**Archivo:** `Services/OptimizedNonceMiddleware.cs` (líneas 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Estos contadores de 32 bits con signo se incrementan atómicamente a través de `Interlocked.Increment`. Después de aproximadamente 2.100 millones de incrementos, se envolverán a `int.MinValue` (−2.147.483.648), lo que hará que el cálculo de eficiencia `(total - generated) * 100.0 / total` produzca resultados incorrectos o sin sentido. A 1.000 solicitudes por segundo, el desbordamiento ocurre después de aproximadamente 24,8 días de operación continua.

**Recomendación:** Cambie los tipos de campo de los contadores de `int` a `long` y use la sobrecarga `long` de `Interlocked.Increment` para evitar el desbordamiento.

---

## Evaluación de Encabezados de Seguridad (Estado Actual)

Los siguientes encabezados se aplican a través de `UseStandardSecurityHeaders` — sin cambios respecto a la revisión anterior:

| Encabezado | Valor | Evaluación |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bien |
| `X-XSS-Protection` | `0` | ✅ Bien (deshabilita el auditor obsoleto) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bien |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bien |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bien |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bien |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bien |
| `Permissions-Policy` | geolocalización, cámara, micrófono, interest-cohort deshabilitados | ✅ Bien |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bien |
| `Content-Security-Policy` | Basado en nonce, aplicado cuando CSP está habilitado | ✅ Bien |
| `Server` | Enmascarado a `"webserver"` | ✅ Bien |
| `X-Powered-By` | Eliminado | ✅ Bien |

---

## Evaluación General

Todos los hallazgos de alta gravedad de revisiones anteriores han sido remediados. Los hallazgos actuales se limitan a dos problemas de gravedad media (#26 clave nonce compartida, #28 ILoggerFactory vacío) y dos elementos informativos de baja gravedad (#25 discrepancia de valor predeterminado de clase, #27 desbordamiento de enteros en contadores). Se recomienda atención inmediata para el hallazgo #28 (singleton ILoggerFactory vacío) ya que suprime silenciosamente el registro de diagnóstico relevante para la seguridad durante las operaciones de nonce. El hallazgo #26 (clave nonce compartida) debe abordarse para restaurar la garantía de unicidad de nonce por solicitud requerida por la especificación CSP.
