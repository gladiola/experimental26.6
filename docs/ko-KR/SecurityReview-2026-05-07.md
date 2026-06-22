# 보안 검토 — WebAppExperimental266

**날짜:** 2026-05-07
**범위:** 전체 코드베이스 정적 분석 (2026-05-06 검토 후속 조치)
**검토자:** 자동화된 보안 검토

---

## 경영진 요약

이 후속 검토는 2026-05-06 보안 검토에서 확인된 5개의 취약점 중 3개가 완전히 수정되었으며, 1개는 부분적으로 수정되었음을 확인합니다. 검토에서는 4개의 새로운 발견 사항도 확인했습니다. 애플리케이션의 전반적인 보안 상태는 계속해서 개선되고 있습니다.

---

## 이전 발견 사항 상태 (2026-05-06)

| # | 발견 사항 | 심각도 | 상태 |
|---|---------|----------|--------|
| 20 | NonceRefresherService가 사용되지 않는 Key Vault 생성자 종속성을 유지함 | 🟠 높음 | ✅ 수정됨 |
| 21 | OcspValidationService 내부 캐시가 스레드 안전하지 않은 Dictionary를 사용함 | 🟡 보통 | ✅ 수정됨 |
| 22 | OCSP 검증 스텁이 여전히 존재함 — 닫힌 상태로 실패하지만 구현되지 않음 | 🔵 낮음 | ⚠️ 수락됨 (설계에 의함) |
| 23 | 빈 AllowedIssuers를 가진 mTLS가 모든 인증서를 거부함 (fail-closed, 문서화되지 않음) | 🔵 낮음 | ✅ 수정됨 |
| 24 | OcspSettings.ServerUnavailableBehavior의 기본값이 "Warn"임 (오류 시 통과 허용) | 🔵 낮음 | ⚠️ 부분적으로 수정됨 |

---

## 이전 발견 사항의 상세 상태

### ✅ 20. NonceRefresherService 사용되지 않는 DI 종속성 — 수정됨

**파일:** `Services/NonceRefresherService.cs`

`NonceRefresherService` 생성자는 이제 `ILogger<NonceRefresherService>`, `ILoggerFactory`, `INonceCatalogService`만 선언합니다. 이전에 사용되지 않던 네 가지 종속성 (`IKeyVaultSettingsService`, `INonceEncryptionSettingsService`, `IAzureADSettingsService`, `IAzureKeyVaultOperationsService`)이 제거되었습니다. 이로써 `EnableKeyVault = false`(기본값) 및 `EnableNonceServices = true`(기본값)일 때 애플리케이션이 시작되지 않는 서비스 거부 위험이 해결됩니다.

---

### ✅ 21. OcspValidationService 스레드 안전하지 않은 캐시 — 수정됨

**파일:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache`가 `ConcurrentDictionary<string, CachedOcspResponse>`로 교체되었습니다. `_cache.Remove` 호출이 `_cache.TryRemove`로 업데이트되었습니다. 캐시는 이제 동시 접근에 안전합니다.

---

### ⚠️ 22. OCSP 검증 스텁 — 수락됨 (설계에 의함)

**파일:** `Services/OcspValidationService.cs`

스텁이 여전히 존재하지만 올바르게 닫힌 상태로 실패합니다. `EnableOcspValidation`의 기본값이 `false`이므로 프로덕션에 영향이 없습니다. 완전한 OCSP 구현이 이루어질 때까지 정보 제공 목적의 발견 사항으로 수락됩니다.

---

### ✅ 23. mTLS 빈 AllowedIssuers — 수정됨

**파일:** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer = true`이고 `AllowedIssuers`가 비어 있을 때 시작 경고가 이제 기록됩니다:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

이는 fail-closed 동작을 경험하는 운영자에게 명확한 안내를 제공합니다.

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — 부분적으로 수정됨

**파일:** `appsettings.template.json` (수정됨), `Models/Settings/OcspSettings.cs` (아직 수정되지 않음)

