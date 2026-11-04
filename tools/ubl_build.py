"""Builds the reference UBL 2.1 / SUNAT XML of specs/examples/xml/ from the JSON examples.

Same technique as ADR 0005, in Python so it can run here: classes generated from the official XSDs
(xsdata, the Python counterpart of XmlSchemaClassGenerator) → serializer → XSD validation → XMLDSig signature
(ADR 0003 order). No templates and no string concatenation: element order and cardinality come from the XSD.

The signing key is a throw-away self-signed test certificate created in memory on every run and never written
to disk (constitution 15); the public certificate travels inside each signed file so anyone can verify it.

Usage:  python tools/ubl_build.py            (rebuilds every file listed in specs/examples/xml/cases.json)
"""
from __future__ import annotations

import datetime as dt
import hashlib
import importlib
import json
import subprocess
import sys
import types
import typing
from dataclasses import fields, is_dataclass
from decimal import Decimal
from pathlib import Path

from lxml import etree

sys.path.insert(0, str(Path(__file__).resolve().parent))
from sunat_calc import (ICBPER_RATE, IGV_RATE, ROOT, Totals, amount_in_words, calculate, fmt,  # noqa: E402
                        load_products, r2)

GEN = ROOT / "tools" / ".generated"
XSD = ROOT / "xsd"
EX = ROOT / "specs" / "examples"
NS = {
    "cac": "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2",
    "cbc": "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2",
    "ext": "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2",
    "ds": "http://www.w3.org/2000/09/xmldsig#",
    "sac": "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1",
}
ROOT_NS = {
    "Invoice": "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2",
    "CreditNote": "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2",
    "DebitNote": "urn:oasis:names:specification:ubl:schema:xsd:DebitNote-2",
    "SummaryDocuments": "urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1",
    "VoidedDocuments": "urn:sunat:names:specification:ubl:peru:schema:xsd:VoidedDocuments-1",
}
XSD_FOR = {
    "Invoice": XSD / "ubl-2.1/maindoc/UBL-Invoice-2.1.xsd",
    "CreditNote": XSD / "ubl-2.1/maindoc/UBL-CreditNote-2.1.xsd",
    "DebitNote": XSD / "ubl-2.1/maindoc/UBL-DebitNote-2.1.xsd",
    "SummaryDocuments": XSD / "sunat/maindoc/UBL-SummaryDocuments-2.0.xsd",
    "VoidedDocuments": XSD / "sunat/maindoc/UBL-VoidedDocuments-2.0.xsd",
}
SUNAT_URN = "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo"
ISSUER = json.loads((EX / "xml" / "issuer.json").read_text(encoding="utf-8"))


# --- 1. Classes generated from the XSD ----------------------------------------------------------------
def ensure_generated() -> None:
    stamp = hashlib.sha256((XSD / "MANIFEST.sha256").read_bytes()).hexdigest()
    marker = GEN / "STAMP"
    if marker.exists() and marker.read_text() == stamp:
        return
    GEN.mkdir(parents=True, exist_ok=True)
    for pkg, folder in (("ubl21", XSD / "ubl-2.1/maindoc"), ("sunat20", XSD / "sunat/maindoc")):
        subprocess.run(["xsdata", "generate", str(folder), "--package", pkg, "--structure-style", "namespaces"],
                       cwd=GEN, check=True, capture_output=True)
    marker.write_text(stamp)


def root_class(name: str):
    ensure_generated()
    if str(GEN) not in sys.path:
        sys.path.insert(0, str(GEN))
    module = {
        "Invoice": "ubl21.oasis.names.specification.ubl.schema.xsd.invoice_2",
        "CreditNote": "ubl21.oasis.names.specification.ubl.schema.xsd.credit_note_2",
        "DebitNote": "ubl21.oasis.names.specification.ubl.schema.xsd.debit_note_2",
        "SummaryDocuments": "sunat20.sunat.names.specification.ubl.peru.schema.xsd.summary_documents_1",
        "VoidedDocuments": "sunat20.sunat.names.specification.ubl.peru.schema.xsd.voided_documents_1",
    }[name]
    return getattr(importlib.import_module(module), name)


