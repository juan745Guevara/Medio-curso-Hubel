#!/usr/bin/env bash
# Regenerates Generated/*.cs from the official XSD (ADR 0005). Run from the repository root:
#   dotnet tool install --global dotnet-xscgen   # XmlSchemaClassGenerator CLI
#   bash src/Abc.Ubl/regenerate.sh
# The committed files were produced with the same options through the XmlSchemaClassGenerator library
# (commit be7676d); the CLI below is the reproducible way to do it with internet access.
set -euo pipefail
P=urn:oasis:names:specification:ubl:schema:xsd
rm -f src/Abc.Ubl/Generated/*.cs
xscgen -o src/Abc.Ubl/Generated --netCore --nullable --compactTypeNames --uniqueTypeNames --order --disableComments \
  --collectionSettersMode=PublicWithoutConstructorInitialization \
  -n "$P:Invoice-2=Abc.Ubl.Invoice" \
  -n "$P:CommonAggregateComponents-2=Abc.Ubl.Cac" \
  -n "$P:CommonBasicComponents-2=Abc.Ubl.Cbc" \
  -n "$P:CommonExtensionComponents-2=Abc.Ubl.Ext" \
  -n "$P:UnqualifiedDataTypes-2=Abc.Ubl.Udt" \
  -n "$P:QualifiedDataTypes-2=Abc.Ubl.Qdt" \
  -n "$P:CommonSignatureComponents-2=Abc.Ubl.Sig" \
  -n "$P:SignatureAggregateComponents-2=Abc.Ubl.Sac" \
  -n "$P:SignatureBasicComponents-2=Abc.Ubl.Sbc" \
  -n "urn:un:unece:uncefact:data:specification:CoreComponentTypeSchemaModule:2=Abc.Ubl.Cct" \
  -n "http://www.w3.org/2000/09/xmldsig#=Abc.Ubl.XmlDsig" \
  -n "http://uri.etsi.org/01903/v1.3.2#=Abc.Ubl.XAdES132" \
  -n "http://uri.etsi.org/01903/v1.4.1#=Abc.Ubl.XAdES141" \
  xsd/ubl-2.1/maindoc/UBL-Invoice-2.1.xsd
