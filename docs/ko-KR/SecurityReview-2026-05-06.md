# 보안 검토 — WebAppExperimental266

**날짜:** 2026-05-06
**범위:** 전체 코드베이스 정적 분석 (2026-05-05 검토에 대한 후속 검토)
**검토자:** 자동화 보안 검토

---

## 경영진 요약

이 후속 검토는 2026-05-05 보안 검토에서 식별된 19개의 취약점이 모두 수정되었음을 확인합니다. 이 검토는 또한 이번 세션에서 발견된 5개의 새로운 또는 잔류 발견 사항을 식별합니다. 이전 검토 이후 애플리케이션의 전반적인 보안 태세가 크게 개선되었습니다.

---

## 이전 발견 사항의 상태 (2026-05-05)

19개의 이전 발견 사항 모두 **수정 확인됨**:

| # | 발견 사항 | 심각도 | 상태 |
|---|----------|--------|------|
| 1 | nonce 생성 시 AES-GCM IV 재사용 | 🔴 치명적 | ✅ 수정됨 |
| 2 | Nonce가 평문으로 기록됨 | 🔴 치명적 | ✅ 수정됨 |
| 3 | 하드코딩된 대체 nonce 문자열 | 🔴 치명적 | ✅ 수정됨 |
| 4 | 스레드 안전하지 않은 전역 nonce 딕셔너리 | 🟠 높음 | ✅ 수정됨 |
| 5 | mTLS 발급자 유효성 검사가 주석 처리됨 | 🟠 높음 | ✅ 수정됨 |
| 6 | mTLS 해지 확인이 기본적으로 비활성화됨 | 🟠 높음 | ✅ 수정됨 |
| 7 | OCSP가 항상 유효를 반환함 (stub) | 🟠 높음 | ✅ 수정됨 |
| 8 | 구성에서 인증/권한 부여가 기본적으로 비활성화됨 | 🟠 높음 | ✅ 수정됨 |
| 9 | 보안 헤더가 파이프라인에서 너무 늦게 적용됨 | 🟠 높음 | ✅ 수정됨 |
| 10 | 세션 쿠키에 `Secure` + `SameSite` 누락 | 🟡 중간 | ✅ 수정됨 |
| 11 | 잘못된 형식의 전역 `Set-Cookie` 헤더 | 🟡 중간 | ✅ 수정됨 |
| 12 | `Content-Type`이 모든 곳에서 `text/html`로 강제됨 | 🟡 중간 | ✅ 수정됨 |
| 13 | `AllowedHosts`가 와일드카드로 설정됨 | 🟡 중간 | ✅ 수정됨 |
| 14 | 레이아웃의 `<script>` 태그에 Nonce가 적용되지 않음 | 🟡 중간 | ✅ 수정됨 |
| 15 | `Referrer-Policy` 헤더 누락 | 🟡 중간 | ✅ 수정됨 |
| 16 | PII가 평문으로 기록됨 | 🔵 낮음 | ✅ 수정됨 |
| 17 | 로그에 부분 연결 문자열 포함 | 🔵 낮음 | ✅ 수정됨 |
| 18 | Key Vault 작업이 stub임 | 🔵 낮음 | ✅ 수정됨 |
| 19 | 더 이상 사용되지 않는 `X-XSS-Protection: 1; mode=block` | 🔵 낮음 | ✅ 수정됨 |

---

## 새로운 / 잔류 발견 사항

| # | 영역 | 심각도 |
|---|------|--------|
| 20 | NonceRefresherService가 사용되지 않는 Key Vault 생성자 의존성을 유지함 | 🟠 높음 |
| 21 | OcspValidationService 내부 캐시가 스레드 안전하지 않은 Dictionary를 사용함 | 🟡 중간 |
| 22 | OCSP 유효성 검사 stub이 여전히 존재함 — 닫힌 상태로 실패하지만 구현되지 않음 | 🔵 낮음 |
| 23 | AllowedIssuers가 비어 있는 mTLS는 모든 인증서를 거부함 (fail-closed, 문서화되지 않음) | 🔵 낮음 |
| 24 | OcspSettings.ServerUnavailableBehavior 기본값이 "Warn"임 (오류 시 통과 허용) | 🔵 낮음 |