# --- 2. Filling the generated classes from plain dicts --------------------------------------------------
def _unwrap(tp):
    origin = typing.get_origin(tp)
    if origin in (list, typing.List):
        return _unwrap(typing.get_args(tp)[0])[0], True
    if origin is typing.Union or origin is types.UnionType:
        args = [a for a in typing.get_args(tp) if a is not type(None)]
        return _unwrap(args[0])[0], False
    return tp, False


def _scalar(tp, v):
    from xsdata.models.datatype import XmlDate, XmlTime
    if tp is XmlDate:
        return XmlDate.from_string(str(v))
    if tp is XmlTime:
        return XmlTime.from_string(str(v))
    if tp is Decimal:
        return Decimal(str(v))
    if tp is bool:
        return bool(v)
    if tp is int:
        return int(v)
    return v if not isinstance(v, Decimal) else fmt(v)


def make(cls, spec):
    """make(cls, {"field": value | dict | list}) → instance. Scalars fill the 'value' of simple components."""
    if not is_dataclass(cls):
        return _scalar(cls, spec)
    if not isinstance(spec, dict):
        spec = {"value": spec}
    hints = typing.get_type_hints(cls)
    names = {f.name for f in fields(cls)}
    kwargs = {}
    for key, val in spec.items():
        if key not in names:
            raise KeyError(f"{cls.__name__} has no field {key!r}")
        tp, is_list = _unwrap(hints[key])
        if tp is object:          # xsd:any (ExtensionContent)
            kwargs[key] = val
        elif is_list:
            kwargs[key] = [make(tp, v) for v in (val if isinstance(val, list) else [val])]
        else:
            kwargs[key] = make(tp, val)
    return cls(**kwargs)


def amt(x: Decimal, cur: str) -> dict:
    return {"value": r2(x), "currency_id": cur}


def code(value: str, catalog: str, list_name: str, **extra) -> dict:
    return {"value": value, "list_agency_name": "PE:SUNAT", "list_name": list_name, "list_uri": f"{SUNAT_URN}{catalog}", **extra}


def tax_scheme(code05: str) -> dict:
    cat = {c["code"]: c for c in json.loads((ROOT / "specs/catalogs/catalog-05.json").read_text(encoding="utf-8"))["codes"]}[code05]
    return {"id": {"value": code05, "scheme_name": "Codigo de tributos", "scheme_agency_name": "PE:SUNAT",
                   "scheme_uri": f"{SUNAT_URN}05"},
            "name": cat["name"], "tax_type_code": cat["un5153"]}


# --- 3. Mapper: domain data → UBL classes (what UblMapper does in .NET) --------------------------------
PLACEHOLDER = "{urn:abc:signature-placeholder}SignaturePlaceholder"


def signature_parts(any_field: str = "other_element") -> tuple[dict, dict]:
    """ExtensionContent must hold one element. Before signing it holds a neutral placeholder from a namespace
    the XSD does not know (xsd:any lax → not validated), so the unsigned XML can be validated (ADR 0003);
    sign() swaps it for the ds:Signature."""
    from xsdata.formats.dataclass.models.generics import AnyElement
    ext = {"ublextension": [{"extension_content": {any_field: AnyElement(qname=PLACEHOLDER)}}]}
    sig = {"id": "IDSignABC",
           "signatory_party": {"party_identification": [{"id": ISSUER["ruc"]}], "party_name": [{"name": ISSUER["name"]}]},
           "digital_signature_attachment": {"external_reference": {"uri": "#SignABC"}}}
    return ext, sig


