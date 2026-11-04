"""Executable check of the Spec-First kit.  Run:  python tools/validate_specs.py   (exit code 0 = all green)

Groups (each prints ✓/✗ per check):
  A. Official XSDs untouched (hash manifest, line endings normalized) and compilable
  B. JSON Schemas valid (2020-12) and their enums equal to the SUNAT catalogs / PLE tables
  C. Valid examples pass; invalid examples fail at the expected layer (schema or business rule)
  D. Reference XML: valid against the official XSD, signature verifies, file name and amounts match their JSON
  E. Acceptance criteria with numbers, recalculated with the reference rules (tools/sunat_calc.py)
  F. OpenAPI and AsyncAPI contracts (refs, summaries, 401/403, binary downloads, FR/AC exist)
  G. Traceability: FR/AC numbering, AC ↔ traceability.md, matrix ↔ SUNAT return codes, links, no secrets
Options:  --write-manifest  recompute xsd/MANIFEST.sha256 (only after replacing the XSDs on purpose)
          --lint            also run Redocly and Spectral through npx (needs Node.js)
"""
from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
from decimal import Decimal as D
from pathlib import Path

import yaml
from jsonschema import Draft202012Validator
from lxml import etree
from referencing import Registry, Resource

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sunat_calc as calc  # noqa: E402

ROOT = calc.ROOT
SPECS = ROOT / "specs"
SCHEMAS = SPECS / "contracts" / "schemas"
EX = SPECS / "examples"
CAT = SPECS / "catalogs"
XSD = ROOT / "xsd"
NS = {"cbc": "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2",
      "cac": "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2",
      "ds": "http://www.w3.org/2000/09/xmldsig#",
      "sac": "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1"}

results: list[tuple[str, str, bool, str]] = []


def check(group: str, name: str, ok: bool, detail: str = "") -> bool:
    results.append((group, name, bool(ok), detail))
    print(f"  {'✓' if ok else '✗'} {name}" + (f" — {detail}" if detail and not ok else ""))
    return bool(ok)


def text(p: Path) -> str:
    return p.read_text(encoding="utf-8").replace("\r\n", "\n")


def lf_hash(p: Path) -> str:
    return hashlib.sha256(p.read_bytes().replace(b"\r\n", b"\n")).hexdigest()


def codes(name: str) -> set[str]:
    return {c["code"] for c in json.loads(text(CAT / f"{name}.json"))["codes"]}


# ---------------------------------------------------------------------------------------------------- A
def group_a(write_manifest: bool) -> None:
    print("\nA. Official XSDs")
    files = sorted(XSD.rglob("*.xsd"))
    manifest = XSD / "MANIFEST.sha256"
    if write_manifest:
        manifest.write_text("".join(f"{lf_hash(f)}  {f.relative_to(XSD).as_posix()}\n" for f in files), encoding="utf-8")
        print("  manifest rewritten")
    expected = dict(line.split("  ", 1)[::-1] for line in text(manifest).splitlines() if line.strip())
    changed = [f.relative_to(XSD).as_posix() for f in files if expected.get(f.relative_to(XSD).as_posix()) != lf_hash(f)]
    missing = [k for k in expected if not (XSD / k).exists()]
    check("A", f"{len(files)} XSD files identical to MANIFEST.sha256 (CRLF/LF neutral)", not changed and not missing,
          f"changed {changed} missing {missing}")
    for main in ["ubl-2.1/maindoc/UBL-Invoice-2.1.xsd", "ubl-2.1/maindoc/UBL-CreditNote-2.1.xsd",
                 "ubl-2.1/maindoc/UBL-DebitNote-2.1.xsd", "ubl-2.1/maindoc/UBL-ApplicationResponse-2.1.xsd",
                 "sunat/maindoc/UBL-SummaryDocuments-2.0.xsd", "sunat/maindoc/UBL-VoidedDocuments-2.0.xsd"]:
        try:
            etree.XMLSchema(etree.parse(str(XSD / main)))
            check("A", f"{Path(main).name} compiles with all its imports", True)
        except Exception as e:  # noqa: BLE001
            check("A", f"{Path(main).name} compiles with all its imports", False, str(e)[:200])