---

## 상세 발견 사항

### ✅ 2026-05-05의 확인된 수정 사항

#### 1. AES-GCM IV 재사용 — 수정됨

**파일:** `Models/Main_Objects/Nonce.cs`

AES-GCM 기반 nonce 생성이 완전히 교체되었습니다. `Nonce.GenerateSecureNonce()`는 이제 16개의 무작위 바이트에 대해 `RandomNumberGenerator.Fill(randomBytes)`를 호출하고 Base64 문자열을 반환합니다. Key Vault 의존성 없음, IV 없음, 암호화 없음 — CSP nonce에 대한 정확히 올바른 접근 방식입니다.

---

#### 2. Nonce 값이 더 이상 기록되지 않음 — 수정됨

**파일:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

두 파일 모두 이제 상태 메시지(`"Nonce retrieved for request."`, `"Nonce generated successfully."`)만 기록하고 nonce 값 자체는 기록하지 않습니다.

---

#### 3. 하드코딩된 대체 Nonce 제거됨 — 수정됨

**파일:** `Services/OptimizedNonceMiddleware.cs`

세 개의 하드코딩된 리터럴 문자열(`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`) 모두 정상 및 예외 대체 경로 모두에서 `Nonce.GenerateSecureNonce()` 호출로 교체되었습니다.

---

#### 4. 스레드 안전한 Nonce 딕셔너리 — 수정됨

**파일:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>`가 `ConcurrentDictionary<string, Nonce>`로 교체되었습니다. `GetANonce`는 이제 2단계 확인-후-조회 대신 단일 원자적 `TryGetValue` 호출을 사용합니다.

---

#### 5. mTLS 발급자 유효성 검사가 이제 기능함 — 수정됨

**파일:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

주석 처리된 발급자 유효성 검사 블록이 `mtlsSettings.IsIssuerAllowed(issuer)` 호출로 교체되었으며, 이는 `AllowedIssuers`에 대해 대소문자를 구분하지 않는 부분 문자열 일치를 수행합니다. 목록이 비어 있을 때(구성되지 않음), 메서드는 `false`를 반환하여 모든 인증서를 거부합니다(fail-closed).

---

#### 6. mTLS 해지 확인이 기본적으로 활성화됨 — 수정됨

**파일:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation`이 이제 기본적으로 `true`입니다. `appsettings.template.json`도 `"CheckCertificateRevocation": true`를 지정합니다.

---

#### 7. OCSP Stub이 이제 닫힌 상태로 실패함 — 수정됨

**파일:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync`는 이제 조용히 `IsValid = true`를 반환하는 대신 `IsValid = false`를 `OcspStatus.Error`와 함께 반환하고 오류를 기록합니다. 구성에서 OCSP를 활성화하면 이제 실제 구현이 제공될 때까지 모든 인증서를 거부하게 됩니다.

---

#### 8. 인증 및 권한 부여가 기본적으로 활성화됨 — 수정됨

**파일:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd`와 `EnableAuthorization` 모두 이제 `FeatureFlags` 클래스에서 기본적으로 `true`입니다. `appsettings.json`도 둘 다 `true`로 설정합니다.

---

#### 9. 보안 헤더가 라우팅 전에 적용됨 — 수정됨

**파일:** `Program.cs`

`UseNonceAndSecurityHeadersAsync`와 `UseStandardSecurityHeaders`가 이제 `UseRouting`, `UseAuthentication`, `UseAuthorization` 이전에 호출됩니다. 401/403 단락을 포함한 모든 응답이 보안 헤더를 받습니다.

---

#### 10–15. 쿠키, Content-Type, AllowedHosts, 레이아웃의 Nonce, Referrer-Policy — 수정됨