def supplier_party() -> dict:
    a = ISSUER["address"]
    return {"party": {
        "party_identification": [{"id": {"value": ISSUER["ruc"], "scheme_id": "6", "scheme_name": "Documento de Identidad",
                                         "scheme_agency_name": "PE:SUNAT", "scheme_uri": f"{SUNAT_URN}06"}}],
        "party_name": [{"name": ISSUER["tradeName"]}],
        "party_legal_entity": [{"registration_name": ISSUER["name"], "registration_address": {
            "id": {"value": a["ubigeo"], "scheme_agency_name": "PE:INEI", "scheme_name": "Ubigeos"},
            "address_type_code": {"value": ISSUER["establishmentCode"], "list_agency_name": "PE:SUNAT",
                                  "list_name": "Establecimientos anexos"},
            "city_name": a["province"], "country_subentity": a["department"], "district": a["district"],
            "address_line": [{"line": a["line"]}],
            "country": {"identification_code": {"value": "PE", "list_id": "ISO 3166-1",
                                                "list_agency_name": "United Nations Economic Commission for Europe",
                                                "list_name": "Country"}}}}]}}


def customer_party(buyer: dict) -> dict:
    d = buyer["document"]
    party = {"party_identification": [{"id": {"value": d["number"], "scheme_id": d["type"], "scheme_name": "Documento de Identidad",
                                              "scheme_agency_name": "PE:SUNAT", "scheme_uri": f"{SUNAT_URN}06"}}],
             "party_legal_entity": [{"registration_name": buyer["name"]}]}
    if buyer.get("address"):
        party["party_legal_entity"][0]["registration_address"] = {"address_line": [{"line": buyer["address"]}]}
    return {"party": party}


def tax_total(t: Totals, cur: str) -> dict:
    subs = []
    def sub(taxable, tax, c05, **kw):
        s = {"tax_amount": amt(tax, cur), "tax_category": {"tax_scheme": tax_scheme(c05)}}
        if taxable is not None:
            s = {"taxable_amount": amt(taxable, cur), **s}
        subs.append(s)
    if t.taxed or t.igv:
        sub(t.taxed, t.igv, "1000")
    if t.exempt:
        sub(t.exempt, Decimal(0), "9997")
    if t.non_taxable:
        sub(t.non_taxable, Decimal(0), "9998")
    if t.free_value:
        sub(t.free_value, t.free_igv, "9996")
    if t.isc:
        sub(sum(l.isc_base for l in t.lines if l.isc), t.isc, "2000")
    if t.icbper:
        sub(None, t.icbper, "7152")
    return {"tax_amount": amt(t.tax_total, cur), "tax_subtotal": subs}


def line_dict(l, cur: str, qty_field: str) -> dict:
    prices = []
    if l.free:
        prices.append({"price_amount": amt(l.reference_unit_value, cur),
                       "price_type_code": code("02", "16", "Tipo de Precio")})
    else:
        prices.append({"price_amount": {"value": fmt(l.unit_price), "currency_id": cur},
                       "price_type_code": code("01", "16", "Tipo de Precio")})
    igv_pct = Decimal(18) if (l.tax_code == "1000" or l.treatment in {"11", "12", "13", "14", "15", "16"}) else Decimal(0)
    subs = [{"taxable_amount": amt(l.igv_base, cur), "tax_amount": amt(l.igv, cur),
             "tax_category": {"percent": fmt(igv_pct),
                              "tax_exemption_reason_code": code(l.treatment, "07", "Afectacion del IGV"),
                              "tax_scheme": tax_scheme(l.tax_code)}}]
    if l.isc:
        cat = {"tier_range": l.isc_system, "tax_scheme": tax_scheme("2000")}
        if l.isc_rate is not None:
            cat = {"percent": fmt(l.isc_rate), **cat}
        subs.insert(0, {"taxable_amount": amt(l.isc_base, cur), "tax_amount": amt(l.isc, cur), "tax_category": cat})
    if l.icbper:
        subs.append({"tax_amount": amt(l.icbper, cur),
                     "base_unit_measure": {"value": l.icbper_bags, "unit_code": "NIU"},
                     "tax_category": {"per_unit_amount": amt(ICBPER_RATE, cur), "tax_scheme": tax_scheme("7152")}})
    p = l.product
    item = {"description": [p["description"]], "sellers_item_identification": {"id": p["code"]}}
    if p.get("unspscCode"):
        item["commodity_classification"] = [{"item_classification_code": {
            "value": p["unspscCode"], "list_id": "UNSPSC", "list_agency_name": "GS1 US", "list_name": "Item Classification"}}]
    return {"id": str(l.order),
            qty_field: {"value": l.quantity, "unit_code": l.unit, "unit_code_list_id": "UN/ECE rec 20",
                        "unit_code_list_agency_name": "United Nations Economic Commission for Europe"},
            "line_extension_amount": amt(l.sale_value, cur),
            "pricing_reference": {"alternative_condition_price": prices},
            "tax_total": [{"tax_amount": amt(l.line_taxes if not l.free else l.igv, cur), "tax_subtotal": subs}],
            "item": item,
            "price": {"price_amount": amt(Decimal(0) if l.free else l.unit_value, cur)}}


