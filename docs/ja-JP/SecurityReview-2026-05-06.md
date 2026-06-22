# セキュリティレビュー — WebAppExperimental266

**日付:** 2026-05-06
**対象範囲:** コードベース全体の静的解析（2026-05-05レビューのフォローアップ）
**レビュアー:** 自動セキュリティレビュー

---

## エグゼクティブサマリー

このフォローアップレビューは、2026-05-05のセキュリティレビューで特定された19件の脆弱性がすべて修正されたことを確認します。また、このセッション中に発見された5件の新規または残存する問題も特定しています。前回のレビュー以降、アプリケーションの全体的なセキュリティ態勢は大幅に改善されています。

---

## 以前の発見事項のステータス（2026-05-05）

19件の以前の発見事項はすべて**修正確認済み**です：

| # | 発見事項 | 深刻度 | ステータス |
|---|----------|--------|-----------|
| 1 | nonce生成でのAES-GCM IVの再利用 | 🔴 重大 | ✅ 修正済み |
| 2 | Nonceが平文でログに記録される | 🔴 重大 | ✅ 修正済み |
| 3 | ハードコードされたフォールバックnonce文字列 | 🔴 重大 | ✅ 修正済み |
| 4 | スレッドセーフでないグローバルnonce辞書 | 🟠 高 | ✅ 修正済み |
| 5 | mTLS発行者検証がコメントアウトされている | 🟠 高 | ✅ 修正済み |
| 6 | mTLS失効確認がデフォルトで無効 | 🟠 高 | ✅ 修正済み |
| 7 | OCSPが常に有効を返す（スタブ） | 🟠 高 | ✅ 修正済み |
| 8 | 設定で認証/認可がデフォルトで無効 | 🟠 高 | ✅ 修正済み |
| 9 | セキュリティヘッダーがパイプライン内で遅すぎるタイミングで適用される | 🟠 高 | ✅ 修正済み |
| 10 | セッションクッキーに`Secure` + `SameSite`が欠如 | 🟡 中 | ✅ 修正済み |
| 11 | 不正な形式のグローバル`Set-Cookie`ヘッダー | 🟡 中 | ✅ 修正済み |
| 12 | `Content-Type`がすべての場所で`text/html`に強制される | 🟡 中 | ✅ 修正済み |
| 13 | `AllowedHosts`がワイルドカードに設定されている | 🟡 中 | ✅ 修正済み |
| 14 | レイアウトの`<script>`タグにNonceが適用されていない | 🟡 中 | ✅ 修正済み |
| 15 | `Referrer-Policy`ヘッダーが欠如 | 🟡 中 | ✅ 修正済み |
| 16 | PIIが平文でログに記録される | 🔵 低 | ✅ 修正済み |
| 17 | ログに部分的な接続文字列 | 🔵 低 | ✅ 修正済み |
| 18 | Key Vault操作がスタブ | 🔵 低 | ✅ 修正済み |
| 19 | 非推奨の`X-XSS-Protection: 1; mode=block` | 🔵 低 | ✅ 修正済み |

---

## 新規 / 残存する発見事項

| # | 領域 | 深刻度 |
|---|------|--------|
| 20 | NonceRefresherServiceが未使用のKey Vaultコンストラクタ依存関係を保持している | 🟠 高 |
| 21 | OcspValidationServiceの内部キャッシュがスレッドセーフでないDictionaryを使用している | 🟡 中 |
| 22 | OCSP検証スタブがまだ存在する — 閉じた状態で失敗するが未実装 | 🔵 低 |
| 23 | AllowedIssuersが空のmTLSはすべての証明書を拒否する（fail-closed、文書化されていない） | 🔵 低 |
| 24 | OcspSettings.ServerUnavailableBehaviorのデフォルトが"Warn"（エラー時のパススルーを許可） | 🔵 低 |

---

## 詳細な発見事項

### ✅ 2026-05-05からの確認された修正