# ---------------------------------------------------------------------------------------------------- B
def registry() -> Registry:
    reg = Registry()
    for f in SCHEMAS.glob("*.json"):
        s = json.loads(text(f))
        res = Resource.from_contents(s)
        reg = reg.with_resource(s["$id"], res).with_resource(f.name, res)
    return reg


REG: Registry | None = None


def validator(schema_name: str) -> Draft202012Validator:
    s = json.loads(text(SCHEMAS / f"{schema_name}.schema.json"))
    return Draft202012Validator(s, registry=REG)


def group_b() -> None:
    global REG
    print("\nB. JSON Schemas and catalogs")
    bad = []
    for f in sorted(SCHEMAS.glob("*.json")):
        try:
            Draft202012Validator.check_schema(json.loads(text(f)))
        except Exception as e:  # noqa: BLE001
            bad.append(f"{f.name}: {e}")
    check("B", f"{len(list(SCHEMAS.glob('*.json')))} JSON Schemas are valid 2020-12", not bad, "; ".join(bad)[:300])
    REG = registry()
    defs = json.loads(text(SCHEMAS / "common.schema.json"))["$defs"]
    pairs = [("DocumentType", "catalog-01"), ("Currency", "catalog-02"), ("UnitOfMeasure", "catalog-03"),
             ("IdentityDocumentType", "catalog-06"), ("IgvTreatment", "catalog-07"), ("IscSystem", "catalog-08"),
             ("OperationType", "catalog-51"), ("Table2IdentityType", "ple-table-02"), ("Table5ExistenceType", "ple-table-05"),
             ("Table10DocumentType", "ple-table-10"), ("Table12OperationType", "ple-table-12"), ("Table13Catalog", "ple-table-13")]
    for d, cat in pairs:
        extra = set(defs[d]["enum"]) - codes(cat)
        check("B", f"enum {d} ⊆ {cat}.json", not extra, f"not in catalog: {sorted(extra)}")
    check("B", "enum Table12OperationType = every code of PLE Table 12", set(defs["Table12OperationType"]["enum"]) == codes("ple-table-12"))
    unit = set(defs["UnitOfMeasure"]["enum"])
    check("B", "every UnitOfMeasure code exists in PLE Table 6 with the same code (matrix §K)", unit <= codes("ple-table-06"),
          str(sorted(unit - codes("ple-table-06"))))
    note = json.loads(text(SCHEMAS / "note-request.schema.json"))
    r07 = set(note["allOf"][0]["then"]["properties"]["reason"]["properties"]["code"]["enum"])
    r08 = set(note["allOf"][1]["then"]["properties"]["reason"]["properties"]["code"]["enum"])
    check("B", "credit note reasons ⊆ catalog 09 and debit note reasons ⊆ catalog 10",
          r07 <= codes("catalog-09") and r08 <= codes("catalog-10"))
    matrix_k = text(SPECS / "traceability/regulatory-matrix.md").split("## K.")[1].split("## Gaps")[0]
    k_codes = set(re.findall(r"^\| `([A-Z0-9]+)` \| `([A-Z0-9]+)` \|", matrix_k, re.M))
    check("B", f"matrix §K: {len(k_codes)} Catalog 03 ↔ Table 6 rows, codes identical and present in both catalogs",
          k_codes and all(a == b and a in codes("catalog-03") and b in codes("ple-table-06") for a, b in k_codes)
          and {a for a, _ in k_codes} == unit)


# ---------------------------------------------------------------------------------------------------- C
SCHEMA_OF = {"issue": "issue-document-request", "note": "note-request", "void": "void-request", "series": "series",
             "contingency": "contingency-request", "legacy": "legacy-record", "product": "product"}


