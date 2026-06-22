# Revisão de Segurança — WebAppExperimental266

**Data:** 2026-05-07
**Âmbito:** Análise estática completa do código-base (seguimento da revisão de 2026-05-06)
**Revisor:** Revisão de Segurança Automatizada

---

## Resumo Executivo

Esta revisão de seguimento confirma que 3 das 5 vulnerabilidades identificadas na revisão de segurança de 2026-05-06 foram totalmente corrigidas, com 1 que permanece parcialmente corrigida. A revisão identifica também 4 novas descobertas. A postura de segurança geral da aplicação continua a melhorar.

---

## Estado das Descobertas Anteriores (2026-05-06)

| # | Descoberta | Gravidade | Estado |
|---|---------|----------|--------|
| 20 | NonceRefresherService retém dependências de construtor do Key Vault não utilizadas | 🟠 Alta | ✅ Corrigido |
| 21 | A cache interna do OcspValidationService utiliza um Dictionary não seguro para threads | 🟡 Média | ✅ Corrigido |
| 22 | O stub de validação OCSP ainda está presente — falha de forma fechada mas não implementado | 🔵 Baixa | ⚠️ Aceite (por design) |
| 23 | mTLS com AllowedIssuers vazio rejeita todos os certificados (fail-closed, não documentado) | 🔵 Baixa | ✅ Corrigido |
| 24 | OcspSettings.ServerUnavailableBehavior tem como padrão "Warn" (permite passagem em caso de erro) | 🔵 Baixa | ⚠️ Parcialmente Corrigido |

---

## Estado Detalhado das Descobertas Anteriores

### ✅ 20. NonceRefresherService Dependências DI Não Utilizadas — Corrigido

**Ficheiro:** `Services/NonceRefresherService.cs`