**파일:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- 세션 쿠키가 이제 `CookieSecurePolicy.Always`와 `SameSiteMode.Strict`를 설정합니다.
- 잘못된 형식의 이름 없는 `Set-Cookie` 헤더가 제거되었습니다.
- 전역 `Content-Type: text/html` 재정의가 제거되었습니다.
- `appsettings.json`의 `AllowedHosts`가 이제 `"localhost;127.0.0.1"`입니다; 템플릿은 `"{{YOUR_HOSTNAME}}"`을 사용합니다.
- `_Layout.cshtml`의 세 `<script>` 태그 모두 이제 `nonce="@Context.Items["Nonce"]"`를 포함합니다.
- `Referrer-Policy: strict-origin-when-cross-origin`이 이제 `UseStandardSecurityHeaders`에 의해 추가됩니다.

---

#### 16–19. PII 로깅, 연결 문자열 로그, Key Vault Stub, X-XSS-Protection — 수정됨

**파일:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- 모든 PII(OID, 이메일, 이름, SID, 역할)가 이제 로그에 기록되기 전에 `LoggingHelper.HashPii()`를 통해 HMAC-SHA256으로 해시됩니다. 안정적인 HMAC 키는 구성에서 `Logging:PiiHmacKey`를 통해 제공될 수 있습니다; 구성되지 않으면 무작위 프로세스별 키가 사용됩니다.
- Cosmos DB 로그 문은 이제 연결 문자열의 내용이 아니라 존재 여부만 확인합니다(`!string.IsNullOrEmpty`).
- `AzureKeyVaultCertificateOperations`는 이제 인증서가 null일 때 시작 시 더미 값을 조용히 반환하는 대신 `InvalidOperationException`을 발생시킵니다.
- `X-XSS-Protection`이 이제 `"0"`으로 설정되어(더 이상 사용되지 않는 XSS 감사기를 비활성화) 현대 브라우저 지침과 일관성을 갖습니다.

---

## 🟠 높음

### 20. NonceRefresherService가 사용되지 않는 Key Vault 생성자 의존성을 유지함

**파일:** `Services/NonceRefresherService.cs`

`NonceRefresherService`는 여전히 `IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`에 대한 생성자 매개변수를 선언합니다. nonce 생성이 `RandomNumberGenerator`를 직접 사용하도록 단순화되었으므로 이러한 의존성 중 어느 것도 사용되지 않습니다.

**위험:** `EnableNonceServices = true`이고 `EnableKeyVault = false`(기본값)일 때, 이러한 서비스는 DI 컨테이너에 등록되지 않아 nonce 서비스가 처음 해결될 때 런타임에 `InvalidOperationException`이 발생합니다. 이것은 기본 구성에 의해 트리거되는 사실상의 서비스 거부 조건입니다. `FeatureFlags` 클래스는 기본적으로 `EnableNonceServices = true`이므로, 클래스 기본값만 의존하는 환경(`appsettings.json` 재정의 없이)은 시작에 실패합니다.

**권고사항:** `NonceRefresherService`에서 네 개의 사용되지 않는 생성자 매개변수와 그에 해당하는 전용 필드를 제거합니다. 서비스는 `ILogger<NonceRefresherService>`, `ILoggerFactory`, `INonceCatalogService`만 필요합니다.

---

## 🟡 중간

### 21. OcspValidationService 내부 캐시가 스레드 안전하지 않은 Dictionary를 사용함

