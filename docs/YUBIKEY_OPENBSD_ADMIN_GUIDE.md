# YubiKey + OpenBSD Admin CA — Administrator Guide

This document describes how an OpenBSD system administrator sets up the
Certificate Authority (CA), provisions user YubiKeys, and revokes access when
required.  The web application trusts only certificates signed by this CA, so
the administrator exclusively controls who can log in.

---

## Architecture overview

```
OpenBSD host (admin workstation / server)
  └─ LibreSSL / OpenSSL CA
       ├─ Issues X.509 certificates → loaded onto user YubiKeys
       ├─ Runs OCSP responder → queried by the web app on every protected request
       └─ Revokes certificates  → takes effect on the next OCSP query (≤ cache TTL)

User YubiKey (PIV applet, slot 9a)
  └─ Holds private key (never exportable) + CA-signed certificate
       └─ Presented as TLS client certificate when the browser connects

Web application (ASP.NET Core)
  └─ Kestrel: AllowCertificate / RequireCertificate (TLS layer)
  └─ YubiKeyRequirementHandler: verifies issuer DN, optional thumbprint, OCSP
  └─ "YubiKeyMfa" authorization policy: applied to /Experimental folder
```

---

## 1. Set up the admin CA on OpenBSD

All commands are run as root (or with `doas`) on the OpenBSD host.

### 1a. Create the CA directory structure

```sh
mkdir -p /etc/ssl/adminCA/{private,certs,crl,newcerts}
chmod 700 /etc/ssl/adminCA/private
echo "01" > /etc/ssl/adminCA/serial
touch /etc/ssl/adminCA/index.txt
```

### 1b. Create `openssl.cnf`

```ini
# /etc/ssl/adminCA/openssl.cnf
[ ca ]
default_ca = CA_default

[ CA_default ]
dir               = /etc/ssl/adminCA
certs             = $dir/certs
crl_dir           = $dir/crl
database          = $dir/index.txt
new_certs_dir     = $dir/newcerts
certificate       = $dir/ca.crt
serial            = $dir/serial
crlnumber         = $dir/crlnumber
crl               = $dir/crl.pem
private_key       = $dir/private/ca.key
RANDFILE          = $dir/private/.rand
default_days      = 365
default_crl_days  = 30
default_md        = sha256
preserve          = no
policy            = policy_strict

[ policy_strict ]
countryName             = optional
stateOrProvinceName     = optional
organizationName        = optional
organizationalUnitName  = optional
commonName              = supplied
emailAddress            = optional

[ req ]
default_bits       = 4096
default_md         = sha256
distinguished_name = req_distinguished_name
x509_extensions    = v3_ca
prompt             = no

[ req_distinguished_name ]
CN = OpenBSD Admin CA
O  = MyOrganization

[ v3_ca ]
subjectKeyIdentifier   = hash
authorityKeyIdentifier = keyid:always,issuer
basicConstraints       = critical,CA:true
keyUsage               = critical,keyCertSign,cRLSign

[ v3_client ]
subjectKeyIdentifier   = hash
authorityKeyIdentifier = keyid,issuer
basicConstraints       = CA:FALSE
keyUsage               = critical,digitalSignature
extendedKeyUsage       = clientAuth

[ v3_ocsp ]
basicConstraints       = CA:FALSE
keyUsage               = critical,digitalSignature
extendedKeyUsage       = OCSPSigning
```

### 1c. Generate the CA key and self-signed root certificate

```sh
openssl genrsa -aes256 -out /etc/ssl/adminCA/private/ca.key 4096
chmod 400 /etc/ssl/adminCA/private/ca.key

openssl req -new -x509 -days 3650 \
    -key /etc/ssl/adminCA/private/ca.key \
    -out /etc/ssl/adminCA/ca.crt \
    -config /etc/ssl/adminCA/openssl.cnf \
    -extensions v3_ca
```