def group_c() -> None:
    print("\nC. Examples")
    products = json.loads(text(EX / "valid/products.json"))
    errs = [e.message for p in products for e in validator("product").iter_errors(p)]
    check("C", f"{len(products)} example products valid against product.schema.json", not errs, "; ".join(errs)[:300])
    for f in sorted((EX / "valid").glob("*.json")):
        if f.name == "products.json":
            continue
        kind = f.name.split("-")[0]
        doc = json.loads(text(f))
        errs = [f"{'/'.join(map(str, e.absolute_path))}: {e.message}" for e in validator(SCHEMA_OF[kind]).iter_errors(doc)]
        biz = calc.business_errors(kind, doc)
        check("C", f"valid/{f.name} passes {SCHEMA_OF[kind]} and the business rules", not errs and not biz,
              "; ".join(errs + biz)[:300])
    expected = json.loads(text(EX / "invalid/expected.json"))
    files = {f.name for f in (EX / "invalid").glob("*.json")} - {"expected.json"}
    check("C", "every invalid example has an expected outcome", files == set(expected), str(files ^ set(expected)))
    for name, exp in sorted(expected.items()):
        doc = json.loads(text(EX / "invalid" / name))
        errs = list(validator(exp["schema"]).iter_errors(doc))
        if exp["layer"] == "schema":
            keywords = {e.validator for err in errs for e in [err, *err.context]} | {
                e.validator for err in errs for e in _flatten(err)}
            ok = bool(errs) and exp["expect"] in keywords
            check("C", f"invalid/{name} rejected by the schema ({exp['expect']}) — {exp['ac']}", ok,
                  f"got {sorted(keywords) or 'no error'}")
        else:
            biz = calc.business_errors(exp["schema"].split("-")[0], doc)
            ok = not errs and exp["expect"] in biz
            check("C", f"invalid/{name} passes the schema but gets {exp['expect']} (422) — {exp['ac']}", ok,
                  f"schema errors {[e.message for e in errs][:2]}, business {biz}")


def _flatten(err):
    yield err
    for c in err.context:
        yield from _flatten(c)


# ---------------------------------------------------------------------------------------------------- D
XSD_FOR = {"Invoice": "ubl-2.1/maindoc/UBL-Invoice-2.1.xsd", "CreditNote": "ubl-2.1/maindoc/UBL-CreditNote-2.1.xsd",
           "DebitNote": "ubl-2.1/maindoc/UBL-DebitNote-2.1.xsd", "SummaryDocuments": "sunat/maindoc/UBL-SummaryDocuments-2.0.xsd",
           "VoidedDocuments": "sunat/maindoc/UBL-VoidedDocuments-2.0.xsd"}


def group_d() -> None:
    from signxml import XMLVerifier
    print("\nD. Reference XML")
    cases = json.loads(text(EX / "xml/cases.json"))
    issuer = json.loads(text(EX / "xml/issuer.json"))
    schemas: dict[str, etree.XMLSchema] = {}
    on_disk = {f.name for f in (EX / "xml").glob("*.xml")}
    check("D", f"{len(cases)} reference XML listed in cases.json, all present", set(cases) == on_disk, str(set(cases) ^ on_disk))
    for name, case in cases.items():
        path = EX / "xml" / name
        if not path.exists():
            continue
        root = etree.parse(str(path)).getroot()
        rn = etree.QName(root).localname
        schemas.setdefault(rn, etree.XMLSchema(etree.parse(str(XSD / XSD_FOR[rn]))))
        sch = schemas[rn]
        ok = sch.validate(root)
        check("D", f"{name} valid against official {Path(XSD_FOR[rn]).name}", ok, str(sch.error_log.last_error))
        try:
            cert = root.find(".//ds:X509Certificate", NS).text
            XMLVerifier().verify(root, x509_cert=cert, ignore_ambiguous_key_info=True)
            in_ext = root.find(".//{*}ExtensionContent/ds:Signature", NS) is not None
            check("D", f"{name} XMLDSig enveloped signature verifies, inside ext:ExtensionContent", in_ext)
        except Exception as e:  # noqa: BLE001
            check("D", f"{name} XMLDSig enveloped signature verifies", False, str(e)[:150])
        doc_id = root.findtext("cbc:ID", namespaces=NS)
        if rn in ("Invoice", "CreditNote", "DebitNote"):
            tcode = {"Invoice": root.findtext("cbc:InvoiceTypeCode", namespaces=NS), "CreditNote": "07", "DebitNote": "08"}[rn]
            check("D", f"{name} file name = RUC-type-ID (Annex A RS 113-2018)", name == f"{issuer['ruc']}-{tcode}-{doc_id}.xml")
            _check_amounts(name, case, root, rn)
        else:
            check("D", f"{name} file name = RUC-ID", name == f"{issuer['ruc']}-{doc_id}.xml")
        missing = [a for a in case.get("acs", []) if a not in ALL_AC]
        check("D", f"{name} ACs {', '.join(case.get('acs', []))} exist in the specs", not missing, str(missing))