#### 1. AES-GCM IVの再利用 — 修正済み

**ファイル:** `Models/Main_Objects/Nonce.cs`

AES-GCMベースのnonce生成が完全に置き換えられました。`Nonce.GenerateSecureNonce()`は現在、16個のランダムなバイトに対して`RandomNumberGenerator.Fill(randomBytes)`を呼び出し、Base64文字列を返します。Key Vault依存なし、IVなし、暗号化なし — CSP nonceに対して正確に正しいアプローチです。

---

#### 2. Nonce値がログに記録されなくなった — 修正済み

**ファイル:** `Services/NonceMiddleware.cs`, `Services/NonceRefresherService.cs`

両方のファイルは現在、ステータスメッセージ（`"Nonce retrieved for request."`, `"Nonce generated successfully."`）のみをログに記録し、nonce値自体は記録しません。

---

#### 3. ハードコードされたフォールバックNonceが削除された — 修正済み

**ファイル:** `Services/OptimizedNonceMiddleware.cs`

3つのハードコードされたリテラル文字列（`"bootstrap-nonce-placeholder"`, `"fallback-nonce"`, `"error-fallback-nonce"`）はすべて、通常パスと例外フォールバックパスの両方で`Nonce.GenerateSecureNonce()`の呼び出しに置き換えられました。

---

#### 4. スレッドセーフなNonce辞書 — 修正済み

**ファイル:** `Services/NonceCatalogService.cs`

`Dictionary<string, Nonce>`が`ConcurrentDictionary<string, Nonce>`に置き換えられました。`GetANonce`は現在、2段階のチェックと検索の代わりに、単一のアトミックな`TryGetValue`呼び出しを使用します。

---

#### 5. mTLS発行者検証が機能するようになった — 修正済み

**ファイル:** `Extensions/ServiceCollectionExtensions.cs`, `Models/Settings/MtlsSettings.cs`

コメントアウトされた発行者検証ブロックが`mtlsSettings.IsIssuerAllowed(issuer)`の呼び出しに置き換えられました。これは`AllowedIssuers`に対して大文字小文字を区別しない部分文字列マッチを実行します。リストが空（未設定）の場合、メソッドは`false`を返し、すべての証明書を拒否します（fail-closed）。

---

#### 6. mTLS失効確認がデフォルトで有効 — 修正済み

**ファイル:** `Models/Settings/MtlsSettings.cs`

`CheckCertificateRevocation`のデフォルトが`true`になりました。`appsettings.template.json`も`"CheckCertificateRevocation": true`を指定しています。

---

#### 7. OCSPスタブが閉じた状態で失敗するようになった — 修正済み

**ファイル:** `Services/OcspValidationService.cs`

`PerformOcspValidationAsync`は現在、静かに`IsValid = true`を返す代わりに、`OcspStatus.Error`と共に`IsValid = false`を返し、エラーをログに記録します。設定でOCSPを有効にすると、実際の実装が提供されるまですべての証明書が拒否されます。

---

#### 8. 認証と認可がデフォルトで有効 — 修正済み

**ファイル:** `Models/Settings/FeatureFlags.cs`

`EnableAzureAd`と`EnableAuthorization`は両方とも、`FeatureFlags`クラスでデフォルト`true`になりました。`appsettings.json`も両方を`true`に設定します。

---

#### 9. セキュリティヘッダーがルーティング前に適用される — 修正済み

**ファイル:** `Program.cs`

`UseNonceAndSecurityHeadersAsync`と`UseStandardSecurityHeaders`は現在、`UseRouting`、`UseAuthentication`、`UseAuthorization`の前に呼び出されます。401/403の短絡を含むすべてのレスポンスがセキュリティヘッダーを受け取ります。

---

#### 10–15. クッキー、Content-Type、AllowedHosts、レイアウト内のNonce、Referrer-Policy — 修正済み

**ファイル:** `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`, `Views/Shared/_Layout.cshtml`, `appsettings.json`

