# WebAppExperimental266

Azure AD 認証、相互 TLS（mTLS）、Azure Key Vault 証明書管理、Azure Cosmos DB、Azure Blob Storage、**AWS Secrets Manager**、**Amazon DynamoDB**、**Google Cloud Secret Manager**、**Google Cloud Firestore**、およびノンスベースのコンテンツ セキュリティ ポリシーを備えた強化された HTTP セキュリティ層を持つ ASP.NET Core 9 Razor Pages Web アプリケーション。

---

## 目次

- [機能](#機能)
- [機能フラグ](#機能フラグ)
- [前提条件](#前提条件)
- [インストール – Windows Azure（App Service）](#インストール--windows-azureapp-service)
- [インストール – Azure サービスと通信する OpenBSD サーバー](#インストール--azure-サービスと通信する-openbsd-サーバー)
- [設定リファレンス](#設定リファレンス)
- [サポートスクリプト](#サポートスクリプト)
- [セキュリティに関する注意事項](#セキュリティに関する注意事項)

---

## 機能

### Azure AD 認証（OpenID Connect）
アプリケーションは、`Microsoft.Identity.Web` を使用して OpenID Connect プロトコルを通じて **Microsoft ID プラットフォーム** でユーザーを認証します。`/Experimental` 以下のすべてのルートには、認証された Azure AD ID が必要です。`/Privacy`、`/Error`、`/About` ページはパブリックにアクセス可能です。

### mTLS クライアント証明書認証
有効にすると、クライアントは有効な X.509 証明書を提示する必要があります。`MtlsSettings` の設定では、チェーン証明書、自己署名証明書、またはその両方を許可するか、証明書失効チェック、および許可された証明書発行者を制御します。

### Azure Key Vault 統合
アプリケーションは起動時に Azure Key Vault から TLS **サーバー証明書**を取得します。読み込まれた `X509Certificate2` は Kestrel の HTTPS 設定に直接注入されるため、ディスク上に PFX ファイルは必要ありません。

### リクエストごとのノンスを使用したコンテンツ セキュリティ ポリシー
有効にすると、すべての HTTP レスポンスに `Content-Security-Policy` ヘッダーが含まれ、その `script-src` ディレクティブにはリクエストごとの**暗号化された乱数ノンス**が含まれます。CSP はインライン スクリプトの SHA-256 ハッシュ ベースの許可リストもサポートします。

### 標準 HTTP セキュリティ ヘッダー
`UseStandardSecurityHeaders` は各レスポンスに次のヘッダーを付加します：`X-Frame-Options`、`X-Content-Type-Options`、`Strict-Transport-Security`、`Referrer-Policy`、`Cross-Origin-Opener-Policy`、`Cross-Origin-Resource-Policy`、`Permissions-Policy`、および `Server`、`X-Powered-By`、`X-AspNetMvc-Version` レスポンスヘッダーの削除。

### Azure Blob Storage
有効にすると、`BlobSettingsService` は接続文字列と設定可能な最大添付ファイル数によってサポートされるスコープ付きサービスを提供します。

### Azure Cosmos DB
有効にすると、アプリケーションは起動時に `database.ReadAsync()` を呼び出して Cosmos DB 接続を確認します。

### AWS Secrets Manager
有効にすると、`AwsSecretsManagerOperationsService` は AWS Secrets Manager からシークレットと証明書を取得します。`AwsSecretsManager` セクションの設定: `Region`、`CertificateSecretName`、`IVSecretName`、`NonceKeySecretName`、`AccessKeyId`/`SecretAccessKey` 資格情報。

### Amazon DynamoDB
有効にすると、`AwsDynamoDbService` は起動時に DynamoDB テーブルへの接続性を確認します。`AwsDynamoDb` セクションの設定: `Region`、`TableName`、`AccessKeyId`/`SecretAccessKey` 資格情報。

### GCP Secret Manager
有効にすると、`GcpSecretManagerOperationsService` は Google Cloud Secret Manager からシークレットを取得します。`GcpSecretManager` セクションの設定: `ProjectId`、`CertificateSecretId`、`IVSecretId`、`NonceKeySecretId`、`CredentialFilePath`（省略可、空の場合は ADC を使用）。

### GCP Firestore
有効にすると、`GcpFirestoreService` は起動時に Firestore クライアントを構築します。`GcpFirestore` セクションの設定: `ProjectId`、`DatabaseId`（デフォルト："(default)"）、`CollectionName`、`CredentialFilePath`（省略可）。

### AWS Cognito ID 管理
有効にすると、`AddAwsCognitoAuthentication` は **Amazon Cognito User Pool** に対して OpenID Connect 認証を設定します — Microsoft Entra ID / Azure AD の AWS 相当です。OIDC 検出エンドポイント：
`https://cognito-idp.{Region}.amazonaws.com/{UserPoolId}/.well-known/openid-configuration`
`AwsCognito` セクションの設定：`Region`、`UserPoolId`、`AppClientId`、`AppClientSecret`（User Secrets に保存）、`Domain`。

### GCP Identity Platform
有効にすると、`AddGcpIdentityAuthentication` は **Google OAuth 2.0 / OIDC** を使用して OpenID Connect 認証を設定します — Microsoft Entra ID / Azure AD の GCP 相当です。OIDC 検出エンドポイント：
`https://accounts.google.com/.well-known/openid-configuration`
`GcpIdentity` セクションの設定：`ClientId`、`ClientSecret`（User Secrets に保存）、省略可能な `ProjectId`。

### セキュアなセッション管理
セッションはインプロセス分散メモリ キャッシュを使用し、**30 分のアイドル タイムアウト**を設定します。セッション Cookie は `HttpOnly`、`Secure = Always`、`SameSite = Strict` で設定されます。

### ローカライズ
アプリケーションは **25 言語**をサポートします：en-US、de-DE、es-ES、fr-FR、pt-PT、it-IT、zh-HK、ko-KR、hi-IN、ru-RU、ar-SA、sw-KE、ja-JP、ht-HT、haw-US、sm-WS、mi-NZ、af-ZA、nl-NL、ha-NG、am-ET、yo-NG、bn-BD、zh-CN、ga-IE。アラビア語には RTL レイアウトの自動切り替えが含まれます。

### PII セーフ ロギング
`LoggingHelper` は HMAC-SHA256 を使用してログ出力の個人識別情報をハッシュ化します。32 バイトの安定したキーは `Logging:PiiHmacKey` 経由で提供できます。

---

## 機能フラグ

すべての主要サブシステムは `appsettings.json` のブール型機能フラグで制御されます。

| フラグ | デフォルト | 説明 |
|---|---|---|
| `EnableSession` | `true` | サーバー側セッションとセッション Cookie |
| `EnableLocalization` | `true` | 多言語サポート（25 言語） |
| `EnableAzureAd` | `true` | Azure AD / OpenID Connect 認証 |
| `EnableAuthorization` | `true` | ルートレベルの承認ポリシー |
| `EnableKeyVault` | `false` | Azure Key Vault から TLS サーバー証明書を読み込む |
| `EnableNonceServices` | `false` | リクエストごとの CSP ノンス生成 |
| `EnableCSP` | `false` | `Content-Security-Policy` ヘッダーを添付 |
| `EnableSecurityHeaders` | `true` | 標準 HTTP セキュリティ ヘッダーを添付 |
| `EnableBlobStorage` | `false` | Azure Blob Storage サービス |
| `EnableCosmosDb` | `false` | Azure Cosmos DB サービス |
| `EnableAwsSecretsManager` | `false` | AWS Secrets Manager（スタブ） |
| `EnableAwsDynamoDb` | `false` | Amazon DynamoDB |
| `EnableAwsCognito` | `false` | AWS Cognito OpenID Connect ID 管理 |
| `EnableGcpSecretManager` | `false` | GCP Secret Manager（スタブ） |
| `EnableGcpFirestore` | `false` | Google Cloud Firestore |
| `EnableGcpIdentity` | `false` | GCP Identity Platform（Google OAuth 2.0 / OIDC） |
| `EnableMtls` | `false` | クライアント TLS 証明書を要求 |
| `EnableOcspValidation` | `false` | OCSP 証明書失効チェック（スタブ） |

---

## 前提条件

1. **Azure AD アプリ登録** – リダイレクト URI、クライアント シークレットまたは証明書資格情報。
2. **Azure Key Vault** – シークレットとして PFX サーバー証明書を含む。
3. **Azure Cosmos DB アカウント**（省略可）。
4. **Azure Blob Storage アカウント**（省略可）。
5. **.NET 9 SDK / ランタイム** – バージョン 9.0 以降。
6. **AWS 資格情報**（`secretsmanager` および `dynamodb` 権限を持つ IAM ユーザーまたはロール）– `EnableAwsSecretsManager` または `EnableAwsDynamoDb` が有効な場合に必要。
7. **GCP サービス アカウントまたは ADC**（`secretmanager` および `datastore` 権限を持つ）– `EnableGcpSecretManager` または `EnableGcpFirestore` が有効な場合に必要。

---

## インストール – Windows Azure（App Service）

### 1. Azure リソースの作成

```powershell
# ログイン
az login

# リソース グループの作成
az group create --name MyResourceGroup --location eastus

# App Service プランの作成（Linux または Windows）
az appservice plan create --name MyPlan --resource-group MyResourceGroup --sku B1 --is-linux

# Web アプリの作成（.NET 9）
az webapp create --name MyWebApp26 --resource-group MyResourceGroup \
  --plan MyPlan --runtime "DOTNETCORE:9.0"
```

### 2. Azure AD アプリケーションの登録

[Azure Portal](https://portal.azure.com) で：
1. **Microsoft Entra ID → アプリの登録 → 新規登録**に移動。
2. リダイレクト URI を `https://<your-app>.azurewebsites.net/signin-oidc` に設定。
3. **証明書とシークレット**で、クライアント シークレットを作成して値をコピー。
4. 概要ブレードから**テナント ID** と**クライアント ID** をメモ。

### 3. Azure Key Vault の作成とサーバー証明書のアップロード

```powershell
az keyvault create --name MyKeyVault26 --resource-group MyResourceGroup --location eastus

# PFX を Key Vault シークレットとしてアップロード（base64 エンコード）
$pfxBase64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes("server.pfx"))
az keyvault secret set --vault-name MyKeyVault26 --name "ServerCert" --value $pfxBase64

# App Service マネージド ID へのアクセスを付与
az keyvault set-policy --name MyKeyVault26 \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### 4. アプリケーション設定の構成

`appsettings.template.json` を `appsettings.json` にコピーし、プレースホルダー値を入力します。シークレットはソース管理に保存**しないでください** — App Service アプリケーション設定として設定するか、ローカルでは User Secrets を使用します：

```powershell
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

### 5. アプリケーションのデプロイ

```bash
dotnet publish -c Release -o ./publish
cd publish
zip -r ../app.zip .
az webapp deployment source config-zip \
  --name MyWebApp26 --resource-group MyResourceGroup --src ../app.zip
```

### 6. HTTPS とカスタム ドメインの有効化（推奨）

```powershell
az webapp update --name MyWebApp26 --resource-group MyResourceGroup --https-only true

az webapp config hostname add --webapp-name MyWebApp26 --resource-group MyResourceGroup \
  --hostname www.example.com
az webapp config ssl bind --certificate-thumbprint <THUMBPRINT> \
  --name MyWebApp26 --resource-group MyResourceGroup --ssl-type SNI
```

### 7. Azure App Service での mTLS の有効化（省略可）

Azure App Service はポータルでクライアント証明書をサポートします：
1. **App Service → TLS/SSL 設定 → クライアント証明書**に移動。
2. **受信クライアント証明書**を**必須**に設定。

次に、アプリケーション設定で `FeatureFlags__EnableMtls=true` を設定します。

---

## インストール – Azure サービスと通信する OpenBSD サーバー

> **重要：** .NET 9 は OpenBSD 向けの公式 Microsoft ビルドが**ありません**。以下の手順では、OpenBSD のパッケージ ツリーで利用可能な [Podman](https://podman.io/) を通じて **Linux 互換コンテナー**を使用し、HTTPS 経由で Azure サービスと通信しながら OpenBSD 上で ASP.NET Core 9 アプリケーションを実行します。

### 1. OpenBSD への前提条件のインストール

```sh
# root として
pkg_add podman
pkg_add curl git
```

### 2. ASP.NET Core 9 ランタイム イメージのプル

```sh
podman pull mcr.microsoft.com/dotnet/aspnet:9.0
```

### 3. アプリケーションのビルド（Linux または Windows のビルド マシン上）

```bash
dotnet publish WebAppExperimental266/WebAppExperimental266.csproj \
  -c Release -r linux-x64 --self-contained true -o ./publish
```

### 4. 設定ファイルの作成

OpenBSD ホストで `/etc/webappexp26/appsettings.json` を作成します：

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

### 5. コンテナーの実行

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

### 6. OpenBSD Packet Filter（pf）ファイアウォールの設定

`/etc/pf.conf` に追加：

```
# インバウンド HTTPS を許可
pass in on egress proto tcp to port 443

# Azure AD、Key Vault、Cosmos DB、Blob Storage へのアウトバウンドを許可
pass out on egress proto tcp to port { 443 }
```

ルールセットのリロード：

```sh
pfctl -f /etc/pf.conf
```

### 7. DNS と TLS 証明書の設定

`AllowedHosts` のホスト名が OpenBSD サーバーのパブリック IP に解決されることを確認します。Azure AD はリダイレクト URI（`/signin-oidc`）が HTTPS 経由でアクセス可能であることを要求します。

### 8. Azure サービスへのアウトバウンド接続

| サービス | エンドポイント |
|---|---|
| Azure AD / Microsoft Identity | `login.microsoftonline.com` |
| Azure Key Vault | `<vault-name>.vault.azure.net` |
| Azure Cosmos DB | `<account>.documents.azure.com` |
| Azure Blob Storage | `<account>.blob.core.windows.net` |
| AWS Secrets Manager | `secretsmanager.REGION.amazonaws.com` |
| Amazon DynamoDB | `dynamodb.REGION.amazonaws.com` |
| GCP Secret Manager | `secretmanager.googleapis.com` |
| GCP Firestore | `firestore.googleapis.com` |

---

## 設定リファレンス

`appsettings.template.json` を `appsettings.json` にコピーして、すべての `{{PLACEHOLDER}}` 値を置き換えます。

| セクション | キー | 説明 |
|---|---|---|
| `AzureAd` | `TenantId`、`ClientId`、`ClientSecret` | Azure AD アプリ登録 |
| `AzureKeyVault` | `KeyVaultURL`、`KeyVaultSecret`、`KeyVaultPassName` | Key Vault と証明書名 |
| `MtlsSettings` | `RequireClientCertificate`、`AllowedIssuers` | mTLS クライアント証明書ポリシー |
| `NonceEncryption` | `Key`、`IV` | ノンス暗号化用の 32 バイト キーと 16 バイト IV（base64） |
| `BlobSettings` | `BlobConnectionString`、`MaxAttachments` | Blob Storage 接続 |
| `CosmosDb` | `CosmosConnectionString`、`DatabaseName`、`ContainerName` | Cosmos DB 接続 |
| `AwsSecretsManager` | `Region`、`CertificateSecretName`、`IVSecretName`、`NonceKeySecretName`、`AccessKeyId`、`SecretAccessKey` | AWS Secrets Manager |
| `AwsDynamoDb` | `Region`、`TableName`、`AccessKeyId`、`SecretAccessKey` | Amazon DynamoDB |
| `GcpSecretManager` | `ProjectId`、`CertificateSecretId`、`IVSecretId`、`NonceKeySecretId`、`CredentialFilePath` | GCP Secret Manager |
| `GcpFirestore` | `ProjectId`、`DatabaseId`、`CollectionName`、`CredentialFilePath` | Google Cloud Firestore |
| `OcspSettings` | `OcspServerUrl`、`CacheDurationMinutes` | OCSP 検証（スタブ） |
| `Logging` | `PiiHmacKey` | ログの PII ハッシュ用の 32 バイト base64 HMAC キー |

同梱の PowerShell スクリプトを使用して暗号化キーと IV を生成します：

```powershell
.\WebAppExperimental266\SupportingScripts\IVandKeySampleGenerator.ps1
```

ローカル開発用にすべてのシークレットを **.NET User Secrets** に保存します：

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

> GCP の場合は、`GOOGLE_APPLICATION_CREDENTIALS` 環境変数をサービス アカウント JSON キー ファイルのパスに設定するか、ローカル開発用に `gcloud auth application-default login` を実行します。

---

## サポートスクリプト

`SupportingScripts/` ディレクトリには PowerShell ユーティリティが含まれています：

| スクリプト | 目的 |
|---|---|
| `IVandKeySampleGenerator.ps1` | ランダムな 32 バイト AES キーと 16 バイト IV（base64）を生成 |
| `HashInlineScriptPowerShell.ps1` | インライン スクリプトの SHA-256 ハッシュを計算（CSP 許可リスト用） |
| `HashInlineScriptPowerShellBase64Output.ps1` | 上と同じ、base64 形式でハッシュを出力 |
| `CertificateUploaderToAzureExample.ps1` | PFX 証明書を Azure Key Vault にアップロード |
| `CheckRoles.ps1` | アプリの Azure RBAC ロール割り当てを確認 |
| `ExportResourceGroups.ps1` | Azure リソース グループ設定をエクスポート |
| `TroubleshootingCosmosDBInfo.ps1` | Cosmos DB 接続を診断 |
| `SetupFromTemplate.ps1` | `appsettings.template.json` から初期設定を自動化 |

---

## セキュリティに関する注意事項

- シークレット（`ClientSecret`、`KeyVaultSecret`、接続文字列、暗号化キー、AWS/GCP 資格情報）を**ソース管理にコミットしないでください**。ローカルでは .NET User Secrets を使用し、本番環境では Azure App Settings / Key Vault 参照を使用します。
- OCSP 検証実装はすべての証明書を拒否する**スタブ**です。本番環境で `EnableOcspValidation` を有効にする前に `OcspValidationService.cs` の `PerformOcspValidationAsync` を置き換えてください。
- ノンス値は**ログに記録されません** — ノンスをプレーンテキストでログに記録すると、ログ アクセス権を持つ攻撃者が任意のインライン スクリプトを注入できます。
- `Server` レスポンスヘッダーは `webserver` にマスクされ、プラットフォーム情報の公開を防ぎます。
- AWS `AccessKeyId` と `SecretAccessKey` は `appsettings.json` に**絶対に含めないでください** — User Secrets、環境変数、または IAM インスタンス ロールを使用します。
- GCP 資格情報では、サービス アカウント JSON ファイルをコミットするのではなく、**アプリケーション デフォルト資格情報（ADC）**（GKE の Workload Identity、またはローカルでは `gcloud auth application-default login`）を使用することを推奨します。
