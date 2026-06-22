# セキュリティレビュー — WebAppExperimental266

**日付:** 2026-05-07
**範囲:** コードベース全体の静的解析（2026-05-06レビューのフォローアップ）
**レビュアー:** 自動化セキュリティレビュー

---

## エグゼクティブサマリー

このフォローアップレビューでは、2026-05-06のセキュリティレビューで特定された5件の脆弱性のうち3件が完全に修正され、1件が部分的に修正されていることを確認しました。またレビューでは4件の新たな発見事項も特定されました。アプリケーション全体のセキュリティ態勢は引き続き改善されています。

---

## 以前の発見事項のステータス（2026-05-06）

| # | 発見事項 | 重大度 | ステータス |
|---|---------|----------|--------|
| 20 | NonceRefresherServiceが未使用のKey Vaultコンストラクタ依存関係を保持している | 🟠 高 | ✅ 修正済み |
| 21 | OcspValidationServiceの内部キャッシュがスレッドセーフでないDictionaryを使用している | 🟡 中 | ✅ 修正済み |
| 22 | OCSP検証スタブが依然として存在する — クローズドモードで失敗するが未実装 | 🔵 低 | ⚠️ 承認済み（設計上） |
| 23 | AllowedIssuersが空のmTLSがすべての証明書を拒否する（fail-closed、未文書化） | 🔵 低 | ✅ 修正済み |
| 24 | OcspSettings.ServerUnavailableBehaviorのデフォルトが"Warn"（エラー時のパススルーを許可） | 🔵 低 | ⚠️ 部分的に修正済み |

---

## 以前の発見事項の詳細ステータス

### ✅ 20. NonceRefresherService 未使用DI依存関係 — 修正済み

**ファイル:** `Services/NonceRefresherService.cs`

`NonceRefresherService`コンストラクタは現在、`ILogger<NonceRefresherService>`、`ILoggerFactory`、`INonceCatalogService`のみを宣言しています。以前の4つの未使用依存関係（`IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService`、`IAzureKeyVaultOperationsService`）は削除されました。これにより、`EnableKeyVault = false`（デフォルト）かつ`EnableNonceServices = true`（デフォルト）の場合にアプリケーションの起動を妨げていたサービス拒否リスクが解消されます。

---

### ✅ 21. OcspValidationService スレッドセーフでないキャッシュ — 修正済み

**ファイル:** `Services/OcspValidationService.cs`

`Dictionary<string, CachedOcspResponse> _cache`が`ConcurrentDictionary<string, CachedOcspResponse>`に置き換えられました。`_cache.Remove`の呼び出しが`_cache.TryRemove`に更新されました。キャッシュは現在、並行アクセスに対して安全です。

---

### ⚠️ 22. OCSP検証スタブ — 承認済み（設計上）

**ファイル:** `Services/OcspValidationService.cs`

スタブは依然として存在しますが、正しくクローズドモードで失敗します。`EnableOcspValidation`のデフォルトが`false`であるため、本番環境への影響はありません。完全なOCSP実装が完了するまで、情報提供目的の発見事項として承認されます。

---

### ✅ 23. mTLS 空のAllowedIssuers — 修正済み

**ファイル:** `Extensions/ServiceCollectionExtensions.cs`

`ValidateClientCertificateIssuer = true`かつ`AllowedIssuers`が空の場合、起動時の警告がログに記録されるようになりました:

> `mTLS: ValidateClientCertificateIssuer is enabled but AllowedIssuers is empty. All client certificates will be rejected. Populate MtlsSettings:AllowedIssuers in configuration to allow specific issuers.`

これにより、fail-closed動作に遭遇したオペレーターに明確なガイダンスが提供されます。

---

### ⚠️ 24. OcspSettings.ServerUnavailableBehavior — 部分的に修正済み

**ファイル:** `appsettings.template.json`（修正済み）、`Models/Settings/OcspSettings.cs`（未修正）

テンプレートは現在、`"ServerUnavailableBehavior": "Fail"`を正しく指定しています。ただし、`OcspSettings.cs`（39行目）のC#クラスデフォルト値は`"Warn"`のままです。オペレーターがOCSPを有効にして構成ファイルから`ServerUnavailableBehavior`を省略した場合、クラスデフォルトの`"Warn"`が暗黙的に適用され、OCSPサーバー障害時のパススルーが許可されます。クラスデフォルト値はテンプレートの推奨値と一致するよう変更される必要があります。

---

## 新たな発見事項

| # | 領域 | 重大度 |
|---|------|----------|
| 25 | OcspSettingsクラスデフォルト（"Warn"）がテンプレート（"Fail"）と乖離している | 🔵 低 |
| 26 | NonceCatalogServiceの単一共有nonceキーがリクエスト間のnonce衝突を許容する | 🟡 中 |
| 27 | OptimizedNonceMiddlewareの静的カウンターが符号付き32ビット整数を使用している（オーバーフローリスク） | 🔵 低 |
| 28 | Program.csが空のILoggerFactoryシングルトンを登録し、フレームワークロガーを隠蔽している | 🟡 中 |

---

## 🟡 中

### 26. NonceCatalogService 共有Nonceキーがリクエスト間のNonce衝突を許容する

**ファイル:** `Services/NonceCatalogService.cs`、`Services/NonceMiddleware.cs`、`Services/OptimizedNonceMiddleware.cs`

nonceカタログは、すべてのnonceを単一の共有キー`"CSPNonce"`の下に保存します。並行負荷の下で、以下の競合状態が発生する可能性があります:

1. リクエストAが`RefreshNonceAsync()`を呼び出す — nonce A1が`_nonceCollection["CSPNonce"]`として保存される。
2. リクエストBが`RefreshNonceAsync()`を呼び出す — nonce B1が`_nonceCollection["CSPNonce"]`を上書きする。
3. リクエストAが`GetANonce("CSPNonce")`を呼び出す — A1ではなくB1を受け取る。
4. リクエストAのCSPヘッダーとレイアウトnonceの両方にB1が含まれる。
5. リクエストBにもB1が含まれる。

2つの並行レスポンスが同じnonceを共有します。両方の値は依然として暗号学的にランダムで予測不可能ですが（ハードコードされた文字列はない）、同じnonce値が複数の同時レスポンスに現れ、CSP仕様が要求するリクエストごとの一意性保証を弱体化させます。あるレスポンスのnonceを観察できる攻撃者は、少なくとも1つの他の並行レスポンスに対して有効なnonceを持つことになります。

**推奨事項:** リクエストごとにミドルウェア内で直接nonceを生成し（例: `Nonce.GenerateSecureNonce()`）、リクエストごとのnonceには共有カタログを経由せずに`HttpContext.Items["Nonce"]`にのみ保存してください。共有カタログは、nonceを1つのリクエスト内のミドルウェア層間で共有する必要がある場合にのみ必要となりますが、これは`HttpContext.Items`がすでにネイティブに処理しています。

---

### 28. Program.cs 空のILoggerFactoryシングルトンを登録する

**ファイル:** `Program.cs`（85行目）

```csharp
builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
```

ASP.NET Coreは`WebApplication.CreateBuilder`の実行中に、`builder.Logging`設定からすべてのログプロバイダーを含む完全に設定された`ILoggerFactory`を自動的に登録します。この明示的な`AddSingleton`登録により、プロバイダーなしの2番目の未設定`LoggerFactory`インスタンスが追加されます。`GetRequiredService<ILoggerFactory>()`が最後に登録された実装を返すため、依存関係の注入で`ILoggerFactory`を受け取るサービス（`NonceRefresherService`など）はこの空のファクトリを使用し、`_loggerFactory.CreateLogger<T>()`を通じてログ出力を生成しません。

**リスク:** `NonceRefresherService`でのサイレントロギング — nonce生成の成功と失敗が設定されたログシンクに送信されません。これにより、機能に影響を与えることなく、セキュリティに敏感な操作中のアプリケーションの可観測性が低下します。

**推奨事項:** 明示的な`builder.Services.AddSingleton<ILoggerFactory, LoggerFactory>()`登録を削除してください。フレームワークの設定済み`ILoggerFactory`（コンソールおよびその他のプロバイダーを含む）は、それに依存するサービスによって正しく解決されます。

---

## 🔵 低 / 情報提供

### 25. OcspSettingsクラスデフォルトがテンプレートと乖離している

**ファイル:** `Models/Settings/OcspSettings.cs`（39行目）

```csharp
public string ServerUnavailableBehavior { get; set; } = "Warn";
```

テンプレート（`appsettings.template.json`）は`"ServerUnavailableBehavior": "Fail"`を指定していますが、C#クラスのデフォルト値は`"Warn"`です。アクティブな構成ファイルから`ServerUnavailableBehavior`が欠如している場合、テンプレートの推奨値ではなくクラスデフォルトが暗黙的に適用されます。これは発見事項#24の残余です。

**推奨事項:** テンプレートおよび最小権限の原則に合わせるため、クラスデフォルトを`"Warn"`から`"Fail"`に変更してください。

---

### 27. OptimizedNonceMiddlewareの静的カウンターがオーバーフローする可能性がある

**ファイル:** `Services/OptimizedNonceMiddleware.cs`（25〜26行目）

```csharp
private static int _nonceGenerationCount = 0;
private static int _requestCount = 0;
```

これらの符号付き32ビットカウンターは`Interlocked.Increment`を通じてアトミックにインクリメントされます。約21億回のインクリメント後、`int.MinValue`（−2,147,483,648）にラップアラウンドし、効率計算`(total - generated) * 100.0 / total`が誤った、または無意味な結果を生成します。毎秒1,000リクエストの場合、約24.8日間の継続動作後にオーバーフローが発生します。

**推奨事項:** カウンターフィールドの型を`int`から`long`に変更し、オーバーフローを防止するために`Interlocked.Increment`の`long`オーバーロードを使用してください。

---

## セキュリティヘッダーの評価（現在の状態）

以下のヘッダーは`UseStandardSecurityHeaders`を通じて適用されています — 以前のレビューから変更なし:

| ヘッダー | 値 | 評価 |
|--------|-------|------------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（非推奨のオーディターを無効化） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | 位置情報、カメラ、マイク、interest-cohortを無効化 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | nonceベース、CSP有効時に適用 | ✅ 良好 |
| `Server` | `"webserver"`にマスキング | ✅ 良好 |
| `X-Powered-By` | 削除済み | ✅ 良好 |

---

## 総合評価

以前のレビューからの高重大度の発見事項はすべて修正されました。現在の発見事項は、2件の中程度の重大度の問題（#26共有nonceキー、#28空のILoggerFactory）と2件の低重大度の情報提供事項（#25クラスデフォルトの不一致、#27カウンターの整数オーバーフロー）に限定されています。nonce操作中にセキュリティ関連の診断ログを暗黙的に抑制する発見事項#28（空のILoggerFactoryシングルトン）への早急な対応が推奨されます。CSP仕様が要求するリクエストごとのnonce一意性保証を回復するために、発見事項#26（共有nonceキー）に対処する必要があります。