템플릿은 이제 `"ServerUnavailableBehavior": "Fail"`을 올바르게 지정합니다. 그러나 `OcspSettings.cs`(39행)의 C# 클래스 기본값은 여전히 `"Warn"`입니다. 운영자가 OCSP를 활성화하고 구성 파일에서 `ServerUnavailableBehavior`를 생략하면, 클래스 기본값 `"Warn"`이 자동으로 적용되어 OCSP 서버 중단 시 통과가 허용됩니다. 클래스 기본값은 템플릿 권장 사항과 일치하도록 변경해야 합니다.

---

## 새로운 발견 사항

| # | 영역 | 심각도 |
|---|------|----------|
| 25 | OcspSettings 클래스 기본값 ("Warn")이 템플릿 ("Fail")과 다름 | 🔵 낮음 |
| 26 | NonceCatalogService 단일 공유 nonce 키로 인해 요청 간 nonce 충돌 허용 | 🟡 보통 |
| 27 | OptimizedNonceMiddleware 정적 카운터가 부호 있는 32비트 정수 사용 (오버플로 위험) | 🔵 낮음 |
| 28 | Program.cs가 빈 ILoggerFactory 싱글톤을 등록하여 프레임워크 로거를 가림 | 🟡 보통 |

---

## 🟡 보통

### 26. NonceCatalogService 공유 Nonce 키로 인한 요청 간 Nonce 충돌

**파일:** `Services/NonceCatalogService.cs`, `Services/NonceMiddleware.cs`, `Services/OptimizedNonceMiddleware.cs`

nonce 카탈로그는 모든 nonce를 단일 공유 키 `"CSPNonce"` 아래에 저장합니다. 동시 부하 하에서 다음과 같은 경쟁 조건이 가능합니다:

1. 요청 A가 `RefreshNonceAsync()`를 호출함 — nonce A1이 `_nonceCollection["CSPNonce"]`로 저장됨.
2. 요청 B가 `RefreshNonceAsync()`를 호출함 — nonce B1이 `_nonceCollection["CSPNonce"]`를 덮어씀.
3. 요청 A가 `GetANonce("CSPNonce")`를 호출함 — A1이 아닌 B1을 받음.
4. 요청 A의 CSP 헤더와 레이아웃 nonce 모두 B1을 포함함.
5. 요청 B도 B1을 포함함.

두 개의 동시 응답이 동일한 nonce를 공유합니다. 두 값 모두 여전히 암호학적으로 무작위이고 예측 불가능하지만(하드코딩된 문자열 없음), 동일한 nonce 값이 여러 동시 응답에 나타나 CSP 규격에서 요구하는 요청별 고유성 보장을 약화시킵니다. 한 응답의 nonce를 관찰할 수 있는 공격자는 적어도 하나의 다른 동시 응답에 대한 유효한 nonce를 갖게 됩니다.

**권장 사항:** 요청별로 미들웨어 내에서 직접 nonce를 생성하고(예: `Nonce.GenerateSecureNonce()`), 요청별 nonce를 위한 공유 카탈로그를 우회하여 `HttpContext.Items["Nonce"]`에만 저장하십시오. 공유 카탈로그는 단일 요청 내 미들웨어 계층 간에 nonce를 공유해야 하는 경우에만 필요한데, 이는 `HttpContext.Items`가 이미 기본으로 처리합니다.

---

### 28. Program.cs 빈 ILoggerFactory 싱글톤 등록

**파일:** `Program.cs` (85행)

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Core는 `WebApplication.CreateBuilder` 중에 `builder.Logging` 구성의 모든 로깅 공급자가 포함된 완전히 구성된 `ILoggerFactory`를 자동으로 등록합니다. 이 명시적인 `AddSingleton` 등록은 공급자 없이 구성되지 않은 두 번째 `LoggerFactory` 인스턴스를 추가합니다. `GetRequiredService<ILoggerFactory>()`는 가장 최근에 등록된 구현을 반환하므로, 의존성 주입을 통해 `ILoggerFactory`를 받는 서비스(`NonceRefresherService` 등)는 이 빈 팩토리를 사용하고 `_loggerFactory.CreateLogger<T>()`를 통해 로그 출력을 생성하지 않습니다.

