namespace WebAppExperimental266.Models.Settings
{
    /// <summary>
    /// Configuration settings for YubiKey PIV certificate MFA at the application level.
    /// The OpenBSD administrator acts as the Certificate Authority: they issue a signed
    /// X.509 certificate into each user's YubiKey PIV slot and can revoke access at any
    /// time by revoking the certificate at the CA (reflected immediately via OCSP).
    /// </summary>
    public class YubiKeySettings
    {
        /// <summary>
        /// One or more issuer distinguished-name substrings that a trusted admin CA
        /// certificate must match (case-insensitive contains check).
        /// Example: ["CN=OpenBSD Admin CA, O=MyOrg"]
        /// </summary>
        public List<string> AllowedCaIssuers { get; set; } = new List<string>();

        /// <summary>
        /// Optional list of SHA-256 thumbprints (hex, no separators) of the admin CA
        /// root or intermediate certificates.  When non-empty, the client certificate's
        /// issuer thumbprint must appear in this list as a belt-and-suspenders check on
        /// top of the issuer DN match.
        /// Example: ["A1B2C3D4E5F6..."]
        /// </summary>
        public List<string> AdminCaThumbprints { get; set; } = new List<string>();

        /// <summary>
        /// When true, the handler additionally verifies that the TLS client-certificate
        /// chain contains a certificate whose issuer DN includes the value of
        /// <see cref="YubiKeyAttestationIssuer"/>.  This is satisfied when the YubiKey
        /// attestation certificate (exported from PIV slot f9, signed by the Yubico
        /// attestation root) is included in the chain presented during the TLS handshake.
        ///
        /// Set to <c>false</c> (the default) when using the standard admin-CA workflow
        /// where the administrator re-signs the user's CSR: in that case the leaf
        /// certificate's issuer is the admin CA, not Yubico, and attestation must be
        /// verified out-of-band during certificate issuance instead.
        /// See <c>docs/YUBIKEY_OPENBSD_ADMIN_GUIDE.md</c> for both workflows.
        /// </summary>
        public bool RequireYubiKeyAttestation { get; set; } = false;

        /// <summary>
        /// The issuer DN fragment to match when <see cref="RequireYubiKeyAttestation"/>
        /// is true.  For standard Yubico-issued attestation this is
        /// "Yubico PIV Attestation".  For enterprise-specific attestation CAs, supply
        /// the relevant DN substring.
        /// </summary>
        public string? YubiKeyAttestationIssuer { get; set; }

        /// <summary>
        /// Skip the OCSP revocation check when the application is running in the
        /// Development environment.  Useful for local workstations that do not have
        /// network access to the OpenBSD OCSP responder.
        /// </summary>
        public bool SkipOcspInDevelopment { get; set; } = true;

        /// <summary>
        /// Returns true when <paramref name="issuer"/> is permitted under the current
        /// configuration.  Always returns true when <see cref="AllowedCaIssuers"/> is
        /// empty (no issuer restriction).
        /// </summary>
        public bool IsIssuerAllowed(string issuer)
        {
            if (AllowedCaIssuers == null || AllowedCaIssuers.Count == 0)
                return true;

            return AllowedCaIssuers.Any(allowed =>
                issuer.Contains(allowed, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns true when <paramref name="thumbprint"/> (hex, upper- or lower-case,
        /// no separators) appears in <see cref="AdminCaThumbprints"/>.  Always returns
        /// true when the list is empty.
        /// </summary>
        public bool IsThumbprintAllowed(string thumbprint)
        {
            if (AdminCaThumbprints == null || AdminCaThumbprints.Count == 0)
                return true;

            return AdminCaThumbprints.Any(t =>
                string.Equals(t.Replace(":", "").Replace(" ", ""),
                              thumbprint.Replace(":", "").Replace(" ", ""),
                              StringComparison.OrdinalIgnoreCase));
        }
    }
}