def invoice_spec(req: dict, case: dict, t: Totals) -> dict:
    cur = req["currency"]
    ext, sig = signature_parts()
    notes = [{"value": amount_in_words(t.payable, cur), "language_locale_id": "1000"}]
    if t.free_value:
        notes.append({"value": "TRANSFERENCIA GRATUITA DE UN BIEN Y/O SERVICIO PRESTADO GRATUITAMENTE", "language_locale_id": "1002"})
    spec = {
        "ublextensions": ext, "ublversion_id": "2.1",
        "customization_id": {"value": "2.0", "scheme_agency_name": "PE:SUNAT"},
        "id": f"{req['series']}-{case['number']}", "issue_date": req["issueDate"], "issue_time": case["issueTime"],
        "invoice_type_code": code(req["type"], "01", "Tipo de Documento", list_id=req["operationType"],
                                  name="Tipo de Operacion", list_scheme_uri=f"{SUNAT_URN}51"),
        "note": notes,
        "document_currency_code": {"value": cur, "list_id": "ISO 4217 Alpha", "list_name": "Currency",
                                   "list_agency_name": "United Nations Economic Commission for Europe"},
        "signature": [sig], "accounting_supplier_party": supplier_party(),
        "accounting_customer_party": customer_party(req["buyer"]),
    }
    pt = req["paymentTerms"]
    if pt["type"] == "CASH":
        spec["payment_terms"] = [{"id": "FormaPago", "payment_means_id": ["Contado"]}]
    else:
        spec["due_date"] = pt["installments"][-1]["dueDate"]
        spec["payment_terms"] = [{"id": "FormaPago", "payment_means_id": ["Credito"], "amount": amt(Decimal(pt["outstandingAmount"]), cur)}] + [
            {"id": "FormaPago", "payment_means_id": [f"Cuota{i:03d}"], "amount": amt(Decimal(q["amount"]), cur), "payment_due_date": q["dueDate"]}
            for i, q in enumerate(pt["installments"], start=1)]
    if cur == "USD":
        spec["payment_exchange_rate"] = {"source_currency_code": "USD", "target_currency_code": "PEN",
                                         "calculation_rate": req["exchangeRate"], "date": req["issueDate"]}
    spec["tax_total"] = [tax_total(t, cur)]
    spec["legal_monetary_total"] = {"line_extension_amount": amt(t.line_extension, cur),
                                    "tax_inclusive_amount": amt(t.tax_inclusive, cur), "payable_amount": amt(t.payable, cur)}
    spec["invoice_line"] = [line_dict(l, cur, "invoiced_quantity") for l in t.lines]
    return spec


