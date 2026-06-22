# Revisión de Seguridad — WebAppExperimental266

**Fecha:** 2026-05-06
**Alcance:** Análisis estático completo del código fuente (seguimiento a la revisión del 2026-05-05)
**Revisor:** Revisión de Seguridad Automatizada

---

## Resumen Ejecutivo

Esta revisión de seguimiento confirma que las 19 vulnerabilidades identificadas en la revisión de seguridad del 2026-05-05 han sido corregidas. La revisión también identifica 5 nuevos hallazgos o residuales descubiertos durante esta sesión. La postura de seguridad general de la aplicación ha mejorado significativamente desde la revisión anterior.

---

## Estado de los Hallazgos Anteriores (2026-05-05)

Los 19 hallazgos anteriores están **confirmados como corregidos**:

| # | Hallazgo | Severidad | Estado |
|---|----------|-----------|--------|
| 1 | Reutilización de IV AES-GCM en la generación de nonce | 🔴 Crítico | ✅ Corregido |
| 2 | Nonce registrado en texto plano | 🔴 Crítico | ✅ Corregido |
| 3 | Cadenas de nonce de respaldo codificadas de forma fija | 🔴 Crítico | ✅ Corregido |
| 4 | Diccionario de nonce global no seguro para subprocesos | 🟠 Alto | ✅ Corregido |
| 5 | Validación del emisor mTLS comentada | 🟠 Alto | ✅ Corregido |
| 6 | Verificación de revocación mTLS desactivada por defecto | 🟠 Alto | ✅ Corregido |
| 7 | OCSP siempre devuelve válido (stub) | 🟠 Alto | ✅ Corregido |
| 8 | Autenticación/autorización desactivada por defecto en la configuración | 🟠 Alto | ✅ Corregido |
| 9 | Encabezados de seguridad aplicados demasiado tarde en el pipeline | 🟠 Alto | ✅ Corregido |
| 10 | Cookie de sesión sin `Secure` + `SameSite` | 🟡 Medio | ✅ Corregido |
| 11 | Encabezado `Set-Cookie` global malformado | 🟡 Medio | ✅ Corregido |
| 12 | `Content-Type` forzado a `text/html` en todos lados | 🟡 Medio | ✅ Corregido |
| 13 | `AllowedHosts` establecido como comodín | 🟡 Medio | ✅ Corregido |
| 14 | Nonce no aplicado a las etiquetas `<script>` en el diseño | 🟡 Medio | ✅ Corregido |
| 15 | Encabezado `Referrer-Policy` faltante | 🟡 Medio | ✅ Corregido |
| 16 | PII registrada en texto plano | 🔵 Bajo | ✅ Corregido |
| 17 | Cadena de conexión parcial en los registros | 🔵 Bajo | ✅ Corregido |
| 18 | Las operaciones de Key Vault son stubs | 🔵 Bajo | ✅ Corregido |
| 19 | `X-XSS-Protection: 1; mode=block` obsoleto | 🔵 Bajo | ✅ Corregido |

---

## Hallazgos Nuevos / Residuales

| # | Área | Severidad |
|---|------|-----------|
| 20 | NonceRefresherService retiene dependencias del constructor de Key Vault no utilizadas | 🟠 Alto |
| 21 | El caché interno de OcspValidationService usa un Dictionary no seguro para subprocesos | 🟡 Medio |
| 22 | El stub de validación OCSP sigue presente — falla cerrado pero no implementado | 🔵 Bajo |
| 23 | mTLS con AllowedIssuers vacío rechaza todos los certificados (fail-closed, no documentado) | 🔵 Bajo |
| 24 | OcspSettings.ServerUnavailableBehavior por defecto es "Warn" (permite paso a través en error) | 🔵 Bajo |

---

## Hallazgos Detallados

### ✅ Correcciones Confirmadas de 2026-05-05

#### 1. Reutilización de IV AES-GCM — Corregido

**Archivo:** `Models/Main_Objects/Nonce.cs`

La generación de nonce basada en AES-GCM ha sido completamente reemplazada. `Nonce.GenerateSecureNonce()` ahora llama a `RandomNumberGenerator.Fill(randomBytes)` en 16 bytes aleatorios y devuelve una cadena Base64. Sin dependencia de Key Vault, sin IV, sin cifrado — exactamente el enfoque correcto para un nonce CSP.

---

#### 2. Los Valores de Nonce Ya No Se Registran — Corregido

**Archivos:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Ambos archivos ahora solo registran mensajes de estado (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) y nunca el valor del nonce en sí.

---

#### 3. Nonces de Respaldo Codificados de Forma Fija Eliminados — Corregido

**Archivo:** `Services/OptimizedNonceMiddleware.cs`

