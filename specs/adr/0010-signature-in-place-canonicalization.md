---
id: 0010
title: Sign the SignedInfo as it is canonicalized in place, with the ds: prefix
status: accepted
origin: implementation of the second exam (finding while testing the signature with independent verifiers)
---
# Context
`System.Security.Cryptography.Xml.SignedXml.ComputeSignature()` canonicalizes `ds:SignedInfo` **detached** from the
document. A verifier that follows XMLDSig §4.3.1 (Apache Santuario on SUNAT's side, libxmlsec1, signxml) canonicalizes it
**where it lives**: inside `ext:ExtensionContent` of an `Invoice` whose root declares `cac`, `cbc`, `ext` and `ds`.
Inclusive C14N writes those in-scope namespace declarations on the `SignedInfo` apex, so the two byte strings differ
and a signature produced the default .NET way is rejected (SUNAT 2335/2336) even though `SignedXml.CheckSignature`
accepts it. This was found by checking the C# output with libxmlsec1 and signxml, not only with .NET.

A second, smaller issue: the .NET `Signature` element redeclares the default namespace (`xmlns="…xmldsig#"`) inside a
document whose default namespace is the Invoice one; some C14N implementations (lxml) mishandle that subtree.

# Decision
`XmlDsigSigner` (Infrastructure):
1. Removes the signature placeholder, so the reference digest (enveloped transform) is computed over the document
   exactly as a verifier sees it after removing `ds:Signature` (ADR 0003).
2. Lets `SignedXml` build `SignedInfo` (C14N inclusive, enveloped transform, RSA-SHA1 by default as in research 001).
3. Writes the signature with the **`ds:` prefix** already declared on the Invoice root (as SUNAT's samples and Greenter
   do), inserts it in `ext:ExtensionContent`, and **recomputes `SignatureValue` over the in-place canonical form** of
   `SignedInfo` (its copy carrying every namespace declaration in scope).

# Verification
`ValidationAndSigningTests.Signature_value_matches_in_place_canonicalization` canonicalizes `SignedInfo` independently
(XPath node-set with its namespace nodes) and checks the RSA signature against it. Outside the test suite, the five
signed invoices were verified with libxmlsec1 and signxml, and validated with lxml against the official XSD.

# Consequences
+ The signature is valid for any spec-compliant verifier, not only for .NET.
– The signer does one extra RSA operation per document (negligible against NFR-01).
