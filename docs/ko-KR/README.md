# WebAppExperimental266

Azure AD 인증, mTLS(상호 TLS), Azure Key Vault 인증서 관리, Azure Cosmos DB, Azure Blob Storage, AWS Secrets Manager, Amazon DynamoDB, GCP Secret Manager, GCP Firestore, nonce 기반 Content Security Policy를 갖춘 강화된 HTTP 보안 레이어를 포함하는 ASP.NET Core 9 Razor Pages 웹 애플리케이션입니다.

---

## 목차

- [기능](#기능)
- [기능 플래그](#기능-플래그)
- [사전 요구 사항](#사전-요구-사항)
- [설치 – Windows Azure(App Service)](#설치--windows-azure-app-service)
- [설치 – Azure 서비스와 통신하는 OpenBSD 서버](#설치--azure-서비스와-통신하는-openbsd-서버)
- [구성 참조](#구성-참조)
- [지원 스크립트](#지원-스크립트)
- [보안 참고 사항](#보안-참고-사항)

---

## 기능

### Azure AD 인증(OpenID Connect)
애플리케이션은 OpenID Connect 프로토콜을 사용하여 **Microsoft ID 플랫폼**을 통해 사용자를 인증합니다(`Microsoft.Identity.Web` 사용). `/Experimental` 하위의 모든 경로는 인증된 Azure AD ID가 필요합니다. `/Privacy`, `/Error`, `/About` 페이지는 공개적으로 접근 가능합니다.

### mTLS 클라이언트 인증서 인증
활성화되면 클라이언트는 유효한 X.509 인증서를 제공해야 합니다. `MtlsSettings`의 설정은 체인 인증서, 자체 서명 인증서 또는 둘 다의 허용 여부, 인증서 해지 확인 및 허용된 인증서 발급자를 제어합니다.

### Azure Key Vault 통합
애플리케이션은 시작 시 Azure Key Vault에서 TLS **서버 인증서**를 가져옵니다. 로드된 `X509Certificate2`는 Kestrel의 HTTPS 기본값에 직접 주입되므로 디스크에 PFX 파일이 필요 없습니다.

### 요청별 Nonce를 사용한 Content Security Policy
활성화되면 모든 HTTP 응답에는 `script-src` 지시문에 요청별 **암호학적으로 안전한 무작위 nonce**가 포함된 `Content-Security-Policy` 헤더가 포함됩니다. CSP는 인라인 스크립트에 대한 SHA-256 해시 기반 허용 목록도 지원합니다.

### 표준 HTTP 보안 헤더
`UseStandardSecurityHeaders`는 모든 응답에 다음을 추가합니다: `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Permissions-Policy` 및 `Server`, `X-Powered-By`, `X-AspNetMvc-Version` 헤더 제거.

### Azure Blob Storage
활성화되면 `BlobSettingsService`는 연결 문자열과 구성 가능한 최대 첨부 파일 수로 지원되는 Scoped 서비스를 제공합니다.

### Azure Cosmos DB
활성화되면 애플리케이션은 시작 시 `database.ReadAsync()`를 호출하여 Cosmos DB 연결을 확인합니다.

### AWS Secrets Manager
활성화되면 `AwsSecretsManagerOperationsService`는 AWS Secrets Manager에서 비밀 및 인증서를 가져옵니다. `AwsSecretsManager` 섹션에서 `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName` 및 `AccessKeyId`/`SecretAccessKey` 자격 증명으로 구성합니다.

### Amazon DynamoDB
활성화되면 `AwsDynamoDbService`는 시작 시 DynamoDB 테이블 연결을 확인합니다. `AwsDynamoDb` 섹션에서 `Region`, `TableName` 및 `AccessKeyId`/`SecretAccessKey` 자격 증명으로 구성합니다.

### GCP Secret Manager
활성화되면 `GcpSecretManagerOperationsService`는 Google Cloud Secret Manager에서 비밀을 가져옵니다. `GcpSecretManager` 섹션에서 `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId` 및 `CredentialFilePath`(선택 사항, 비어 있으면 ADC 사용)로 구성합니다.

### GCP Firestore
활성화되면 `GcpFirestoreService`는 시작 시 Firestore 클라이언트를 빌드합니다. `GcpFirestore` 섹션에서 `ProjectId`, `DatabaseId`(기본값: "(default)"), `CollectionName` 및 `CredentialFilePath`(선택 사항)로 구성합니다.

### AWS Cognito 아이덴티티 관리
활성화되면 `AddAwsCognitoAuthentication`은 **Amazon Cognito 사용자 풀**에 대한 OpenID Connect 인증을 구성합니다 — Microsoft Entra ID / Azure AD의 AWS 대응물입니다. OIDC 검색 엔드포인트:
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
`AwsCognito` 섹션에서 구성: `Region`, `UserPoolId`, `AppClientId`, `AppClientSecret`(사용자 비밀에 저장), `Domain`(Cognito 호스팅 UI 도메인).

### GCP Identity Platform
활성화되면 `AddGcpIdentityAuthentication`은 **Google OAuth 2.0 / OIDC**를 사용하여 OpenID Connect 인증을 구성합니다 — Microsoft Entra ID / Azure AD의 GCP 대응물입니다. OIDC 검색 엔드포인트:
`https://accounts.google.com/.well-known/openid-configuration`
`GcpIdentity` 섹션에서 구성: `ClientId`, `ClientSecret`(사용자 비밀에 저장), 선택적 `ProjectId`. **Google Cloud Console → API 및 서비스 → 사용자 인증 정보**에서 자격 증명을 받으세요.

### 보안 세션 관리
세션은 **30분 유휴 시간 초과**를 사용하는 프로세스 내 분산 메모리 캐시를 사용합니다. 세션 쿠키는 `HttpOnly`, `Secure = Always`, `SameSite = Strict`로 구성됩니다.

### 현지화
애플리케이션은 **25개 언어**를 지원합니다: en-US, de-DE, es-ES, fr-FR, pt-PT, it-IT, zh-HK, ko-KR, hi-IN, ru-RU, ar-SA, sw-KE, ja-JP, ht-HT, haw-US, sm-WS, mi-NZ, af-ZA, nl-NL, ha-NG, am-ET, yo-NG, bn-BD, zh-CN, ga-IE. 아랍어에는 자동 RTL 레이아웃 전환이 포함됩니다.

### PII 안전 로깅
`LoggingHelper`는 HMAC-SHA256을 사용하여 로그 출력의 개인 식별 정보를 해시합니다. `Logging:PiiHmacKey`를 통해 안정적인 32바이트 키를 제공할 수 있습니다.

---

## 기능 플래그

모든 주요 하위 시스템은 `appsettings.json`의 boolean 기능 플래그로 제어됩니다.

| 플래그 | 기본값 | 설명 |
|---|---|---|
| `EnableSession` | `true` | 서버 측 세션 및 세션 쿠키 |
| `EnableLocalization` | `true` | 다국어 지원(25개 언어) |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect 인증 |
| `EnableAuthorization` | `true` | 경로 수준 권한 부여 정책 |
| `EnableKeyVault` | `false` | Azure Key Vault에서 TLS 서버 인증서 로드 |
| `EnableNonceServices` | `false` | 요청별 CSP nonce 생성 |
| `EnableCSP` | `false` | `Content-Security-Policy` 헤더 추가 |
| `EnableSecurityHeaders` | `true` | 표준 HTTP 보안 헤더 추가 |
| `EnableBlobStorage` | `false` | Azure Blob Storage 서비스 |
| `EnableCosmosDb` | `false` | Azure Cosmos DB 서비스 |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager 스텁 |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito 아이덴티티 관리(OpenID Connect) |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager 스텁 |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform(Google OAuth 2.0 / OIDC) |
| `EnableMtls` | `false` | 클라이언트 TLS 인증서 요구 |
| `EnableOcspValidation` | `false` | OCSP 인증서 해지 확인(스텁) |

---

## 사전 요구 사항

1. **Azure AD 앱 등록** – 리디렉션 URI, 클라이언트 암호 또는 인증서 자격 증명 포함.
2. **Azure Key Vault** – PFX 서버 인증서를 비밀로 포함.
3. **Azure Cosmos DB 계정**(선택 사항).
4. **Azure Blob Storage 계정**(선택 사항).
5. **.NET 9 SDK / 런타임** – 버전 9.0 이상.
6. **AWS 자격 증명** (`secretsmanager` 및 `dynamodb` 권한이 있는 IAM 사용자/역할) – `EnableAwsSecretsManager` 또는 `EnableAwsDynamoDb`가 활성화될 때 필요.
7. **GCP 서비스 계정 또는 ADC** (`secretmanager` 및 `datastore` 권한 포함) – `EnableGcpSecretManager` 또는 `EnableGcpFirestore`가 활성화될 때 필요.

---

## 설치 – Windows Azure(App Service)

### 1. Azure 리소스 만들기

```powershell
# Log in
az login

# Create a resource group
az group create --name MyResourceGroup --location eastus

# Create an App Service plan (Linux or Windows)
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Create the web app (.NET 9)
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Azure AD 애플리케이션 등록

[Azure Portal](https://portal.azure.com)에서:
1. **Microsoft Entra ID → 앱 등록 → 새 등록**으로 이동합니다.
2. 리디렉션 URI를 `https://<your-app>.azurewebsites.net/signin-oidc`로 설정합니다.
3. **인증서 및 비밀** 아래에서 클라이언트 비밀을 만들고 값을 복사합니다.
4. 개요 블레이드에서 **테넌트 ID**와 **클라이언트 ID**를 기록합니다.

### 3. Azure Key Vault 만들기 및 서버 인증서 업로드

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# Upload your PFX as a Key Vault secret (base64-encoded)
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# Grant the App Service Managed Identity access
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. 애플리케이션 설정 구성

`appsettings.template.json`을 `appsettings.json`으로 복사하고 자리 표시자 값을 입력합니다. 비밀은 소스 컨트롤에 **저장해서는 안 됩니다** — App Service 애플리케이션 설정으로 설정하거나 로컬에서 User Secrets를 통해 설정합니다:

```powershell
# In Azure App Service, set secrets as app settings:
az webapp config appsettings set --name MyWebApp26 --resource-group MyResourceGroup --settings \
  "AzureAd__TenantId=<TENANT_ID>" \
  "AzureAd__ClientId=<CLIENT_ID>" \
  "AzureAd__ClientSecret=<CLIENT_SECRET>" \
  "AzureKeyVault__KeyVaultURL=https://MyKeyVault26.vault.azure.net/" \
  "AzureKeyVault__KeyVaultSecret=<KV_SECRET>" \
  "AzureKeyVault__KeyVaultPassName=ServerCert" \
  "FeatureFlags__EnableKeyVault=true" \
  "FeatureFlags__EnableAzureAd=true"
```

### 5. 애플리케이션 배포

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. HTTPS 및 사용자 지정 도메인 활성화(권장)

```powershell
# Force HTTPS
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

# Bind a custom domain and managed TLS certificate
az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Azure App Service에서 mTLS 활성화(선택 사항)

Azure App Service는 포털을 통해 클라이언트 인증서를 지원합니다:
1. **App Service → TLS/SSL 설정 → 클라이언트 인증서**로 이동합니다.
2. **수신 클라이언트 인증서**를 **필수**로 설정합니다.

그런 다음 애플리케이션 설정에서 `FeatureFlags__EnableMtls=true`를 설정합니다.

---

## 설치 – Azure 서비스와 통신하는 OpenBSD 서버

> **중요:** .NET 9는 OpenBSD용 공식 Microsoft 빌드가 **없습니다**. 아래 지침은 OpenBSD에서 ASP.NET Core 9 애플리케이션을 실행하면서 HTTPS를 통해 Azure 서비스와 통신하기 위해 **Linux 호환 컨테이너**([Podman](https://podman.io/) 사용, OpenBSD 패키지 트리에서 사용 가능)를 사용합니다.

### 1. OpenBSD에 필수 구성 요소 설치

```sh
# As root
pkg_add podman
pkg_add curl git
```

OpenBSD 버전에서 Podman이나 Docker를 사용할 수 없는 경우, **Linux VM**(예: Debian/Ubuntu 게스트가 있는 vmm(4))에서 앱을 실행하고 해당 게스트 내에서 표준 Linux 배포 경로를 따르는 것을 고려하십시오.

### 2. ASP.NET Core 9 런타임 이미지 가져오기

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. 애플리케이션 빌드(Linux 또는 Windows 빌드 머신에서)

.NET 9 SDK가 설치된 머신에서 Linux x64를 대상으로 하는 자체 포함 빌드를 게시합니다:

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

`publish/` 디렉터리를 OpenBSD 호스트로 전송합니다(예: `scp` 또는 공유 볼륨 사용).

### 4. 구성 파일 만들기

OpenBSD 호스트에서 프로덕션 값으로 `/etc/webappexp26/appsettings.json`을 만듭니다(파일에 비밀 없음; 대신 환경 변수 사용):

```json
{
  "AllowedHosts": "your.hostname.example.com",
  "FeatureFlags": {
    "EnableAzureAd": true,
    "EnableKeyVault": true,
    "EnableSecurityHeaders": true,
    "EnableMtls": false
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "CallbackPath": "/signin-oidc"
  },
  "AzureKeyVault": {
    "KeyVaultURL": "https://YOUR_KEYVAULT_NAME.vault.azure.net/",
    "KeyVaultPassName": "ServerCert"
  }
}
```

비밀은 다음 단계에서 환경 변수로 주입됩니다.

### 5. 컨테이너 실행

```sh
podman run -d \
  --name webappexp26 \
  -p 443:8443 \
  -v /etc/webappexp26:/app/config:ro \
  -v /path/to/publish:/app:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="https://+:8443" \
  -e AzureAd__ClientSecret="YOUR_CLIENT_SECRET" \
  -e AzureKeyVault__KeyVaultSecret="YOUR_KV_SECRET" \
  -e Logging__PiiHmacKey="YOUR_32_BYTE_BASE64_KEY" \
  mcr.microsoft.com/dotnet/aspnet:9.0 \
  dotnet /app/WebAppExperimental266.dll \
    --contentRoot /app \
    --configDir /app/config
```

### 6. OpenBSD Packet Filter(pf) 방화벽 구성

`/etc/pf.conf`에 추가하여 인바운드 HTTPS를 허용하고 Azure 엔드포인트로의 아웃바운드 연결을 허용합니다:

```
# Allow inbound HTTPS
pass in on egress proto tcp to port 443

# Allow outbound to Azure AD, Key Vault, Cosmos DB, Blob Storage
pass out on egress proto tcp to port { 443 }
```

규칙 집합 다시 로드:

```sh
pfctl -f /etc/pf.conf
```

### 7. DNS 및 TLS 인증서 구성

`AllowedHosts`의 호스트 이름이 OpenBSD 서버의 공용 IP로 확인되는지 확인합니다. Azure AD는 리디렉션 URI(`/signin-oidc`)가 HTTPS를 통해 접근 가능해야 하므로 서버 인증서를 신뢰할 수 있어야 합니다. 공용 CA의 인증서(예: `acme-client(1)`을 통한 Let's Encrypt)를 사용하거나 CA 서명 인증서를 Azure Key Vault에 업로드하고 `EnableKeyVault`를 활성화하십시오.

### 8. Azure 서비스에 대한 아웃바운드 연결

다음 Azure 서비스 엔드포인트는 OpenBSD 호스트에서 TCP 443을 통해 접근 가능해야 합니다:

| Service | Endpoint |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

컨테이너를 시작하기 전에 연결을 테스트합니다:

```sh
curl -I https://login.microsoftonline.com
curl -I https://YOUR_KEYVAULT_NAME.vault.azure.net
```

---

## 구성 참조

`appsettings.template.json`을 `appsettings.json`으로 복사하고 모든 `{{PLACEHOLDER}}` 값을 교체합니다.

| 섹션 | 키 | 설명 |
|---|---|---|
| `AzureAd` | `TenantId`, `ClientId`, `ClientSecret` | Azure AD 앱 등록 |
| `AzureKeyVault` | `KeyVaultURL`, `KeyVaultSecret`, `KeyVaultPassName` | Key Vault 및 인증서 이름 |
| `MtlsSettings` | `RequireClientCertificate`, `AllowedIssuers` | mTLS 클라이언트 인증서 정책 |
| `NonceEncryption` | `Key`, `IV` | nonce 암호화를 위한 32바이트 키 및 16바이트 IV(base64) |
| `BlobSettings` | `BlobConnectionString`, `MaxAttachments` | Blob Storage 연결 |
| `CosmosDb` | `CosmosConnectionString`, `DatabaseName`, `ContainerName` | Cosmos DB 연결 |
| `AwsSecretsManager` | `Region`, `CertificateSecretName`, `IVSecretName`, `NonceKeySecretName`, `AccessKeyId`, `SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`, `TableName`, `AccessKeyId`, `SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`, `CertificateSecretId`, `IVSecretId`, `NonceKeySecretId`, `CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`, `DatabaseId`, `CollectionName`, `CredentialFilePath` | GCP Firestore |
| `OcspSettings` | `OcspServerUrl`, `CacheDurationMinutes` | OCSP 유효성 검사(스텁) |
| `Logging` | `PiiHmacKey` | 로그에서 PII 해싱을 위한 32바이트 base64 HMAC 키 |

포함된 PowerShell 스크립트를 사용하여 암호화 키와 IV를 생성합니다:

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

로컬 개발을 위해 모든 비밀을 **.NET User Secrets**에 저장합니다:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"
dotnet user-secrets set "AzureKeyVault:KeyVaultSecret" "YOUR_KV_SECRET"
dotnet user-secrets set "NonceEncryption:Key" "YOUR_BASE64_KEY"
dotnet user-secrets set "NonceEncryption:IV" "YOUR_BASE64_IV"
dotnet user-secrets set "AwsSecretsManager:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsSecretsManager:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
dotnet user-secrets set "AwsDynamoDb:AccessKeyId" "YOUR_AWS_ACCESS_KEY_ID"
dotnet user-secrets set "AwsDynamoDb:SecretAccessKey" "YOUR_AWS_SECRET_ACCESS_KEY"
```

> GCP의 경우 서비스 계정 JSON 파일 경로로 `GOOGLE_APPLICATION_CREDENTIALS` 환경 변수를 설정하거나 로컬 개발을 위해 `gcloud auth application-default login`을 실행합니다.

---

## 지원 스크립트

`SupportingScripts/` 디렉터리에는 PowerShell 유틸리티가 포함되어 있습니다:

| 스크립트 | 목적 |
|---|---|
| `IVandKeySampleGenerator.ps1` | 무작위 32바이트 AES 키 및 16바이트 IV(base64) 생성 |
| `HashInlineScriptPowerShell.ps1` | 인라인 스크립트에 대한 SHA-256 해시 계산(CSP 허용 목록용) |
| `HashInlineScriptPowerShellBase64Output.ps1` | 위와 동일, base64 형식으로 해시 출력 |
| `CertificateUploaderToAzureExample.ps1` | Azure Key Vault에 PFX 인증서 업로드 |
| `CheckRoles.ps1` | 앱의 Azure RBAC 역할 할당 확인 |
| `ExportResourceGroups.ps1` | Azure 리소스 그룹 구성 내보내기 |
| `TroubleshootingCosmosDBInfo.ps1` | Cosmos DB 연결 진단 |
| `SetupFromTemplate.ps1` | `appsettings.template.json`에서 초기 구성 자동화 |

---

## 보안 참고 사항

- **비밀을 소스 제어에 커밋하지 마십시오.**
- OCSP 유효성 검사 구현은 모든 인증서를 거부하는 **스텁**입니다. 프로덕션에서 `EnableOcspValidation`을 활성화하기 전에 `PerformOcspValidationAsync`를 교체하십시오.
- Nonce 값은 **절대 로깅되지 않습니다**.
- `Server` 응답 헤더는 `webserver`로 마스킹됩니다.
- **AWS 또는 GCP 자격 증명을 소스 제어에 저장하지 마십시오.** 환경 변수 또는 시크릿 관리자를 사용하십시오.
- AWS 및 GCP 구현은 **스텁**으로, 프로덕션 사용 전 완전한 구현이 필요합니다.
- AWS의 경우, 하드코딩된 액세스 키보다 IAM 역할을 가능한 한 선호하십시오.
- GCP의 경우, 명시적 서비스 계정 파일보다 Application Default Credentials(ADC)를 선호하십시오.
