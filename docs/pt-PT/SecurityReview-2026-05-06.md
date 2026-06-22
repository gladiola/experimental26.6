# Revisão de Segurança — WebAppExperimental266

**Data:** 2026-05-06
**Âmbito:** Análise estática completa do código-fonte (seguimento à revisão de 2026-05-05)
**Revisor:** Revisão de Segurança Automatizada

---

## Sumário Executivo

Esta revisão de seguimento confirma que as 19 vulnerabilidades identificadas na revisão de segurança de 2026-05-05 foram todas corrigidas. A revisão identifica também 5 novos resultados ou residuais descobertos durante esta sessão. A postura de segurança geral da aplicação melhorou significativamente desde a revisão anterior.

---

## Estado dos Resultados Anteriores (2026-05-05)

Todos os 19 resultados anteriores estão **confirmados como corrigidos**:

| # | Resultado | Severidade | Estado |
|---|-----------|------------|--------|
| 1 | Reutilização de IV AES-GCM na geração de nonce | 🔴 Crítico | ✅ Corrigido |
| 2 | Nonce registado em texto simples | 🔴 Crítico | ✅ Corrigido |
| 3 | Cadeias de nonce de substituição codificadas | 🔴 Crítico | ✅ Corrigido |
| 4 | Dicionário de nonce global não thread-safe | 🟠 Alto | ✅ Corrigido |
| 5 | Validação de emissor mTLS comentada | 🟠 Alto | ✅ Corrigido |
| 6 | Verificação de revogação mTLS desativada por defeito | 🟠 Alto | ✅ Corrigido |
| 7 | OCSP retorna sempre válido (stub) | 🟠 Alto | ✅ Corrigido |
| 8 | Autenticação/autorização desativada por defeito na configuração | 🟠 Alto | ✅ Corrigido |
| 9 | Cabeçalhos de segurança aplicados demasiado tarde no pipeline | 🟠 Alto | ✅ Corrigido |
| 10 | Cookie de sessão sem `Secure` + `SameSite` | 🟡 Médio | ✅ Corrigido |
| 11 | Cabeçalho `Set-Cookie` global malformado | 🟡 Médio | ✅ Corrigido |
| 12 | `Content-Type` forçado para `text/html` em todo o lado | 🟡 Médio | ✅ Corrigido |
| 13 | `AllowedHosts` definido como wildcard | 🟡 Médio | ✅ Corrigido |
| 14 | Nonce não aplicado às etiquetas `<script>` no layout | 🟡 Médio | ✅ Corrigido |
| 15 | Cabeçalho `Referrer-Policy` em falta | 🟡 Médio | ✅ Corrigido |
| 16 | PII registada em texto simples | 🔵 Baixo | ✅ Corrigido |
| 17 | Cadeia de ligação parcial nos registos | 🔵 Baixo | ✅ Corrigido |
| 18 | Operações do Key Vault são stubs | 🔵 Baixo | ✅ Corrigido |
| 19 | `X-XSS-Protection: 1; mode=block` obsoleto | 🔵 Baixo | ✅ Corrigido |

---

## Resultados Novos / Residuais

| # | Área | Severidade |
|---|------|------------|
| 20 | NonceRefresherService retém dependências de construtor do Key Vault não utilizadas | 🟠 Alto |
| 21 | Cache interno do OcspValidationService utiliza Dictionary não thread-safe | 🟡 Médio |
| 22 | Stub de validação OCSP ainda presente — falha fechado mas não implementado | 🔵 Baixo |
| 23 | mTLS com AllowedIssuers vazio rejeita todos os certificados (fail-closed, não documentado) | 🔵 Baixo |
| 24 | OcspSettings.ServerUnavailableBehavior tem o valor padrão "Warn" (permite passagem em erro) | 🔵 Baixo |

---

## Resultados Detalhados

### ✅ Correções Confirmadas de 2026-05-05

#### 1. Reutilização de IV AES-GCM — Corrigido

**Ficheiro:** `Models/Main_Objects/Nonce.cs`

A geração de nonce baseada em AES-GCM foi completamente substituída. `Nonce.GenerateSecureNonce()` chama agora `RandomNumberGenerator.Fill(randomBytes)` em 16 bytes aleatórios e devolve uma cadeia Base64. Sem dependência do Key Vault, sem IV, sem encriptação — exatamente a abordagem correta para um nonce CSP.

---

#### 2. Valores de Nonce Já Não São Registados — Corrigido

**Ficheiros:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

Ambos os ficheiros registam agora apenas mensagens de estado (`"Nonce retrieved for request."`, `"Nonce generated successfully."`) e nunca o próprio valor do nonce.

---

#### 3. Nonces de Substituição Codificados Removidos — Corrigido

**Ficheiro:** `Services/OptimizedNonceMiddleware.cs`