def _num(root, path) -> D:
    v = root.findtext(path, namespaces=NS)
    return D(v) if v is not None else D(0)


def _check_amounts(name, case, root, rn):
    req = json.loads(text(EX / case["request"]))
    t = calc.calculate(req["items"])
    cur = req.get("currency", "PEN")
    total_tag = "cac:RequestedMonetaryTotal" if rn == "DebitNote" else "cac:LegalMonetaryTotal"
    got = {
        "payable": _num(root, f"{total_tag}/cbc:PayableAmount"),
        "lineExtension": _num(root, f"{total_tag}/cbc:LineExtensionAmount"),
        "taxTotal": _num(root, "cac:TaxTotal/cbc:TaxAmount"),
        "lines": len(root.findall("cac:InvoiceLine", NS) + root.findall("cac:CreditNoteLine", NS) + root.findall("cac:DebitNoteLine", NS)),
    }
    want = {"payable": t.payable, "lineExtension": t.line_extension, "taxTotal": t.tax_total, "lines": len(t.lines)}
    check("D", f"{name} totals equal the recalculation of {case['request']} (payable {t.payable})", got == want, f"xml {got} vs {want}")
    legend = root.findtext("cbc:Note[@languageLocaleID='1000']", namespaces=NS)
    check("D", f"{name} legend 1000 = amount in words", legend == calc.amount_in_words(t.payable, cur), legend or "missing")
    if t.free_value:
        check("D", f"{name} free-of-charge: legend 1002 and TaxSubtotal 9996 present",
              root.find("cbc:Note[@languageLocaleID='1002']", NS) is not None
              and root.find("cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme[cbc:ID='9996']", NS) is not None)
    if cur == "USD":
        check("D", f"{name} USD: cac:PaymentExchangeRate = {req['exchangeRate']}",
              root.findtext("cac:PaymentExchangeRate/cbc:CalculationRate", namespaces=NS) == req["exchangeRate"])
    systems = {l.isc_system for l in t.lines if l.isc_system}
    if systems:
        tiers = {e.text for e in root.iterfind(".//cac:TaxCategory/cbc:TierRange", NS)}
        check("D", f"{name} catalog 08 systems {sorted(systems)} in cac:TaxCategory/cbc:TierRange", tiers == systems, str(tiers))


