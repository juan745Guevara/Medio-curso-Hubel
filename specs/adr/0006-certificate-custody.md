---
id: 0006
title: Custody of the digital certificate (.pfx) used to sign
status: accepted
origin: problem statement
---
# Context
Every Electronic document is signed with the Issuer's Digital certificate (FR-09). Whoever holds the private key can issue documents in ABC's name, so its custody is a security decision, not an implementation detail. NFR-05 requires it to be encrypted and never appear in logs or responses; constitution 15 forbids versioning it. Pending clarification: own certificate or a PSE's (spec 001).

# Decision
1. **Single location: the server.** Signing happens only in the monolith (`SignerPort`). The Windows and Android apps never receive, store or use the certificate.
2. **Who:** only the Administrator role loads or replaces it, through `PUT /configuration/certificate` over HTTPS. Every load, replacement and failed attempt is audited (user, date, thumbprint; never the file or the password).
3. **Validation on load:** the PKCS#12 opens with the password, contains the private key, the RUC in the subject matches the Issuer's RUC, it is within its validity period and allows digital signature. Otherwise `422 INVALID_CERTIFICATE`. The Administrator must also register the certificate in SUNAT Operaciones en Línea (SOL) before using it in production.
4. **Storage (`CertificateStorePort`):**
   - Initial adapter: the .pfx is encrypted with ASP.NET Core Data Protection (AES-256 + HMAC) and stored in its own table; the key ring lives outside the database, protected by a key-encryption certificate of the server's operating system. The upload password is not kept in clear text.
   - The private key is loaded into memory only to sign (`X509KeyStorageFlags.EphemeralKeySet`), never written to a temporary file.
   - Future adapter: HSM or cloud key vault (Azure Key Vault / AWS KMS) where the key cannot be exported and signing is remote; the port does not change.
5. **Rotation:** alert 30 days before expiry (security.md). The new certificate is loaded before the old one expires; the old public certificate is kept to verify signatures for 5 years (NFR-06), its private key is deleted.
6. **Compromise:** disable signing (`CERTIFICATE_REVOKED`), revoke it with the certification authority, remove it from SOL, load a new one; documents already accepted remain valid.
7. **Backups:** database backups and key-ring backups are stored separately, so that a stolen database backup does not expose the key.

# Alternatives evaluated
| Option | Result |
|---|---|
| .pfx on each POS computer or phone | Rejected: N copies of the key, impossible to revoke and audit |
| .pfx as a plain file on the server with the password in `appsettings.json` | Rejected: violates NFR-05 and constitution 15 |
| HSM or cloud key vault from day 1 | Deferred: best protection, but extra cost; the port allows moving to it without changing the domain |
| Delegate signing to a PSE/OSE | Depends on the open clarification; it would be another `SignerPort` adapter |

# Consequences
+ The key exists in one place, encrypted, with an audit trail and a rotation procedure.
+ Clients cannot leak it because they never have it.
– The key ring and its backup must be operated with care; losing it means loading the certificate again.