Las tres cadenas literales codificadas de forma fija (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) han sido reemplazadas con llamadas a `Nonce.GenerateSecureNonce()` tanto en las rutas normales como en las de respaldo de excepción.

---

#### 4. Diccionario de Nonce Seguro para Subprocesos — Corregido

**Archivo:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` ha sido reemplazado con `ConcurrentDictionary<string, Nonce>`. `GetANonce` ahora usa una sola llamada atómica `TryGetValue` en lugar de una verificación en dos pasos de comprobar y luego buscar.

---

#### 5. Validación del Emisor mTLS Ahora Funcional — Corregido

**Archivo:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

El bloque de validación del emisor comentado ha sido reemplazado por una llamada a `mtlsSettings.IsIssuerAllowed(issuer)`, que realiza una coincidencia de subcadena sin distinción de mayúsculas/minúsculas contra `AllowedIssuers`. Cuando la lista está vacía (no configurada), el método devuelve `false`, rechazando todos los certificados (fail-closed).

---

#### 6. La Verificación de Revocación mTLS Por Defecto Está Habilitada — Corregido

**Archivo:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` ahora por defecto es `true`. El `appsettings.template.json` también especifica `"CheckCertificateRevocation": true`.

---

#### 7. El Stub OCSP Ahora Falla Cerrado — Corregido

**Archivo:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` ahora devuelve `IsValid = false` con `OcspStatus.Error` y registra un error, en lugar de devolver silenciosamente `IsValid = true`. Habilitar OCSP en la configuración ahora rechazará todos los certificados hasta que se proporcione una implementación real, en lugar de aceptarlos silenciosamente.

---

#### 8. Autenticación y Autorización Habilitadas Por Defecto — Corregido

**Archivo:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` y `EnableAuthorization` ahora por defecto son `true` en la clase `FeatureFlags`. `appsettings.json` también establece ambos en `true`.

---

#### 9. Encabezados de Seguridad Aplicados Antes del Enrutamiento — Corregido

**Archivo:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` y `UseStandardSecurityHeaders` ahora se llaman antes de `UseRouting`, `UseAuthentication` y `UseAuthorization`. Todas las respuestas, incluidos los cortocircuitos 401/403, reciben los encabezados de seguridad.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce en el Diseño, Referrer-Policy — Corregido

**Archivos:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- La cookie de sesión ahora establece `CookieSecurePolicy.Always` y `SameSiteMode.Strict`.
- El encabezado `Set-Cookie` sin nombre malformado ha sido eliminado.
- La anulación global de `Content-Type: text/html` ha sido eliminada.
- `AllowedHosts` en `appsettings.json` es ahora `"localhost;127.0.0.1"`; la plantilla usa `"{{YOUR_HOSTNAME}}"`.
- Las tres etiquetas `<script>` en `_Layout.cshtml` ahora incluyen `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` ahora es agregado por `UseStandardSecurityHeaders`.

---

#### 16–19. Registro de PII, Registro de Cadena de Conexión, Stubs de Key Vault, X-XSS-Protection — Corregido

**Archivos:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Toda la PII (OID, correo electrónico, nombre, SID, roles) ahora se hashea con HMAC-SHA256 mediante `LoggingHelper.HashPii()` antes de escribirse en los registros. Se puede proporcionar una clave HMAC estable a través de `Logging:PiiHmacKey` en la configuración; se usa una clave aleatoria por proceso cuando no está configurada.
- La declaración de registro de Cosmos DB ahora solo confirma si una cadena de conexión está presente (`!string.IsNullOrEmpty`), no su contenido.
- `AzureKeyVaultCertificateOperations` ahora lanza `InvalidOperationException` al inicio cuando el certificado es nulo, en lugar de devolver silenciosamente valores ficticios.
- `X-XSS-Protection` ahora está establecido en `"0"` (deshabilita el auditor XSS obsoleto), coherente con la guía moderna de navegadores.

---

## 🟠 Alto

### 20. NonceRefresherService Retiene Dependencias del Constructor de Key Vault No Utilizadas

**Archivo:** `Services/NonceRefresherService.cs`

`NonceRefresherService` todavía declara parámetros del constructor para `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` e `IAzureKeyVaultOperationsService`. Dado que la generación de nonce se simplificó para usar `RandomNumberGenerator` directamente, ninguna de estas dependencias se utiliza.

**Riesgo:** Cuando `EnableNonceServices = true` y `EnableKeyVault = false` (el predeterminado), estos servicios no están registrados en el contenedor DI, lo que provoca una `InvalidOperationException` en tiempo de ejecución cuando el servicio de nonce se resuelve por primera vez. Esto es efectivamente una condición de denegación de servicio desencadenada por la configuración predeterminada. La clase `FeatureFlags` por defecto establece `EnableNonceServices = true`, por lo que cualquier entorno que dependa únicamente de los valores predeterminados de clase (sin anulaciones de `appsettings.json`) no podría iniciarse.

**Recomendación:** Elimine los cuatro parámetros del constructor no utilizados y sus campos privados correspondientes de `NonceRefresherService`. El servicio solo requiere `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`.

---

## 🟡 Medio

### 21. El Caché Interno de OcspValidationService Usa un Dictionary No Seguro para Subprocesos

**Archivo:** `Services/OcspValidationService.cs` (línea 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` no es seguro para subprocesos para lecturas y escrituras concurrentes. Si `OcspValidationService` está registrado como singleton (o si la misma instancia se comparte entre solicitudes por cualquier otro mecanismo), las validaciones OCSP concurrentes podrían corromper el caché, causando entradas perdidas, excepciones lanzadas o datos obsoletos que se devuelven.