def note_spec(req: dict, case: dict, t: Totals) -> dict:
    cur = case.get("currency", "PEN")
    ext, sig = signature_parts()
    credit = req["type"] == "07"
    mod = req["modifiedDocument"]
    spec = {
        "ublextensions": ext, "ublversion_id": "2.1",
        "customization_id": {"value": "2.0", "scheme_agency_name": "PE:SUNAT"},
        "id": f"{req['series']}-{case['number']}", "issue_date": case["issueDate"], "issue_time": case["issueTime"],
        "note": [{"value": amount_in_words(t.payable, cur), "language_locale_id": "1000"}],
        "document_currency_code": {"value": cur, "list_id": "ISO 4217 Alpha", "list_name": "Currency",
                                   "list_agency_name": "United Nations Economic Commission for Europe"},
        "discrepancy_response": [{"reference_id": mod["number"],
                                  "response_code": code(req["reason"]["code"], "09" if credit else "10",
                                                        "Tipo de nota de credito" if credit else "Tipo de nota de debito"),
                                  "description": [req["reason"]["description"]]}],
        "billing_reference": [{"invoice_document_reference": {"id": mod["number"],
                                                              "document_type_code": code(mod["type"], "01", "Tipo de Documento")}}],
        "signature": [sig], "accounting_supplier_party": supplier_party(),
        "accounting_customer_party": customer_party(case["buyer"]),
        "tax_total": [tax_total(t, cur)],
    }
    totals = {"line_extension_amount": amt(t.line_extension, cur), "tax_inclusive_amount": amt(t.tax_inclusive, cur),
              "payable_amount": amt(t.payable, cur)}
    if credit:
        spec["legal_monetary_total"] = totals
        spec["credit_note_line"] = [line_dict(l, cur, "credited_quantity") for l in t.lines]
    else:
        spec["requested_monetary_total"] = totals
        spec["debit_note_line"] = [line_dict(l, cur, "debited_quantity") for l in t.lines]
    return spec


def summary_supplier() -> dict:
    return {"customer_assigned_account_id": ISSUER["ruc"], "additional_account_id": ["6"],
            "party": {"party_legal_entity": [{"registration_name": ISSUER["name"]}]}}


def summary_spec(case: dict) -> dict:
    ext, sig = signature_parts("any_element")
    lines = []
    for i, src in enumerate(case["documents"], start=1):
        req = json.loads((EX / src["request"]).read_text(encoding="utf-8"))
        t = calculate(req["items"])
        cur = req["currency"]
        pays = []
        for value, instr in ((t.taxed, "01"), (t.exempt, "02"), (t.non_taxable, "03"), (t.free_value, "05")):
            if value:
                pays.append({"paid_amount": amt(value, cur), "instruction_id": instr})
        taxes = [{"tax_amount": amt(t.igv, cur), "tax_subtotal": [{"tax_amount": amt(t.igv, cur),
                  "tax_category": {"tax_scheme": {"id": "1000", "name": "IGV", "tax_type_code": "VAT"}}}]}]
        d = req["buyer"]["document"]
        lines.append({"line_id": str(i), "document_type_code": req["type"], "id": f"{req['series']}-{src['number']}",
                      "accounting_customer_party": {"customer_assigned_account_id": d["number"], "additional_account_id": [d["type"]]},
                      "status": {"condition_code": {"value": src["conditionCode"], **{a: None for a in ("list_id", "list_agency_id", "list_agency_name", "list_name", "list_version_id", "language_id", "list_uri", "list_scheme_uri")}}},
                      "total_amount": amt(t.payable, cur), "billing_payment": pays, "tax_total": taxes})
    return {"ublextensions": ext, "ublversion_id": "2.0", "customization_id": "1.1", "id": case["id"],
            "reference_date": case["referenceDate"], "issue_date": case["issueDate"], "signature": [sig],
            "accounting_supplier_party": summary_supplier(), "summary_documents_line": lines}


def voided_spec(case: dict) -> dict:
    ext, sig = signature_parts("any_element")
    req = json.loads((EX / case["request"]).read_text(encoding="utf-8"))
    lines = []
    for i, d in enumerate(req["documents"], start=1):
        series, number = d["number"].split("-")
        lines.append({"line_id": str(i), "document_type_code": d["type"], "document_serial_id": series,
                      "document_number_id": number, "void_reason_description": d["reason"]})
    return {"ublextensions": ext, "ublversion_id": "2.0", "customization_id": "1.0", "id": case["id"],
            "reference_date": case["referenceDate"], "issue_date": case["issueDate"], "signature": [sig],
            "accounting_supplier_party": summary_supplier(), "voided_documents_line": lines}


# --- 4. Serialize → validate XSD → sign → validate again ---------------------------------------------
def serialize(root_name: str, spec: dict) -> bytes:
    from xsdata.formats.dataclass.serializers import XmlSerializer
    from xsdata.formats.dataclass.serializers.config import SerializerConfig
    obj = make(root_class(root_name), spec)
    ns_map = {None: ROOT_NS[root_name], **{k: v for k, v in NS.items() if k != "sac" or root_name in ("SummaryDocuments", "VoidedDocuments")}}
    xml = XmlSerializer(config=SerializerConfig(indent=None, xml_declaration=True, encoding="UTF-8")).render(obj, ns_map=ns_map)
    return xml.encode("utf-8")