- セッションクッキーが`CookieSecurePolicy.Always`と`SameSiteMode.Strict`を設定するようになりました。
- 名前のない不正な形式の`Set-Cookie`ヘッダーが削除されました。
- グローバルな`Content-Type: text/html`オーバーライドが削除されました。
- `appsettings.json`の`AllowedHosts`が`"localhost;127.0.0.1"`になりました；テンプレートは`"{{YOUR_HOSTNAME}}"`を使用します。
- `_Layout.cshtml`の3つの`<script>`タグすべてに`nonce="@Context.Items["Nonce"]"`が含まれるようになりました。
- `Referrer-Policy: strict-origin-when-cross-origin`が`UseStandardSecurityHeaders`によって追加されるようになりました。

---

#### 16–19. PIIロギング、接続文字列ログ、Key Vaultスタブ、X-XSS-Protection — 修正済み

**ファイル:** `Services/LoggingHelper.cs`, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`

- すべてのPII（OID、メール、名前、SID、ロール）は現在、ログに書き込まれる前に`LoggingHelper.HashPii()`を通じてHMAC-SHA256でハッシュされます。安定したHMACキーは設定の`Logging:PiiHmacKey`を通じて提供できます；設定されていない場合はプロセスごとのランダムキーが使用されます。
- Cosmos DBログステートメントは現在、接続文字列の内容ではなく、存在するかどうかのみを確認します（`!string.IsNullOrEmpty`）。
- `AzureKeyVaultCertificateOperations`は現在、証明書がnullの場合、ダミー値を静かに返す代わりに、起動時に`InvalidOperationException`をスローします。
- `X-XSS-Protection`が`"0"`に設定され（非推奨のXSS監査機能を無効にする）、現代のブラウザガイダンスと一致しています。

---

## 🟠 高

### 20. NonceRefresherServiceが未使用のKey Vaultコンストラクタ依存関係を保持している

**ファイル:** `Services/NonceRefresherService.cs`

`NonceRefresherService`は引き続き`IKeyVaultSettingsService`、`INonceEncryptionSettingsService`、`IAzureADSettingsService`、`IAzureKeyVaultOperationsService`のコンストラクタパラメータを宣言しています。nonce生成が`RandomNumberGenerator`を直接使用するように簡略化されたため、これらの依存関係はどれも使用されていません。

**リスク:** `EnableNonceServices = true`かつ`EnableKeyVault = false`（デフォルト）の場合、これらのサービスはDIコンテナに登録されておらず、nonceサービスが最初に解決されるときにランタイムで`InvalidOperationException`が発生します。これは実質的に、デフォルト設定によって引き起こされるサービス拒否状態です。`FeatureFlags`クラスはデフォルトで`EnableNonceServices = true`を設定するため、クラスのデフォルト値のみに依存する環境（`appsettings.json`のオーバーライドなし）は起動に失敗します。

**推奨事項:** `NonceRefresherService`から4つの未使用のコンストラクタパラメータと対応するプライベートフィールドを削除してください。サービスには`ILogger<NonceRefresherService>`、`ILoggerFactory`、`INonceCatalogService`のみが必要です。

---

## 🟡 中

### 21. OcspValidationServiceの内部キャッシュがスレッドセーフでないDictionaryを使用している

**ファイル:** `Services/OcspValidationService.cs`（47行目）

```csharp
private readonly Dictionary<string, CachedOcspResponse> _cache;
```

`Dictionary<TKey, TValue>`は同時読み取りおよび書き込みに対してスレッドセーフではありません。`OcspValidationService`がシングルトンとして登録されている場合（または同じインスタンスが他のメカニズムによってリクエスト間で共有される場合）、同時OCSP検証がキャッシュを破損し、エントリの消失、例外のスロー、または古いデータの返却を引き起こす可能性があります。

**推奨事項:** `Dictionary<string, CachedOcspResponse>`を`ConcurrentDictionary<string, CachedOcspResponse>`に置き換えてください。`_cache.Remove`呼び出し（103行目）を`_cache.TryRemove`に更新してください。

---

## 🔵 低 / 情報提供

### 22. OCSP検証スタブ — 閉じた状態で失敗するが未実装

**ファイル:** `Services/OcspValidationService.cs`（157–173行目）

`PerformOcspValidationAsync`はまだスタブです。発見事項#7の修正により、動作が「常に有効」から「常に無効（fail-closed）」に正しく変更されました。ただし、このメソッドはまだ実際のOCSP実装ではありません。`EnableOcspValidation = false`（デフォルト）の限りは、本番環境への影響はありません。どの環境でもOCSPを有効にする前に、本番品質のOCSPクライアントを実装する必要があります。

---

### 23. AllowedIssuersが空のmTLSはすべてのクライアント証明書を拒否する

**ファイル:** `Models/Settings/MtlsSettings.cs`

`ValidateClientCertificateIssuer = true`（デフォルト）かつ`AllowedIssuers`が空（未設定の場合もデフォルト）の場合、`IsIssuerAllowed()`は`false`を返し、すべてのクライアント証明書が拒否されます。これは正しいfail-closedの動作ですが、目立つように文書化されていません。テンプレートを注意深く読まずにmTLSを有効にするオペレーターは、明確な説明なしにすべてのクライアント接続が拒否されることに気づく可能性があります。

**推奨事項:** `ValidateClientCertificateIssuer = true`かつ`AllowedIssuers`が空の場合、起動時に警告ログメッセージを追加してください。

---

### 24. OcspSettings.ServerUnavailableBehaviorのデフォルトが"Warn"

**ファイル:** `appsettings.template.json`（134行目）, `Services/OcspValidationService.cs`

`ServerUnavailableBehavior`設定はテンプレートでデフォルト`"Warn"`になっており、OCSPサーバーに到達できない場合にリクエストの通過を許可します。セキュリティが高い環境では、OCSPサーバーの停止が証明書失効確認を静かに低下させないよう、これを`"Fail"`にする必要があります。

**推奨事項:** テンプレートで3つのオプション（`Fail`、`Allow`、`Warn`）を明確に文書化し、最小権限の原則に合わせてデフォルトを`"Fail"`に変更することを検討してください。

---

## セキュリティヘッダー評価（現在の状態）

以下のヘッダーが`UseStandardSecurityHeaders`を通じて適用されています：

| ヘッダー | 値 | 評価 |
|---------|-----|------|
| `X-Frame-Options` | `DENY` | ✅ 良好 |
| `X-XSS-Protection` | `0` | ✅ 良好（非推奨の監査機能を無効化） |
| `X-Content-Type-Options` | `nosniff` | ✅ 良好 |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ 良好 |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ 良好 |
| `Cross-Origin-Opener-Policy` | `same-origin` | ✅ 良好 |
| `Cross-Origin-Resource-Policy` | `same-site` | ✅ 良好 |
| `Permissions-Policy` | ジオロケーション、カメラ、マイク、interest-cohort無効化 | ✅ 良好 |
| `Cache-Control` | `no-cache, no-store, must-revalidate` | ✅ 良好 |
| `Content-Security-Policy` | Nonceベース、CSP有効時に適用 | ✅ 良好 |
| `Server` | `"webserver"`にマスクされている | ✅ 良好 |
| `X-Powered-By` | 削除済み | ✅ 良好 |

---

## 総合評価

アプリケーションは前回のレビューから重大および高深刻度のすべての脆弱性を対処しました。現在の発見事項は、1件の高深刻度の設定/DI問題（発見事項#20）と低深刻度の情報提供項目に限定されています。セキュリティ態勢は大幅に改善されました。発見事項#20（NonceRefresherServiceの未使用DI依存関係）に対する即時対応を推奨します。これはデフォルト設定でアプリケーションが起動できなくなる可能性があるためです。