**파일:** `Services/OcspValidationService.cs` (47행)

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>`는 동시 읽기 및 쓰기에 대해 스레드 안전하지 않습니다. `OcspValidationService`가 싱글톤으로 등록된 경우(또는 동일한 인스턴스가 다른 메커니즘에 의해 요청 간에 공유되는 경우), 동시 OCSP 유효성 검사가 캐시를 손상시켜 항목 손실, 예외 발생 또는 오래된 데이터가 반환될 수 있습니다.

**권고사항:** `Dictionary<string, CachedOcspResponse>`를 `ConcurrentDictionary<string, CachedOcspResponse>`로 교체합니다. `_cache.Remove` 호출(103행)을 `_cache.TryRemove`로 업데이트합니다.

---

## 🔵 낮음 / 정보성

### 22. OCSP 유효성 검사 Stub — 닫힌 상태로 실패하지만 구현되지 않음

**파일:** `Services/OcspValidationService.cs` (157–173행)

`PerformOcspValidationAsync`는 여전히 stub입니다. 발견 사항 #7의 수정은 동작을 "항상 유효"에서 "항상 무효(fail-closed)"로 올바르게 변경했습니다. 그러나 메서드는 여전히 실제 OCSP 구현이 아닙니다. `EnableOcspValidation = false`(기본값)인 한 프로덕션 영향은 없습니다. 모든 환경에서 OCSP를 활성화하기 전에 프로덕션 품질의 OCSP 클라이언트를 구현해야 합니다.

---

### 23. AllowedIssuers가 비어 있는 mTLS는 모든 클라이언트 인증서를 거부함

**파일:** `Models/Settings/MtlsSettings.cs`

`ValidateClientCertificateIssuer = true`(기본값)이고 `AllowedIssuers`가 비어 있을 때(구성되지 않은 경우에도 기본값), `IsIssuerAllowed()`는 `false`를 반환하여 모든 클라이언트 인증서가 거부됩니다. 이것은 올바른 fail-closed 동작이지만 눈에 띄게 문서화되지 않았습니다. 템플릿을 주의 깊게 읽지 않고 mTLS를 활성화하는 운영자는 명확한 설명 없이 모든 클라이언트 연결이 거부되는 것을 발견할 수 있습니다.

**권고사항:** `ValidateClientCertificateIssuer = true`이고 `AllowedIssuers`가 비어 있을 때 시작 시 경고 로그 메시지를 추가합니다.

---

### 24. OcspSettings.ServerUnavailableBehavior 기본값이 "Warn"임

**파일:** `appsettings.template.json` (134행), `Services/OcspValidationService.cs`

`ServerUnavailableBehavior` 설정은 템플릿에서 기본적으로 `"Warn"`으로 설정되어 OCSP 서버에 도달할 수 없을 때 요청이 통과하도록 허용합니다. 높은 보안 환경에서는 OCSP 서버 중단이 인증서 해지 확인을 조용히 저하시키지 않도록 `"Fail"`이어야 합니다.

**권고사항:** 템플릿에서 세 가지 옵션(`Fail`, `Allow`, `Warn`)을 명확하게 문서화하고 최소 권한 원칙에 맞게 기본값을 `"Fail"`로 변경하는 것을 고려합니다.

---

## 보안 헤더 평가 (현재 상태)

다음 헤더들이 이제 `UseStandardSecurityHeaders`를 통해 적용됩니다:

| 헤더 | 값 | 평가 |
|------|----|------|
| `X-Frame-Options` | `DENY` | ✅ 양호 |
| `X-XSS-Protection` | `0` | ✅ 양호 (더 이상 사용되지 않는 감사기 비활성화) |
| `X-Content-Type-Options` | `nosniff` | ✅ 양호 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 양호 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 양호 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 양호 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 양호 |
| `Permissions-Policy` | 위치정보, 카메라, 마이크, interest-cohort 비활성화 | ✅ 양호 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 양호 |
| `Content-Security-Policy` | Nonce 기반, CSP 활성화 시 적용 | ✅ 양호 |
| `Server` | `"webserver"`로 마스킹됨 | ✅ 양호 |
| `X-Powered-By` | 제거됨 | ✅ 양호 |

---

## 전체 평가

애플리케이션은 이전 검토의 모든 치명적 및 높은 심각도 취약점을 해결했습니다. 현재 발견 사항은 하나의 높은 심각도 구성/DI 문제(발견 사항 #20)와 낮은 심각도 정보성 항목으로 제한됩니다. 보안 태세가 상당히 개선되었습니다. 발견 사항 #20(NonceRefresherService의 사용되지 않는 DI 의존성)에 대한 즉각적인 조치를 권장합니다. 이는 기본 구성에서 애플리케이션 시작을 방해할 수 있습니다.