The CA certificate subject (`CN=OpenBSD Admin CA, O=MyOrganization`) becomes the
value you enter for **`YubiKeySettings:AllowedCaIssuers`** and
**`MtlsSettings:AllowedIssuers`** in the web application configuration.

To display the thumbprint for `YubiKeySettings:AdminCaThumbprints`:

```sh
openssl x509 -in /etc/ssl/adminCA/ca.crt -noout -fingerprint -sha256 \
    | sed 's/://g' | awk -F= '{print $2}'
```

---

## 2. Provision a user YubiKey

Install the required tools:

```sh
pkg_add yubikey-manager py3-yubikey-manager
```

Repeat the following steps for each user.

### 2a. Generate the key pair on the YubiKey (PIV slot 9a)

The private key is generated inside the YubiKey and is **never exportable**.

```sh
ykman piv keys generate \
    --algorithm ECCP256 \
    --pin-policy ALWAYS \
    --touch-policy ALWAYS \
    9a /tmp/user_alice_pub.pem
```

`--pin-policy ALWAYS` requires the user to enter their YubiKey PIN on every use.  
`--touch-policy ALWAYS` requires a physical tap of the YubiKey on every use.

### 2b. Create a Certificate Signing Request (CSR)

```sh
ykman piv certificates request \
    --subject "CN=alice,O=MyOrganization" \
    9a /tmp/user_alice_pub.pem /tmp/user_alice.csr
```

Replace `alice` with the user's username.  The `CN` value will appear in the
certificate `Subject` and can be read by the application as a user identifier.

### 2c. Sign the CSR with the admin CA

```sh
openssl ca \
    -config /etc/ssl/adminCA/openssl.cnf \
    -extensions v3_client \
    -days 365 \
    -in /tmp/user_alice.csr \
    -out /tmp/user_alice.crt \
    -notext
```

### 2d. Import the signed certificate back onto the YubiKey

```sh
ykman piv certificates import 9a /tmp/user_alice.crt
```

The user can now authenticate to the web application.  The private key never
left the YubiKey hardware.

### 2e. Clean up temporary files

```sh
rm /tmp/user_alice_pub.pem /tmp/user_alice.csr /tmp/user_alice.crt
```

---

## 3. Run the OCSP responder on the OpenBSD host

The web application queries the OCSP responder on every protected request to
check whether the certificate has been revoked.  Set `OcspSettings:OcspServerUrl`
in the application configuration to point at this service.

### 3a. Generate an OCSP signing key and certificate

```sh
openssl genrsa -out /etc/ssl/adminCA/private/ocsp.key 2048
chmod 400 /etc/ssl/adminCA/private/ocsp.key

openssl req -new \
    -key /etc/ssl/adminCA/private/ocsp.key \
    -out /tmp/ocsp.csr \
    -subj "/CN=OCSP Responder/O=MyOrganization"

openssl ca \
    -config /etc/ssl/adminCA/openssl.cnf \
    -extensions v3_ocsp \
    -days 365 \
    -in /tmp/ocsp.csr \
    -out /etc/ssl/adminCA/ocsp.crt \
    -notext

rm /tmp/ocsp.csr
```

### 3b. Start the OCSP responder

```sh
openssl ocsp \
    -index /etc/ssl/adminCA/index.txt \
    -port 2560 \
    -rsigner /etc/ssl/adminCA/ocsp.crt \
    -rkey /etc/ssl/adminCA/private/ocsp.key \
    -CA /etc/ssl/adminCA/ca.crt \
    -text &
```

For a persistent service, create an `/etc/rc.d/ocspd` script or run via `cron`
to restart on reboot.

Set the following in `appsettings.json` (or User Secrets):

```json
"OcspSettings": {
  "EnableOcspValidation": true,
  "OcspServerUrl": "http://<openbsd-host-ip>:2560",
  "ServerUnavailableBehavior": "Fail"
}
```

Also set `EnableYubiKeyRequired: true` in `FeatureFlags`.

---

## 4. Revoke a user's access

Revoking a certificate takes effect on the next OCSP query by the web
application (within the `OcspSettings:CacheDurationMinutes` window, default 60 min).

