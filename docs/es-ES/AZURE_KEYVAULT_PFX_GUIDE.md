# Guía de Certificados PFX en Azure Key Vault

## Fecha: 2024-12-20

## Resumen

Esta guía describe el enfoque correcto para almacenar y recuperar certificados PFX completos (con clave privada) en Azure Key Vault.

## Errores comunes a evitar

### Incorrecto: guardar PFX como secreto Base64

No se recomienda por:
1. Límite de tamaño de secretos
2. Riesgo de corrupción por codificación
3. Pérdida de metadatos de certificado
4. Manejo complejo e innecesario

## Enfoque correcto

### Método recomendado: API de certificados

1. Importar certificado con `Import-AzKeyVaultCertificate`
2. Leer metadatos con `CertificateClient`
3. Obtener PFX con clave privada desde `SecretClient` (`certificate.SecretId`)
4. Construir `X509Certificate2` con los bytes Base64 decodificados

## Implementación recomendada en la app

Reemplazar el código plantilla en:
- `AzureKeyVaultOperations/AzureKeyVaultCertificateOperations.cs`

Puntos clave:
- Preferir `DefaultAzureCredential` en producción (Managed Identity)
- Manejar `CryptographicException` y `RequestFailedException`
- Verificar presencia de clave privada

## Paquetes NuGet requeridos

```xml
<PackageReference Include="Azure.Identity" Version="1.13.1" />
<PackageReference Include="Azure.Security.KeyVault.Certificates" Version="4.7.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.7.0" />
```

## Permisos necesarios en Key Vault

Para la identidad de la aplicación (Service Principal o Managed Identity):

- **Certificates**: Get, List
- **Secrets**: Get, List

Se necesitan ambos porque:
- Certificados: metadatos
- Secretos: clave privada (PFX)

## Rotación

Key Vault admite rotación automática de certificados. La app debe consultar siempre la versión actual salvo que exista un motivo para fijar versión.

## Solución de problemas

- **Certificado no encontrado**: validar nombre y existencia en Key Vault
- **Acceso denegado**: revisar políticas RBAC/permisos de certificado y secreto
- **Sin clave privada**: confirmar uso de `GetSecretAsync` además de `GetCertificateAsync`
- **CryptographicException**: revisar formato/corrupción del PFX

## Checklist de migración

- [ ] Actualizar operaciones de Key Vault con implementación real
- [ ] Importar certificado vía API de certificados
- [ ] Configurar permisos Certificates + Secrets
- [ ] Ajustar `appsettings` y secretos
- [ ] Probar recuperación y validar `HasPrivateKey`
- [ ] Probar integración mTLS

## Resumen final

### Hacer
- Importar PFX como certificado de Key Vault
- Recuperar con `CertificateClient` + `SecretClient`
- Usar Managed Identity en producción

### No hacer
- Guardar PFX como secreto Base64 “manual”
- Omitir permisos de secretos
- Usar secretos de cliente en producción cuando hay Managed Identity
