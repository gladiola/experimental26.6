# Guía de Autenticación de Certificados de Cliente mTLS (Mutual TLS)

## Resumen

Este proyecto admite **TLS mutuo (mTLS)**, donde servidor y cliente presentan certificados válidos. Esto añade autenticación de doble vía y eleva la seguridad del canal.

## ¿Qué es mTLS?

mTLS amplía TLS estándar con:
1. **Certificado de servidor**: el servidor demuestra su identidad (HTTPS normal)
2. **Certificado de cliente**: el cliente también demuestra su identidad (mTLS)

## Configuración

### 1. Feature Flag

Active mTLS en `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableKeyVault": true,
    "EnableMtls": true
  }
}
```

### 2. Ajustes de mTLS

```json
{
  "MtlsSettings": {
    "RequireClientCertificate": true,
    "AllowCertificateChains": true,
    "AllowSelfSignedCertificates": false,
    "CheckCertificateRevocation": false,
    "ClientCertificateName": "my-client-cert",
    "ValidateClientCertificateIssuer": true
  }
}
```

| Ajuste | Tipo | Predeterminado | Descripción |
|---|---|---|---|
| `RequireClientCertificate` | bool | `true` | Certificado de cliente obligatorio |
| `AllowCertificateChains` | bool | `true` | Permite certificados encadenados (CA) |
| `AllowSelfSignedCertificates` | bool | `false` | Permite autofirmados (solo desarrollo) |
| `CheckCertificateRevocation` | bool | `false` | Verificación online de revocación |
| `ClientCertificateName` | string | null | Nombre del certificado en Key Vault |
| `ValidateClientCertificateIssuer` | bool | `true` | Valida emisor del certificado |

### 3. Certificado del servidor (Azure Key Vault)

```json
{
  "AzureKeyVault": {
    "KeyVaultURL": "https://your-keyvault.vault.azure.net/",
    "KeyVaultSecret": "server-cert-name",
    "KeyVaultPassName": "server-cert-password"
  }
}
```

## Pasos de implementación

1. Cree/importe el certificado de servidor (PFX) en Key Vault.
2. Genere/obtenga certificados de cliente (CA en producción).
3. Configure `FeatureFlags` y `MtlsSettings`.
4. Pruebe con certificado de cliente (`curl`, PowerShell o navegador).

### Prueba con cURL

```bash
curl --cert client.pem --key client.key https://your-app.azurewebsites.net
```

### Prueba con PowerShell

```powershell
$cert = Get-PfxCertificate -FilePath "client.pfx"
Invoke-WebRequest -Uri "https://your-app.azurewebsites.net" -Certificate $cert
```

## Comportamiento por entorno

### Desarrollo
- Certificado de servidor desde Key Vault (si está disponible)
- Certificados de cliente opcionales según configuración
- Puede permitirse autofirmado para pruebas

### Producción
- Certificado de servidor desde Key Vault
- Certificado de cliente requerido si `EnableMtls = true`
- Recomendado: certificados emitidos por CA + revocación habilitada

## Buenas prácticas

### Hacer
- Usar certificados CA en producción
- Guardar certificados y secretos fuera del código fuente
- Habilitar revocación en producción
- Rotar certificados periódicamente

### No hacer
- Usar autofirmados en producción
- Confirmar certificados/secretos en git
- Reutilizar certificados de cliente entre usuarios

## Solución de problemas

- **No se envía certificado de cliente**: verificar instalación y `RequireClientCertificate`.
- **Falla cadena de confianza**: instalar raíz CA o habilitar autofirmado solo para pruebas.
- **No carga certificado de servidor**: revisar permisos de Key Vault y credenciales.

## Referencias

- [Microsoft Docs: Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [Azure Key Vault Integration](https://learn.microsoft.com/en-us/azure/app-service/configure-ssl-certificate-in-code)