_SCHEMAS: dict[str, etree.XMLSchema] = {}


def xsd_errors(root_name: str, xml: bytes) -> list[str]:
    if root_name not in _SCHEMAS:
        _SCHEMAS[root_name] = etree.XMLSchema(etree.parse(str(XSD_FOR[root_name])))
    schema = _SCHEMAS[root_name]
    ok = schema.validate(etree.fromstring(xml))
    return [] if ok else [f"line {e.line}: {e.message}" for e in schema.error_log]


def test_certificate():
    from cryptography import x509
    from cryptography.hazmat.primitives import hashes
    from cryptography.hazmat.primitives.asymmetric import rsa
    from cryptography.x509.oid import NameOID
    key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
    name = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, f"ABC TEST CERTIFICATE - NOT FOR PRODUCTION - RUC {ISSUER['ruc']}"),
                      x509.NameAttribute(NameOID.ORGANIZATION_NAME, ISSUER["name"])])
    now = dt.datetime(2026, 10, 1, tzinfo=dt.timezone.utc)
    cert = (x509.CertificateBuilder().subject_name(name).issuer_name(name).public_key(key.public_key())
            .serial_number(x509.random_serial_number()).not_valid_before(now)
            .not_valid_after(now + dt.timedelta(days=3650)).sign(key, hashes.SHA256()))
    return key, cert


def sign(xml: bytes, key, cert) -> bytes:
    from cryptography.hazmat.primitives.serialization import Encoding
    from signxml import XMLSigner, methods
    root = etree.fromstring(xml)
    ph = root.find(f".//{PLACEHOLDER}")
    ds_ph = etree.Element(f"{{{NS['ds']}}}Signature", Id="placeholder", nsmap={"ds": NS["ds"]})
    ph.getparent().replace(ph, ds_ph)
    signer = XMLSigner(method=methods.enveloped, signature_algorithm="rsa-sha256", digest_algorithm="sha256",
                       c14n_algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315")
    signed = signer.sign(root, key=key, cert=[cert.public_bytes(Encoding.PEM).decode()])
    sig = signed.find(f".//{{{NS['ds']}}}Signature")
    sig.set("Id", "SignABC")
    return etree.tostring(signed, xml_declaration=True, encoding="UTF-8")


def build_all() -> int:
    cases = json.loads((EX / "xml" / "cases.json").read_text(encoding="utf-8"))
    products = load_products()
    key, cert = test_certificate()
    failures = 0
    for out, case in cases.items():
        kind = case["kind"]
        if kind in ("invoice", "note"):
            req = json.loads((EX / case["request"]).read_text(encoding="utf-8"))
            t = calculate(req["items"], products)
            root_name = "Invoice" if kind == "invoice" else ("CreditNote" if req["type"] == "07" else "DebitNote")
            spec = invoice_spec(req, case, t) if kind == "invoice" else note_spec(req, case, t)
        elif kind == "summary":
            root_name, spec = "SummaryDocuments", summary_spec(case)
        else:
            root_name, spec = "VoidedDocuments", voided_spec(case)
        xml = serialize(root_name, spec)
        errs = xsd_errors(root_name, xml)            # ADR 0003: validate BEFORE signing
        if errs:
            failures += 1
            print(f"✗ {out}: XSD errors before signing:\n   " + "\n   ".join(errs[:10]))
            continue
        signed = sign(xml, key, cert)
        errs = xsd_errors(root_name, signed)
        if errs:
            failures += 1
            print(f"✗ {out}: XSD errors after signing: {errs[:3]}")
            continue
        (EX / "xml" / out).write_bytes(signed)
        print(f"✓ {out} ({root_name}) built, valid against {XSD_FOR[root_name].name}, signed")
    return failures


if __name__ == "__main__":
    sys.exit(1 if build_all() else 0)
