using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppExperimental266.Models.Settings;

namespace WebAppExperimental266.Services
{
    /// <summary>
    /// Marker requirement used by the "YubiKeyMfa" authorization policy.
    /// A request satisfies this requirement only when a valid, non-revoked
    /// client certificate — issued by the OpenBSD admin CA and (optionally)
    /// generated on a genuine YubiKey — is present on the TLS connection.
    /// </summary>
    public class YubiKeyRequirement : IAuthorizationRequirement { }

    /// <summary>
    /// Authorization handler that enforces YubiKey PIV certificate presence and
    /// validity as a second factor after the primary identity provider login.
    ///
    /// Validation steps performed on every protected request:
    ///   1. A client certificate must be present on the TLS connection.
    ///   2. The certificate issuer must be in <see cref="YubiKeySettings.AllowedCaIssuers"/>.
    ///   3. When <see cref="YubiKeySettings.AdminCaThumbprints"/> is non-empty the
    ///      issuer-CA thumbprint embedded in the certificate chain must match.
    ///   4. When <see cref="YubiKeySettings.RequireYubiKeyAttestation"/> is true the
    ///      certificate issuer must contain the expected YubiKey attestation CA string.
    ///   5. The OCSP responder (operated on the OpenBSD host) must confirm the
    ///      certificate has not been revoked.  Step 5 may be skipped in Development
    ///      when <see cref="YubiKeySettings.SkipOcspInDevelopment"/> is true.
    /// </summary>
    public class YubiKeyRequirementHandler : AuthorizationHandler<YubiKeyRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly YubiKeySettings _settings;
        private readonly IOcspValidationService _ocspService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<YubiKeyRequirementHandler> _logger;

        public YubiKeyRequirementHandler(
            IHttpContextAccessor httpContextAccessor,
            YubiKeySettings settings,
            IOcspValidationService ocspService,
            IWebHostEnvironment env,
            ILogger<YubiKeyRequirementHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _settings = settings;
            _ocspService = ocspService;
            _env = env;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            YubiKeyRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var cert = httpContext?.Connection.ClientCertificate;

            // Step 1 — certificate must be present
            if (cert == null)
            {
                _logger.LogWarning("YubiKey MFA: No client certificate present on the connection");
                context.Fail(new AuthorizationFailureReason(this, "A YubiKey client certificate is required"));
                return;
            }

            // Step 2 — issuer must be the admin CA
            if (_settings.AllowedCaIssuers.Count > 0 && !_settings.IsIssuerAllowed(cert.Issuer))
            {
                _logger.LogError(
                    "YubiKey MFA: Certificate issuer '{Issuer}' is not in the allowed admin CA list. Allowed: [{Allowed}]",
                    cert.Issuer,
                    string.Join(", ", _settings.AllowedCaIssuers));
                context.Fail(new AuthorizationFailureReason(this, "Certificate issuer is not a trusted admin CA"));
                return;
            }

            // Step 3 — issuer-CA thumbprint check (belt-and-suspenders)
            if (_settings.AdminCaThumbprints.Count > 0)
            {
                // Walk the certificate chain to find the issuing CA thumbprint.
                // ChainElements[0] is the end-entity certificate itself; we skip it
                // and only test intermediate / root CA elements (index >= 1).
                // RevocationMode is intentionally set to NoCheck here: the
                // authoritative revocation check is performed in Step 5 via the
                // OCSP responder.  Building the chain with online revocation would
                // duplicate that check and add unnecessary latency.
                using var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
                chain.ChainPolicy.RevocationMode =
                    System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                chain.Build(cert);

                bool thumbprintMatched = false;
                for (int i = 1; i < chain.ChainElements.Count; i++)
                {
                    var element = chain.ChainElements[i];
                    if (element.Certificate.Thumbprint != null &&
                        _settings.IsThumbprintAllowed(element.Certificate.Thumbprint))
                    {
                        thumbprintMatched = true;
                        break;
                    }
                }

                if (!thumbprintMatched)
                {
                    _logger.LogError(
                        "YubiKey MFA: No CA certificate in the chain matched an allowed admin CA thumbprint for '{Subject}'",
                        cert.Subject);
                    context.Fail(new AuthorizationFailureReason(this, "Certificate chain does not include a trusted admin CA"));
                    return;
                }
            }

            // Step 4 — optional YubiKey attestation issuer check.
            //
            // This check verifies that the certificate was generated on genuine YubiKey
            // hardware by inspecting whether any certificate in the TLS client-certificate
            // chain was issued by the configured YubiKey attestation CA.  This is the
            // correct approach when the YubiKey attestation certificate (slot f9, signed
            // by Yubico) is included in the presented chain.
            //
            // IMPORTANT: When the standard admin-CA workflow is used (the admin re-signs
            // the CSR so the leaf cert's issuer is the admin CA, not Yubico), set
            // RequireYubiKeyAttestation = false.  Attestation in that workflow must be
            // performed out-of-band during certificate issuance, not at request time.
            // See docs/YUBIKEY_OPENBSD_ADMIN_GUIDE.md for both workflows.
            if (_settings.RequireYubiKeyAttestation &&
                !string.IsNullOrEmpty(_settings.YubiKeyAttestationIssuer))
            {
                // Build the chain (if not already built above) and scan every element's
                // issuer for the attestation CA fragment — including the leaf itself,
                // because the cert may be directly signed by the Yubico attestation CA.
                // RevocationMode is intentionally set to NoCheck here: the
                // authoritative revocation check is performed in Step 5 via the
                // OCSP responder.
                using var attestChain = new System.Security.Cryptography.X509Certificates.X509Chain();
                attestChain.ChainPolicy.RevocationMode =
                    System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                attestChain.Build(cert);

                bool attestationFound = attestChain.ChainElements.Cast<
                    System.Security.Cryptography.X509Certificates.X509ChainElement>()
                    .Any(e => e.Certificate.Issuer.Contains(
                        _settings.YubiKeyAttestationIssuer,
                        StringComparison.OrdinalIgnoreCase));

                if (!attestationFound)
                {
                    _logger.LogError(
                        "YubiKey MFA: Certificate chain for '{Subject}' did not contain a YubiKey attestation issuer " +
                        "(expected issuer fragment '{Expected}'). " +
                        "Ensure the Yubico attestation certificate is included in the TLS chain, " +
                        "or set RequireYubiKeyAttestation=false if using the admin-CA re-sign workflow.",
                        cert.Subject,
                        _settings.YubiKeyAttestationIssuer);
                    context.Fail(new AuthorizationFailureReason(this,
                        "Certificate attestation check failed — no YubiKey attestation CA found in the certificate chain"));
                    return;
                }
            }

            // Step 5 — OCSP revocation check
            bool skipOcsp = _settings.SkipOcspInDevelopment && _env.IsDevelopment();
            if (skipOcsp)
            {
                _logger.LogInformation(
                    "YubiKey MFA: Skipping OCSP revocation check in Development for '{Subject}'",
                    cert.Subject);
            }
            else
            {
                bool isValid = await _ocspService.ValidateCertificateAsync(cert);
                if (!isValid)
                {
                    _logger.LogError(
                        "YubiKey MFA: OCSP check failed (certificate revoked or responder unreachable) for '{Subject}'",
                        cert.Subject);
                    context.Fail(new AuthorizationFailureReason(this,
                        "Certificate has been revoked or could not be validated by the OCSP responder"));
                    return;
                }
            }

            _logger.LogInformation(
                "YubiKey MFA: Certificate validated successfully for '{Subject}'",
                cert.Subject);
            context.Succeed(requirement);
        }
    }
}