# ---------------------------------------------------------------------------------------------------- E
def group_e() -> None:
    print("\nE. Acceptance criteria recalculated")
    c = calc.calculate
    def item(code, q, v, **kw):
        unit = calc.load_products()[code]["unit"]
        return {"productCode": code, "quantity": q, "unit": unit, "unitValue": v, **kw}
    l = c([item("P-001", "10", "5.00")]).lines[0]
    check("E", "AC-08 taxed item: sale value 50.00, IGV 9.00, amount 59.00", (l.sale_value, l.igv, l.sale_value + l.igv) == (D("50.00"), D("9.00"), D("59.00")))
    l = c([item("P-021", "4", "25.00")]).lines[0]
    check("E", "AC-09 ISC 01 25 %: ISC 25.00, IGV base 125.00, IGV 22.50, amount 147.50",
          (l.isc, l.igv_base, l.igv, l.sale_value + l.isc + l.igv) == (D("25.00"), D("125.00"), D("22.50"), D("147.50")))
    p = dict(calc.load_products()); p["X"] = {"code": "X", "unit": "NIU", "igvTreatment": "10", "isc": {"system": "02", "fixedAmount": "1.25"}}
    l = c([{"productCode": "X", "quantity": "12", "unit": "NIU", "unitValue": "1.00"}], p).lines[0]
    check("E", "AC-10 ISC 02: 12 × 1.25 = 15.00", l.isc == D("15.00"))
    l = c([item("P-030", "3", "0.10")]).lines[0]
    check("E", "AC-11 ICBPER 1.50 outside the IGV base, IGV 0.05, amount 1.85",
          (l.icbper, l.igv, l.sale_value + l.igv + l.icbper) == (D("1.50"), D("0.05"), D("1.85")))
    l = c([item("P-010", "4", "5.00")]).lines[0]
    check("E", "AC-12 exempt item: IGV 0.00, tax 9997", l.igv == 0 and l.tax_code == "9997")
    t = c([item("P-001", "10", "5.00"), item("P-010", "4", "5.00")])
    check("E", "AC-13 totals: taxed 50.00, exempt 20.00, IGV 9.00, total 79.00",
          (t.taxed, t.exempt, t.igv, t.payable) == (D("50.00"), D("20.00"), D("9.00"), D("79.00")))
    check("E", "AC-25 legend: SETENTA Y NUEVE CON 00/100 SOLES", calc.amount_in_words(D("79.00"), "PEN") == "SETENTA Y NUEVE CON 00/100 SOLES")
    check("E", "AC-03 / AC-04 RUC 20131312955 valid, 20131312954 invalid", calc.ruc_is_valid("20131312955") and not calc.ruc_is_valid("20131312954"))
    t = c([item("P-002", "2", "0.00", igvTreatment="15", referenceUnitValue="3.00")])
    l = t.lines[0]
    check("E", "AC-67 free line: 9996 taxable 6.00, IGV 1.08, total amount unchanged (0.00)",
          (l.tax_code, l.sale_value, l.igv, t.payable) == ("9996", D("6.00"), D("1.08"), D("0.00")))
    p["Y"] = {"code": "Y", "unit": "NIU", "igvTreatment": "10", "isc": {"system": "02", "fixedAmount": "1.50"}, "iscUnitsPerItem": "0.62"}
    l = c([{"productCode": "Y", "quantity": "12", "unit": "NIU", "unitValue": "4.00"}], p).lines[0]
    check("E", "AC-68 ISC per litre: 12 × 0.62 × 1.50 = 11.16", l.isc == D("11.16"))
    rc = json.loads(text(CAT / "sunat-return-codes.json"))
    cls = {x["code"]: x["class"] for x in rc["codes"]}
    check("E", "AC-61/62/63 return-code classes: 0109 EXCEPTION, 2800 REJECTION, 4287 OBSERVATION",
          (cls.get("0109"), cls.get("2800"), cls.get("4287")) == ("EXCEPTION", "REJECTION", "OBSERVATION"))


