# Guía de Optimización de Generación de Nonce

## Problema

Generar nonce en **cada solicitud HTTP** (incluyendo estáticos y health checks) provoca:
- Más llamadas a Key Vault
- Más operaciones criptográficas
- Mayor costo y latencia

## Objetivo

Generar nonce solo para respuestas HTML que realmente necesitan CSP.

## Estrategias

### Opción 1: Filtrado por ruta (simple)
No generar nonce para `/css`, `/js`, `/lib`, `/images`, `/api` ni archivos con extensión.

### Opción 2: Generación por respuesta (recomendada)
Crear nonce en `OnStarting` si `Content-Type` contiene `text/html`.

### Opción 3: Generación perezosa (más eficiente)
Generar nonce bajo demanda con bloqueo y TTL corto.

## Recomendación práctica

Usar **filtrado por ruta + caché corta** para reducir generación innecesaria y mantener seguridad en páginas HTML.

## Impacto esperado

Ejemplo de carga (1000 req/min):
- Antes: 1000 nonces, 2000 llamadas KV
- Después: 100 nonces, 200 llamadas KV

Resultado: ~**90% de reducción** en generación y llamadas.

## Pruebas sugeridas

```powershell
# Debe generar nonce
Invoke-WebRequest "https://localhost:5001/"

# No debería generar nonce
Invoke-WebRequest "https://localhost:5001/css/site.css"
```

## Pasos de migración

1. Respaldar middleware actual
2. Implementar middleware optimizado
3. Actualizar registro en `Program.cs`
4. Validar rutas estáticas y páginas
5. Monitorizar métricas y logs

## Resultado esperado

- Menor costo en Key Vault
- Mejor latencia en contenido estático
- Misma garantía de seguridad CSP para HTML