**위험:** `NonceRefresherService`에서의 무음 로깅 — nonce 생성 성공 및 실패가 구성된 로깅 싱크로 전송되지 않습니다. 이는 기능에 영향을 주지 않으면서 보안에 민감한 작업 중 애플리케이션의 관찰 가능성을 저하시킵니다.

**권장 사항:** 명시적인 `builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()` 등록을 제거하십시오. 프레임워크의 구성된 `ILoggerFactory`(콘솔 및 다른 공급자 포함)가 이에 의존하는 서비스에 의해 올바르게 해결될 것입니다.

---

## 🔵 낮음 / 정보 제공

### 25. OcspSettings 클래스 기본값이 템플릿과 다름

**파일:** `Models/Settings/OcspSettings.cs` (39행)

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

템플릿(`appsettings.template.json`)은 `"ServerUnavailableBehavior": "Fail"`을 지정하지만, C# 클래스 기본값은 `"Warn"`입니다. 활성 구성 파일에서 `ServerUnavailableBehavior`가 없으면 템플릿 권장 사항 대신 클래스 기본값이 자동으로 적용됩니다. 이는 발견 사항 #24의 잔여물입니다.

**권장 사항:** 템플릿 및 최소 권한 원칙에 맞게 클래스 기본값을 `"Warn"`에서 `"Fail"`로 변경하십시오.

---

### 27. OptimizedNonceMiddleware 정적 카운터의 오버플로 가능성

**파일:** `Services/OptimizedNonceMiddleware.cs` (25–26행)

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

이 부호 있는 32비트 카운터는 `Interlocked.Increment`를 통해 원자적으로 증가됩니다. 약 21억 번의 증가 후, `int.MinValue`(−2,147,483,648)로 랩어라운드되어 효율성 계산 `(total - generated) * 100.0 / total`이 잘못되거나 의미 없는 결과를 생성하게 됩니다. 초당 1,000개의 요청에서 약 24.8일 연속 운영 후 오버플로가 발생합니다.

**권장 사항:** 카운터 필드 유형을 `int`에서 `long`으로 변경하고 오버플로를 방지하기 위해 `Interlocked.Increment`의 `long` 오버로드를 사용하십시오.

---

## 보안 헤더 평가 (현재 상태)

다음 헤더는 `UseStandardSecurityHeaders`를 통해 적용됩니다 — 이전 검토와 변경 없음:

| 헤더 | 값 | 평가 |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ 양호 |
| `X-XSS-Protection` | `0` | ✅ 양호 (더 이상 사용되지 않는 감사기 비활성화) |
| `X-Content-Type-Options` | `nosniff` | ✅ 양호 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 양호 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 양호 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 양호 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 양호 |
| `Permissions-Policy` | 지리위치, 카메라, 마이크, interest-cohort 비활성화 | ✅ 양호 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 양호 |
| `Content-Security-Policy` | Nonce 기반, CSP 활성화 시 적용 | ✅ 양호 |
| `Server` | `"webserver"`로 마스킹 | ✅ 양호 |
| `X-Powered-By` | 제거됨 | ✅ 양호 |

---

## 전반적인 평가

이전 검토의 모든 높은 심각도 발견 사항이 수정되었습니다. 현재 발견 사항은 두 가지 중간 심각도 문제(#26 공유 nonce 키, #28 빈 ILoggerFactory)와 두 가지 낮은 심각도 정보 항목(#25 클래스 기본값 불일치, #27 카운터의 정수 오버플로)으로 제한됩니다. nonce 작업 중 보안 관련 진단 로깅을 자동으로 억제하는 발견 사항 #28(빈 ILoggerFactory 싱글톤)에 즉각적인 주의를 기울일 것을 권장합니다. CSP 규격에서 요구하는 요청별 nonce 고유성 보장을 복원하기 위해 발견 사항 #26(공유 nonce 키)을 해결해야 합니다.