O construtor de `NonceRefresherService` agora apenas declara `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`. As quatro dependências anteriormente não utilizadas (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`) foram removidas. Isto resolve o risco de negação de serviço que impedia a aplicação de iniciar quando `EnableKeyVault = false` (o padrão) e `EnableNonceServices = true` (o padrão).

---

### ✅ 21. Cache Não Segura para Threads do OcspValidationService — Corrigido

**Ficheiro:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache` foi substituído por `ConcurrentDictionary<string, CachedOcspResponse>`. A chamada `_cache.Remove` foi atualizada para `_cache.TryRemove`. A cache é agora segura para acesso concorrente.

---

### ⚠️ 22. Stub de Validação OCSP — Aceite (Por Design)

**Ficheiro:** `Services/OcspValidationService.cs`

O stub permanece presente mas falha corretamente de forma fechada. Como `EnableOcspValidation` tem como padrão `false`, isto não tem impacto na produção. Isto é aceite como uma descoberta informativa enquanto aguarda uma implementação completa de OCSP.

---

### ✅ 23. mTLS AllowedIssuers Vazio — Corrigido

**Ficheiro:** `Extensions/ServiceCollectionExtensions.cs`

Um aviso de arranque é agora registado quando `ValidateClientCertificateIssuer = true` e `AllowedIssuers` está vazio:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

Isto fornece orientação clara aos operadores que encontram o comportamento fail-closed.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — Parcialmente Corrigido

**Ficheiros:** `appsettings.template.json` (corrigido), `Models/Settings/OcspSettings.cs` (ainda não corrigido)

O modelo agora especifica corretamente `"ServerUnavailableBehavior": "Fail"`. No entanto, o padrão da classe C# em `OcspSettings.cs` (linha 39) permanece `"Warn"`. Se um operador ativar OCSP e omitir `ServerUnavailableBehavior` do seu ficheiro de configuração, o padrão da classe `"Warn"` aplica-se silenciosamente, permitindo a passagem em interrupções do servidor OCSP. O padrão da classe deve ser alterado para corresponder à recomendação do modelo.

---

## Novas Descobertas

| # | Área | Gravidade |
|---|------|----------|
| 25 | O padrão da classe OcspSettings ("Warn") diverge do modelo ("Fail") | 🔵 Baixa |
| 26 | A chave nonce partilhada única do NonceCatalogService permite colisão de nonce entre pedidos | 🟡 Média |
| 27 | Os contadores estáticos do OptimizedNonceMiddleware usam inteiros de 32 bits com sinal (risco de transbordo) | 🔵 Baixa |
| 28 | Program.cs regista um singleton ILoggerFactory vazio, eclipsando o logger do framework | 🟡 Média |

---

## 🟡 Média

### 26. A Chave Nonce Partilhada do NonceCatalogService Permite Colisão de Nonce Entre Pedidos

**Ficheiros:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

O catálogo de nonces armazena todos os nonces sob uma única chave partilhada `"CSPNonce"`. Sob carga concorrente, é possível a seguinte condição de corrida:

1. O pedido A chama `RefreshNonceAsync()` — o nonce A1 é armazenado como `_nonceCollection["CSPNonce"]`.
2. O pedido B chama `RefreshNonceAsync()` — o nonce B1 substitui `_nonceCollection["CSPNonce"]`.
3. O pedido A chama `GetANonce("CSPNonce")` — recebe B1, não A1.
4. O cabeçalho CSP e o nonce de layout do pedido A contêm ambos B1.
5. O pedido B também contém B1.

Duas respostas concorrentes partilham o mesmo nonce. Embora ambos os valores sejam ainda criptograficamente aleatórios e imprevisíveis (sem cadeia codificada), o mesmo valor de nonce aparece em múltiplas respostas simultâneas, enfraquecendo a garantia de unicidade por pedido exigida pela especificação CSP. Um atacante que pode observar o nonce de uma resposta tem um nonce válido para pelo menos uma outra resposta concorrente.

**Recomendação:** Gere o nonce diretamente dentro do middleware por pedido (por exemplo, `Nonce.GenerateSecureNonce()`) e armazene-o apenas em `HttpContext.Items["Nonce"]`, contornando o catálogo partilhado para nonces por pedido. O catálogo partilhado seria então apenas necessário se um nonce precisar de ser partilhado entre camadas de middleware dentro de um único pedido, o que `HttpContext.Items` já trata de forma nativa.

---

### 28. Program.cs Regista um Singleton ILoggerFactory Vazio

**Ficheiro:** `Program.cs` (linha 85)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core regista automaticamente um `ILoggerFactory` totalmente configurado (com todos os fornecedores de registo da configuração `builder.Logging`) durante `WebApplication.CreateBuilder`. Este registo explícito `AddSingleton` adiciona uma segunda instância `LoggerFactory` não configurada sem fornecedores. Como `GetRequiredService<ILoggerFactory>()` devolve a implementação registada mais recentemente, os serviços que recebem `ILoggerFactory` via injeção de dependências (como `NonceRefresherService`) usarão esta fábrica vazia e não produzirão qualquer saída de registo via `_loggerFactory.CreateLogger<T>()`.

**Risco:** Registo silencioso no `NonceRefresherService` — os sucessos e falhas na geração de nonces não são emitidos para nenhum receptor de registo configurado. Isto reduz a observabilidade da aplicação durante operações sensíveis à segurança sem afetar a funcionalidade.

**Recomendação:** Remova o registo explícito `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`. O `ILoggerFactory` configurado do framework (com consola e quaisquer outros fornecedores) será então resolvido corretamente pelos serviços que dependem dele.

---

## 🔵 Baixa / Informativo

### 25. O Padrão da Classe OcspSettings Diverge do Modelo

**Ficheiro:** `Models/Settings/OcspSettings.cs` (linha 39)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

O modelo (`appsettings.template.json`) especifica `"ServerUnavailableBehavior": "Fail"`, mas o padrão da classe C# é `"Warn"`. Se `ServerUnavailableBehavior` estiver ausente do ficheiro de configuração ativo, o padrão da classe aplica-se silenciosamente em vez da recomendação do modelo. Isto é um resíduo da descoberta #24.

**Recomendação:** Altere o padrão da classe de `"Warn"` para `"Fail"` para alinhar com o modelo e o princípio do menor privilégio.

---

### 27. Os Contadores Estáticos do OptimizedNonceMiddleware Podem Transbordar

**Ficheiro:** `Services/OptimizedNonceMiddleware.cs` (linhas 25–26)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

Estes contadores de 32 bits com sinal são incrementados atomicamente via `Interlocked.Increment`. Após aproximadamente 2,1 mil milhões de incrementos, irão envolver para `int.MinValue` (−2.147.483.648), fazendo com que o cálculo de eficiência `(total - generated) * 100.0 / total` produza resultados incorretos ou sem significado. A 1.000 pedidos por segundo, o transbordo ocorre após aproximadamente 24,8 dias de operação contínua.

**Recomendação:** Altere os tipos de campo dos contadores de `int` para `long` e use a sobrecarga `long` de `Interlocked.Increment` para evitar o transbordo.

---

## Avaliação dos Cabeçalhos de Segurança (Estado Atual)

Os seguintes cabeçalhos são aplicados via `UseStandardSecurityHeaders` — inalterados em relação à revisão anterior:

| Cabeçalho | Valor | Avaliação |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ Bom |
| `X-XSS-Protection` | `0` | ✅ Bom (desativa o auditor obsoleto) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bom |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bom |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bom |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bom |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bom |
| `Permissions-Policy` | geolocalização, câmara, microfone, interest-cohort desativados | ✅ Bom |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bom |
| `Content-Security-Policy` | Baseado em nonce, aplicado quando CSP está ativado | ✅ Bom |
| `Server` | Mascarado para `"webserver"` | ✅ Bom |
| `X-Powered-By` | Removido | ✅ Bom |

---

## Avaliação Global

Todas as descobertas de alta gravidade das revisões anteriores foram corrigidas. As descobertas atuais limitam-se a dois problemas de gravidade média (#26 chave nonce partilhada, #28 ILoggerFactory vazio) e dois itens informativos de baixa gravidade (#25 discrepância no padrão da classe, #27 transbordo de inteiro nos contadores). Atenção imediata é recomendada para a descoberta #28 (singleton ILoggerFactory vazio) pois suprime silenciosamente o registo de diagnóstico relevante para a segurança durante operações de nonce. A descoberta #26 (chave nonce partilhada) deve ser abordada para restaurar a garantia de unicidade de nonce por pedido exigida pela especificação CSP.