**Recomendación:** Reemplace `Dictionary<string, CachedOcspResponse>` con `ConcurrentDictionary<string, CachedOcspResponse>`. Actualice la llamada `_cache.Remove` (línea 103) a `_cache.TryRemove`.

---

## 🔵 Bajo / Informativo

### 22. Stub de Validación OCSP — Falla Cerrado pero No Implementado

**Archivo:** `Services/OcspValidationService.cs` (líneas 157–173)

`PerformOcspValidationAsync` sigue siendo un stub. La corrección del hallazgo #7 cambió correctamente el comportamiento de "siempre válido" a "siempre inválido (fail-closed)". Sin embargo, el método todavía no es una implementación OCSP real. Mientras `EnableOcspValidation = false` (el predeterminado), esto no tiene impacto en producción. Antes de habilitar OCSP en cualquier entorno, se debe implementar un cliente OCSP de calidad de producción.

---

### 23. mTLS con AllowedIssuers Vacío Rechaza Todos los Certificados de Cliente

**Archivo:** `Models/Settings/MtlsSettings.cs`

Cuando `ValidateClientCertificateIssuer = true` (el predeterminado) y `AllowedIssuers` está vacío (también el predeterminado cuando no está configurado), `IsIssuerAllowed()` devuelve `false`, lo que hace que todos los certificados de cliente sean rechazados. Este es el comportamiento correcto de fail-closed, pero no está documentado de manera prominente. Los operadores que habiliten mTLS sin leer detenidamente la plantilla pueden encontrar que todas las conexiones de cliente son rechazadas sin una explicación obvia.

**Recomendación:** Agregue un mensaje de registro de advertencia al inicio cuando `ValidateClientCertificateIssuer = true` y `AllowedIssuers` esté vacío.

---

### 24. OcspSettings.ServerUnavailableBehavior Por Defecto Es "Warn"

**Archivo:** `appsettings.template.json` (línea 134), `Services/OcspValidationService.cs`

La configuración `ServerUnavailableBehavior` por defecto es `"Warn"` en la plantilla, lo que permite que las solicitudes pasen cuando no se puede alcanzar el servidor OCSP. Para entornos de alta seguridad, esto debería ser `"Fail"` para que las interrupciones del servidor OCSP no degraden silenciosamente la verificación de revocación de certificados.

**Recomendación:** Documente las tres opciones (`Fail`, `Allow`, `Warn`) claramente en la plantilla y considere cambiar el valor predeterminado a `"Fail"` para cumplir con el principio de mínimo privilegio.

---

## Evaluación de Encabezados de Seguridad (Estado Actual)

Los siguientes encabezados ahora se aplican mediante `UseStandardSecurityHeaders`:

| Encabezado | Valor | Evaluación |
|------------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bueno |
| `X-XSS-Protection` | `0` | ✅ Bueno (deshabilita el auditor obsoleto) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bueno |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bueno |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bueno |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bueno |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bueno |
| `Permissions-Policy` | geolocalización, cámara, micrófono, interest-cohort deshabilitados | ✅ Bueno |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bueno |
| `Content-Security-Policy` | Basado en nonce, aplicado cuando CSP está habilitado | ✅ Bueno |
| `Server` | Enmascarado a `"webserver"` | ✅ Bueno |
| `X-Powered-By` | Eliminado | ✅ Bueno |

---

## Evaluación General

La aplicación ha abordado todas las vulnerabilidades de severidad crítica y alta de la revisión anterior. Los hallazgos actuales se limitan a un problema de configuración/DI de alta severidad (hallazgo #20) y elementos informativos de menor severidad. La postura de seguridad ha mejorado sustancialmente. Se recomienda acción inmediata para el hallazgo #20 (dependencias DI no utilizadas en NonceRefresherService), ya que puede impedir que la aplicación se inicie con la configuración predeterminada.