# ---------------------------------------------------------------------------------------------------- F
def group_f(lint: bool) -> None:
    print("\nF. API contracts")
    api_path = SPECS / "contracts/invoicing-api.yaml"
    api = yaml.safe_load(text(api_path))
    ops = [(p, m, o) for p, item in api["paths"].items() for m, o in item.items() if m in ("get", "post", "put", "patch", "delete")]
    check("F", f"OpenAPI {api['openapi']}: {len(ops)} operations, each with operationId and summary",
          all("operationId" in o and o.get("summary") for _, _, o in ops))
    ids = [o["operationId"] for _, _, o in ops]
    check("F", "operationIds are unique", len(ids) == len(set(ids)))
    no_auth = [o["operationId"] for _, _, o in ops if not {"401", "403"} <= set(o["responses"])]
    check("F", "every operation declares 401 and 403 (bearerAuth is global)", not no_auth, str(no_auth))
    raw = text(api_path)
    check("F", "binary downloads use format: binary (no contentEncoding)", "contentEncoding" not in raw)
    bad_refs = []
    for ref in set(re.findall(r"\$ref:\s*'([^']+)'", raw)):
        file, _, pointer = ref.partition("#")
        doc = api if not file else json.loads(text((api_path.parent / file).resolve()))
        node = doc
        for part in [p for p in pointer.split("/") if p]:
            node = node.get(part) if isinstance(node, dict) else None
        if node is None:
            bad_refs.append(ref)
    check("F", "every $ref of the OpenAPI resolves (files and JSON pointers)", not bad_refs, str(bad_refs))
    unknown = sorted({x for x in re.findall(r"\b(FR-\d+|AC-\d+)\b", raw) if x not in ALL_FR | ALL_AC})
    check("F", "every FR/AC named in the OpenAPI exists in the specs", not unknown, str(unknown))
    issue = api["paths"]["/documents"]["post"]["responses"]
    check("F", "issueDocument answers 201, 200 (idempotent replay), 202, 422, 502 and 503",
          {"200", "201", "202", "422", "502", "503"} <= set(issue))
    asy = yaml.safe_load(text(SPECS / "contracts/events.asyncapi.yaml"))
    dangling = [c for c in asy["operations"].values() if c["channel"]["$ref"].split("/")[-1] not in asy["channels"]]
    check("F", f"AsyncAPI {asy['asyncapi']}: {len(asy['channels'])} channels, every operation points to an existing one", not dangling)
    if lint:
        for cmd, label in ((["npx", "-y", "@redocly/cli@latest", "lint", str(api_path), "--format=summary"], "Redocly lint: 0 errors, 0 warnings"),
                           (["npx", "-y", "@stoplight/spectral-cli", "lint", str(api_path), "--ruleset", str(ROOT / ".spectral.yaml"), "--fail-severity", "warn"], "Spectral lint: 0 warnings")):
            r = subprocess.run(cmd, capture_output=True, text=True)
            out = r.stdout + r.stderr
            ok = r.returncode == 0 and "warning" not in out.lower().replace("no results with a severity of 'warn'", "")
            check("F", label, ok, out[-300:])


# ---------------------------------------------------------------------------------------------------- G
ALL_FR: set[str] = set()
ALL_AC: set[str] = set()


def collect_ids() -> None:
    for f in (SPECS / "features").glob("*/spec.md"):
        t = text(f)
        ALL_FR.update(re.findall(r"^- \*\*(FR-\d+)\*\*", t, re.M))
        ALL_AC.update(re.findall(r"^(AC-\d+) \(", t, re.M))