```sh
# Find the certificate serial number
openssl ca -config /etc/ssl/adminCA/openssl.cnf -status <serial>

# Revoke the certificate
openssl ca \
    -config /etc/ssl/adminCA/openssl.cnf \
    -revoke /etc/ssl/adminCA/newcerts/<serial>.pem

# Update the CRL (optional — OCSP uses index.txt directly)
openssl ca \
    -config /etc/ssl/adminCA/openssl.cnf \
    -gencrl \
    -out /etc/ssl/adminCA/crl.pem
```

The OCSP responder reads `index.txt` on every query, so revocation is
effective immediately after the command above; no responder restart is needed.

To reduce the window further, lower `OcspSettings:CacheDurationMinutes` in the
application configuration.

---

## 5. Verify a user certificate (admin check)

```sh
openssl verify \
    -CAfile /etc/ssl/adminCA/ca.crt \
    /tmp/user_alice.crt

# Check OCSP status manually
openssl ocsp \
    -issuer /etc/ssl/adminCA/ca.crt \
    -cert /tmp/user_alice.crt \
    -url http://localhost:2560 \
    -resp_text
```

---

## 6. Application configuration summary

| Setting | Value |
|---|---|
| `FeatureFlags:EnableYubiKeyRequired` | `true` |
| `FeatureFlags:EnableMtls` | `true` (to require cert at TLS layer) |
| `YubiKeySettings:AllowedCaIssuers` | `["CN=OpenBSD Admin CA, O=MyOrganization"]` |
| `YubiKeySettings:SkipOcspInDevelopment` | `true` (dev) / `false` (prod) |
| `MtlsSettings:AllowedIssuers` | `["CN=OpenBSD Admin CA, O=MyOrganization"]` |
| `OcspSettings:EnableOcspValidation` | `true` |
| `OcspSettings:OcspServerUrl` | `http://<openbsd-host>:2560` |
| `OcspSettings:ServerUnavailableBehavior` | `Fail` (recommended) |

---

## 7. YubiKey attestation (optional, advanced)

YubiKey attestation proves that a key pair was generated inside genuine YubiKey
hardware.  The application supports two workflows; choose the one appropriate
for your security posture.

### Workflow A — Yubico-signed attestation chain (online check)

In this workflow the certificate presented as the TLS client certificate is the
YubiKey attestation certificate signed by the Yubico attestation root.  The
handler checks that any certificate in the presented chain has an issuer
containing `YubiKeyAttestationIssuer` (default `"Yubico PIV Attestation"`).

```sh
# Export the attestation certificate for slot 9a (signed by Yubico)
ykman piv attest 9a /tmp/user_alice_attest.crt

# Import it into slot 9a so it is presented as the TLS client cert
ykman piv certificates import 9a /tmp/user_alice_attest.crt
```

Set `RequireYubiKeyAttestation: true` and `YubiKeyAttestationIssuer: "Yubico PIV Attestation"`.

Note: in this workflow the TLS client cert is issued by Yubico, not the admin CA.
You will want to leave `AllowedCaIssuers` empty or configure it with the Yubico
attestation CA issuer instead of the admin CA.

### Workflow B — Admin-CA re-signed certificate (recommended for most deployments)

In this workflow the admin CA signs the user certificate (as described in sections
2a–2d above).  The resulting cert's issuer is the admin CA, not Yubico.
Attestation is verified **out-of-band** during certificate issuance:

1. Ask the user to run `ykman piv attest 9a /tmp/attest.crt` and send you the
   attestation cert.
2. Verify it against Yubico's root CA: `openssl verify -CAfile yubico_root.crt /tmp/attest.crt`
3. Proceed with the CSR → sign → import workflow in sections 2b–2d only if
   attestation passes.

Set `RequireYubiKeyAttestation: false` (the default) for this workflow.
The handler does not perform any at-request-time attestation check; trust is
established during issuance and enforced via the admin CA issuer and OCSP.
