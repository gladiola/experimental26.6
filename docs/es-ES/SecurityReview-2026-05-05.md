# Revisión de Seguridad — WebAppExperimental266

**Fecha:** 2026-05-05  
**Alcance:** análisis estático completo del código

---

## Tabla resumen

| # | Área | Severidad |
|---|---|---|
| 1 | Reutilización de IV AES-GCM en nonce | 🔴 Crítica ✅ |
| 2 | Nonce registrado en texto plano | 🔴 Crítica ✅ |
| 3 | Nonces de respaldo hardcodeados | 🔴 Crítica ✅ |
| 4 | Diccionario global de nonce no seguro en concurrencia | 🟠 Alta |
| 5 | Validación de emisor mTLS comentada | 🟠 Alta |
| 6 | Revocación mTLS desactivada por defecto | 🟠 Alta |
| 7 | OCSP siempre válido (stub) | 🟠 Alta |
| 8 | Auth/AuthZ desactivadas por defecto en configuración | 🟠 Alta |
| 9 | Encabezados de seguridad aplicados tarde en pipeline | 🟠 Alta |
| 10 | Cookie de sesión sin Secure + SameSite | 🟡 Media |
| 11 | Encabezado global Set-Cookie malformado | 🟡 Media |
| 12 | Content-Type forzado a text/html globalmente | 🟡 Media |
| 13 | AllowedHosts con comodín | 🟡 Media |
| 14 | Nonce no aplicado en `<script>` del layout | 🟡 Media |
| 15 | Falta Referrer-Policy | 🟡 Media |
| 16 | PII en logs en texto plano | 🔵 Baja |
| 17 | Fragmento de connection string en logs | 🔵 Baja |
| 18 | Operaciones de Key Vault en modo stub | 🔵 Baja |
| 19 | Cabecera X-XSS-Protection deprecada | 🔵 Baja |

---

## Hallazgos críticos (estado)

### 1) Reutilización de IV AES-GCM — ✅ corregido
Nonce criptográficamente inseguro por IV fijo reutilizado. Corregido moviendo la generación a CSPRNG directo.

### 2) Nonce en logs — ✅ corregido
Se eliminó el registro del valor real de nonce y se dejaron mensajes de estado.

### 3) Fallback nonce hardcodeado — ✅ corregido
Se reemplazaron literales por generación aleatoria en rutas de fallback/errores.

---

## Hallazgos altos/medios/bajos

Los demás hallazgos del reporte original cubren endurecimiento de concurrencia, mTLS/OCSP, configuración segura por defecto, orden de middleware, cookies y encabezados HTTP, y saneamiento de logs. Consulte los documentos de corrección por criticidad en esta misma carpeta para detalles de mitigación.