def group_g() -> None:
    print("\nG. Traceability and repository hygiene")
    fr = sorted(int(x[3:]) for x in ALL_FR)
    ac = sorted(int(x[3:]) for x in ALL_AC)
    check("G", f"FR-01..FR-{fr[-1]:02d} numbered without gaps or repeats", fr == list(range(1, fr[-1] + 1)))
    check("G", f"AC-01..AC-{ac[-1]:02d} numbered without gaps or repeats", ac == list(range(1, ac[-1] + 1)))
    trace = text(SPECS / "traceability/traceability.md")
    rows = re.findall(r"^\| \d{3} \| US-\d+ \| (FR-\d+) \| (AC-\d+) \|", trace, re.M)
    t_ac = [a for _, a in rows]
    check("G", f"traceability.md has exactly one row per AC ({len(ALL_AC)})", sorted(t_ac) == sorted(ALL_AC) and len(t_ac) == len(set(t_ac)),
          str(sorted(set(ALL_AC) ^ set(t_ac))))
    spec_pairs = {}
    for f in (SPECS / "features").glob("*/spec.md"):
        spec_pairs.update(dict(re.findall(r"^(AC-\d+) \((FR-\d+)", text(f), re.M)))
    wrong = [a for f_, a in rows if spec_pairs.get(a) != f_]
    check("G", "each traceability row uses the FR its AC tests in the spec", not wrong, str(wrong))
    check("G", "every FR has at least one AC", ALL_FR <= {f for f, _ in rows}, str(sorted(ALL_FR - {f for f, _ in rows})))
    matrix = text(SPECS / "traceability/regulatory-matrix.md")
    rc = {x["code"] for x in json.loads(text(CAT / "sunat-return-codes.json"))["codes"]}
    used = set()
    for line in matrix.splitlines():
        if line.startswith("|"):
            for cell in line.split("|"):
                cell = cell.strip()
                if re.fullmatch(r"\d{4}(,\s*\d{4})*", cell):
                    used |= set(re.findall(r"\d{4}", cell))
    check("G", f"the {len(used)} SUNAT codes cited in the regulatory matrix exist in sunat-return-codes.json", used <= rc, str(sorted(used - rc)))
    m_ac = set(re.findall(r"\bAC-\d+\b", matrix))
    check("G", "every AC cited in the regulatory matrix exists", m_ac <= ALL_AC, str(sorted(m_ac - ALL_AC)))
    rows_num = [int(n) for n in re.findall(r"^\| (\d+) \| (?:\*\*)?[A-Z]", matrix.split("## G.")[0], re.M)]
    check("G", f"regulatory matrix SUNAT ↔ UBL rows 1..{max(rows_num)} each present once", sorted(rows_num) == list(range(1, max(rows_num) + 1)))
    broken = []
    for md in list(ROOT.glob("*.md")) + list(SPECS.rglob("*.md")) + list((ROOT / ".claude").rglob("*.md")) + list(XSD.glob("*.md")):
        for target in re.findall(r"\]\((?!https?://|mailto:|#)([^)#\s]+)", text(md)):
            if not (md.parent / target).exists():
                broken.append(f"{md.relative_to(ROOT)} → {target}")
    check("G", "every relative link in the Markdown files resolves", not broken, "; ".join(broken[:8]))
    secrets = [p.relative_to(ROOT).as_posix() for p in ROOT.rglob("*") if p.is_file() and ".git" not in p.parts
               and (p.suffix.lower() in {".pfx", ".p12", ".key", ".jks"} or (p.suffix in {".pem", ".txt", ".xml", ".json", ".md"} and "PRIVATE KEY-----" in p.read_text(errors="ignore")))]
    check("G", "no certificate, private key or keystore versioned (constitution 15)", not secrets, str(secrets))
    lf = [p.relative_to(ROOT).as_posix() for p in list(SPECS.rglob("*")) + list((ROOT / "tools").glob("*.py"))
          if p.is_file() and p.suffix in {".md", ".json", ".yaml", ".py"} and b"\r\n" in p.read_bytes()]
    # Informational only: every check above normalizes CRLF, so a Windows copy (CRLF) gives the same result.
    print(f"  · line endings: {'all LF' if not lf else f'{len(lf)} files with CRLF (normalized by every check; .gitattributes keeps LF in git)'}")


def main() -> int:
    args = set(sys.argv[1:])
    collect_ids()
    group_a("--write-manifest" in args)
    group_b()
    group_c()
    group_d()
    group_e()
    group_f("--lint" in args)
    group_g()
    failed = [r for r in results if not r[2]]
    print(f"\n{len(results)} checks, {len(results) - len(failed)} passed, {len(failed)} failed")
    for g, n, _, d in failed:
        print(f"  ✗ [{g}] {n} {d}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