As três cadeias literais codificadas (`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) foram substituídas por chamadas a `Nonce.GenerateSecureNonce()` tanto nos caminhos normais como nos de substituição por exceção.

---

#### 4. Dicionário de Nonce Thread-Safe — Corrigido

**Ficheiro:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>` foi substituído por `ConcurrentDictionary<string, Nonce>`. `GetANonce` usa agora uma única chamada atómica `TryGetValue` em vez de uma verificação em dois passos.

---

#### 5. Validação de Emissor mTLS Agora Funcional — Corrigido

**Ficheiro:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

O bloco de validação de emissor comentado foi substituído por uma chamada a `mtlsSettings.IsIssuerAllowed(issuer)`, que realiza uma correspondência de subcadeia insensível a maiúsculas/minúsculas contra `AllowedIssuers`. Quando a lista está vazia (não configurada), o método devolve `false`, rejeitando todos os certificados (fail-closed).

---

#### 6. Verificação de Revogação mTLS Ativada por Defeito — Corrigido

**Ficheiro:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation` tem agora o valor padrão `true`. O `appsettings.template.json` também especifica `"CheckCertificateRevocation": true`.

---

#### 7. Stub OCSP Agora Falha Fechado — Corrigido

**Ficheiro:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync` devolve agora `IsValid = false` com `OcspStatus.Error` e regista um erro, em vez de devolver silenciosamente `IsValid = true`. Ativar OCSP na configuração irá agora rejeitar todos os certificados até que seja fornecida uma implementação real, em vez de os aceitar silenciosamente.

---

#### 8. Autenticação e Autorização Ativadas por Defeito — Corrigido

**Ficheiro:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd` e `EnableAuthorization` têm agora ambos o valor padrão `true` na classe `FeatureFlags`. `appsettings.json` também define ambos como `true`.

---

#### 9. Cabeçalhos de Segurança Aplicados Antes do Roteamento — Corrigido

**Ficheiro:** `Program.cs`

`UseNonceAndSecurityHeadersAsync` e `UseStandardSecurityHeaders` são agora chamados antes de `UseRouting`, `UseAuthentication` e `UseAuthorization`. Todas as respostas, incluindo curto-circuitos 401/403, recebem os cabeçalhos de segurança.

---

#### 10–15. Cookie, Content-Type, AllowedHosts, Nonce no Layout, Referrer-Policy — Corrigido

**Ficheiros:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- O cookie de sessão define agora `CookieSecurePolicy.Always` e `SameSiteMode.Strict`.
- O cabeçalho `Set-Cookie` sem nome malformado foi removido.
- A substituição global `Content-Type: text/html` foi removida.
- `AllowedHosts` em `appsettings.json` é agora `"localhost;127.0.0.1"`; o modelo usa `"{{YOUR_HOSTNAME}}"`.
- As três etiquetas `<script>` em `_Layout.cshtml` incluem agora `nonce="@Context.Items["Nonce"]"`.
- `Referrer-Policy: strict-origin-when-cross-origin` é agora adicionado por `UseStandardSecurityHeaders`.

---

#### 16–19. Registo de PII, Registo de Cadeia de Ligação, Stubs do Key Vault, X-XSS-Protection — Corrigido

**Ficheiros:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- Toda a PII (OID, e-mail, nome, SID, funções) é agora processada com hash HMAC-SHA256 via `LoggingHelper.HashPii()` antes de ser escrita nos registos. Uma chave HMAC estável pode ser fornecida via `Logging:PiiHmacKey` na configuração; uma chave aleatória por processo é usada quando não configurada.
- A instrução de registo do Cosmos DB confirma agora apenas se uma cadeia de ligação está presente (`!string.IsNullOrEmpty`), não o seu conteúdo.
- `AzureKeyVaultCertificateOperations` lança agora `InvalidOperationException` no arranque quando o certificado é nulo, em vez de devolver silenciosamente valores fictícios.
- `X-XSS-Protection` está agora definido para `"0"` (desativando o auditor XSS obsoleto), consistente com as orientações modernas dos browsers.

---

## 🟠 Alto

### 20. NonceRefresherService Retém Dependências de Construtor do Key Vault Não Utilizadas

**Ficheiro:** `Services/NonceRefresherService.cs`

`NonceRefresherService` ainda declara parâmetros de construtor para `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService` e `IAzureKeyVaultOperationsService`. Uma vez que a geração de nonce foi simplificada para usar `RandomNumberGenerator` diretamente, nenhuma destas dependências é utilizada.

**Risco:** Quando `EnableNonceServices = true` e `EnableKeyVault = false` (o padrão), estes serviços não estão registados no contentor DI, causando uma `InvalidOperationException` em tempo de execução quando o serviço de nonce é resolvido pela primeira vez. Isto é efetivamente uma condição de negação de serviço desencadeada pela configuração padrão. A classe `FeatureFlags` tem por padrão `EnableNonceServices = true`, pelo que qualquer ambiente que dependa exclusivamente dos padrões de classe (sem substituições do `appsettings.json`) falharia ao iniciar.

**Recomendação:** Remova os quatro parâmetros de construtor não utilizados e os seus campos privados correspondentes de `NonceRefresherService`. O serviço requer apenas `ILogger<NonceRefresherService>`, `ILoggerFactory` e `INonceCatalogService`.

---

## 🟡 Médio

### 21. Cache Interno do OcspValidationService Utiliza Dictionary Não Thread-Safe

**Ficheiro:** `Services/OcspValidationService.cs` (linha 47)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>` não é thread-safe para leituras e escritas concorrentes. Se `OcspValidationService` estiver registado como singleton (ou se a mesma instância for partilhada entre pedidos por qualquer outro mecanismo), as validações OCSP concorrentes poderão corromper a cache, causando entradas perdidas, exceções lançadas ou dados obsoletos a serem devolvidos.

**Recomendação:** Substitua `Dictionary<string, CachedOcspResponse>` por `ConcurrentDictionary<string, CachedOcspResponse>`. Atualize a chamada `_cache.Remove` (linha 103) para `_cache.TryRemove`.

---

## 🔵 Baixo / Informativo

### 22. Stub de Validação OCSP — Falha Fechado mas Não Implementado

**Ficheiro:** `Services/OcspValidationService.cs` (linhas 157–173)

`PerformOcspValidationAsync` é ainda um stub. A correção do resultado #7 alterou corretamente o comportamento de "sempre válido" para "sempre inválido (fail-closed)". No entanto, o método ainda não é uma implementação OCSP real. Enquanto `EnableOcspValidation = false` (o padrão), isto não tem impacto em produção. Antes de ativar OCSP em qualquer ambiente, deve ser implementado um cliente OCSP de qualidade de produção.

---

### 23. mTLS com AllowedIssuers Vazio Rejeita Todos os Certificados de Cliente

**Ficheiro:** `Models/Settings/MtlsSettings.cs`

Quando `ValidateClientCertificateIssuer = true` (o padrão) e `AllowedIssuers` está vazio (também o padrão quando não configurado), `IsIssuerAllowed()` devolve `false`, fazendo com que todos os certificados de cliente sejam rejeitados. Este é o comportamento fail-closed correto, mas não está documentado de forma proeminente. Os operadores que ativam mTLS sem ler cuidadosamente o modelo podem descobrir que todas as ligações de cliente são rejeitadas sem uma explicação óbvia.

**Recomendação:** Adicione uma mensagem de registo de aviso no arranque quando `ValidateClientCertificateIssuer = true` e `AllowedIssuers` estiver vazio.

---

### 24. OcspSettings.ServerUnavailableBehavior Tem o Valor Padrão "Warn"

**Ficheiro:** `appsettings.template.json` (linha 134), `Services/OcspValidationService.cs`

A definição `ServerUnavailableBehavior` tem o valor padrão `"Warn"` no modelo, o que permite que os pedidos passem quando o servidor OCSP não pode ser alcançado. Para ambientes de alta segurança, isto deve ser `"Fail"` para que as falhas do servidor OCSP não degradem silenciosamente a verificação de revogação de certificados.

**Recomendação:** Documente claramente as três opções (`Fail`, `Allow`, `Warn`) no modelo e considere alterar o padrão para `"Fail"` de forma a respeitar o princípio do menor privilégio.

---

## Avaliação dos Cabeçalhos de Segurança (Estado Atual)

Os seguintes cabeçalhos são agora aplicados via `UseStandardSecurityHeaders`:

| Cabeçalho | Valor | Avaliação |
|-----------|-------|-----------|
| `X-Frame-Options` | `DENY` | ✅ Bom |
| `X-XSS-Protection` | `0` | ✅ Bom (desativa o auditor obsoleto) |
| `X-Content-Type-Options` | `nosniff` | ✅ Bom |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ Bom |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ Bom |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ Bom |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ Bom |
| `Permissions-Policy` | geolocalização, câmara, microfone, interest-cohort desativados | ✅ Bom |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ Bom |
| `Content-Security-Policy` | Baseado em nonce, aplicado quando CSP ativado | ✅ Bom |
| `Server` | Mascarado para `"webserver"` | ✅ Bom |
| `X-Powered-By` | Removido | ✅ Bom |

---

## Avaliação Geral

A aplicação corrigiu todas as vulnerabilidades de severidade crítica e alta da revisão anterior. Os resultados atuais limitam-se a um problema de configuração/DI de alta severidade (resultado #20) e itens informativos de menor severidade. A postura de segurança melhorou substancialmente. Recomenda-se ação imediata para o resultado #20 (dependências DI não utilizadas em NonceRefresherService), pois pode impedir a aplicação de iniciar com a configuração padrão.
