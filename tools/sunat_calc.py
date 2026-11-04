"""Reference implementation of the tax and business rules of the specs (FR-02 to FR-06, FR-12, FR-46 to FR-48).

It is *not* the product (the product is .NET, ADR 0002): it is an executable version of the rules so the
examples, the reference XML and the acceptance criteria can be checked. Exact decimals only (constitution 7).
"""
from __future__ import annotations

import json
from dataclasses import dataclass, field
from decimal import ROUND_HALF_UP, Decimal
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
D = Decimal
IGV_RATE = D("0.18")          # 15.5 % IGV + 2.5 % IPM since 2026, shown as 18 (research 001)
ICBPER_RATE = D("0.50")       # per bag since 2023 (research 001)
ISC_RETAIL_FACTOR = D("0.847")  # 1 / 1.18, 3 decimals (research 001, ISC system 03)
RECEIPT_ID_THRESHOLD = D("700.00")
FREE_TREATMENTS = {"11", "12", "13", "14", "15", "16", "21", "31", "32", "33", "34", "35", "36", "37"}
FREE_TAXED = {"11", "12", "13", "14", "15", "16"}


def r2(x: Decimal) -> Decimal:
    return x.quantize(D("0.01"), rounding=ROUND_HALF_UP)


def fmt(x: Decimal, min_dec: int = 2, max_dec: int = 10) -> str:
    """Decimal as text: at least 2 decimals, at most 10, no exponent."""
    q = x.quantize(D(1).scaleb(-max_dec), rounding=ROUND_HALF_UP).normalize()
    s = format(q, "f")
    if "." not in s:
        s += "."
    ent, dec = s.split(".")
    return f"{ent}.{dec.ljust(min_dec, '0')}"


# --- FR-02 -------------------------------------------------------------------------------------------
def ruc_is_valid(ruc: str) -> bool:
    if len(ruc) != 11 or not ruc.isdigit() or ruc[:2] not in {"10", "15", "16", "17", "20"}:
        return False
    weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2]
    total = sum(int(d) * w for d, w in zip(ruc[:10], weights))
    dv = 11 - total % 11
    dv = {10: 0, 11: 1}.get(dv, dv)
    return dv == int(ruc[10])


# --- Legend 1000 (FR-15) -----------------------------------------------------------------------------
_U = ["", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ", "ONCE", "DOCE",
      "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE", "VEINTE",
      "VEINTIUNO", "VEINTIDOS", "VEINTITRES", "VEINTICUATRO", "VEINTICINCO", "VEINTISEIS", "VEINTISIETE",
      "VEINTIOCHO", "VEINTINUEVE"]
_T = ["", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"]
_C = ["", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS",
      "OCHOCIENTOS", "NOVECIENTOS"]


def _hundreds(n: int) -> str:
    if n == 0:
        return ""
    if n == 100:
        return "CIEN"
    c, rest = divmod(n, 100)
    if rest < 30:
        tail = _U[rest]
    else:
        t, u = divmod(rest, 10)
        tail = _T[t] + (f" Y {_U[u]}" if u else "")
    return " ".join(p for p in (_C[c], tail) if p)


def number_in_words(n: int) -> str:
    if n == 0:
        return "CERO"
    millions, rest = divmod(n, 1_000_000)
    thousands, units = divmod(rest, 1000)
    parts = []
    if millions:
        parts.append("UN MILLON" if millions == 1 else f"{_hundreds(millions)} MILLONES")
    if thousands:
        parts.append("MIL" if thousands == 1 else f"{_hundreds(thousands)} MIL")
    if units:
        parts.append(_hundreds(units))
    return " ".join(parts)


def amount_in_words(amount: Decimal, currency: str) -> str:
    amount = r2(amount)
    whole = int(amount)
    cents = int((amount - whole) * 100)
    name = {"PEN": "SOLES", "USD": "DOLARES AMERICANOS"}[currency]
    return f"{number_in_words(whole)} CON {cents:02d}/100 {name}"


# --- FR-05, FR-06, FR-47, FR-48 ----------------------------------------------------------------------
@dataclass
class LineResult:
    order: int
    product: dict
    treatment: str
    tax_code: str                 # catalog 05 of the IGV-level subtotal: 1000, 9997, 9998, 9996
    quantity: Decimal
    unit: str
    unit_value: Decimal
    reference_unit_value: Decimal | None
    sale_value: Decimal           # cbc:LineExtensionAmount
    isc_system: str | None = None
    isc_base: Decimal = D(0)
    isc: Decimal = D(0)
    isc_rate: Decimal | None = None
    igv_base: Decimal = D(0)
    igv: Decimal = D(0)
    icbper_bags: int = 0
    icbper: Decimal = D(0)
    unit_price: Decimal = D(0)    # PricingReference code 01 (tax included, without ICBPER)

    @property
    def free(self) -> bool:
        return self.treatment in FREE_TREATMENTS

    @property
    def line_taxes(self) -> Decimal:
        return self.isc + (D(0) if self.free else self.igv) + self.icbper


@dataclass
class Totals:
    lines: list[LineResult] = field(default_factory=list)
    taxed: Decimal = D(0)          # Σ sale value of taxed lines (header TaxSubtotal 1000 TaxableAmount, PLE 14.1 field 14)
    exempt: Decimal = D(0)
    non_taxable: Decimal = D(0)
    free_value: Decimal = D(0)
    free_igv: Decimal = D(0)
    igv: Decimal = D(0)
    isc: Decimal = D(0)
    icbper: Decimal = D(0)
    line_extension: Decimal = D(0)
    tax_inclusive: Decimal = D(0)
    payable: Decimal = D(0)

    @property
    def tax_total(self) -> Decimal:
        return self.igv + self.isc + self.icbper


def load_products() -> dict[str, dict]:
    data = json.loads((ROOT / "specs/examples/valid/products.json").read_text(encoding="utf-8"))
    return {p["code"]: p for p in data}


def calculate(items: list[dict], products: dict[str, dict] | None = None) -> Totals:
    products = products or load_products()
    t = Totals()
    for order, it in enumerate(items, start=1):
        p = products[it["productCode"]]
        treatment = it.get("igvTreatment", p["igvTreatment"])
        qty = D(it["quantity"])
        uv = D(it["unitValue"])
        ref = D(it["referenceUnitValue"]) if "referenceUnitValue" in it else None
        free = treatment in FREE_TREATMENTS
        if free:
            sale_value = r2(qty * ref)
        else:
            sale_value = r2(qty * uv - D(it.get("discount", "0")))
        if treatment.startswith("1"):
            tax_code = "9996" if free else "1000"
        elif treatment.startswith("2"):
            tax_code = "9996" if free else "9997"
        elif treatment.startswith("3"):
            tax_code = "9996" if free else "9998"
        else:
            tax_code = "9995"
        line = LineResult(order, p, treatment, tax_code, qty, it["unit"], uv, ref, sale_value)
        isc_cfg = p.get("isc")
        if isc_cfg and not free:
            line.isc_system = isc_cfg["system"]
            if isc_cfg["system"] == "01":
                line.isc_rate = D(isc_cfg["rate"])
                line.isc_base = sale_value
                line.isc = r2(sale_value * line.isc_rate / 100)
            elif isc_cfg["system"] == "02":
                units = D(p.get("iscUnitsPerItem", "1"))
                line.isc_base = sale_value
                line.isc = r2(qty * units * D(isc_cfg["fixedAmount"]))
            else:  # 03
                line.isc_rate = D(isc_cfg["rate"])
                line.isc_base = r2(qty * D(isc_cfg["suggestedRetailPrice"]) * ISC_RETAIL_FACTOR)
                line.isc = r2(line.isc_base * line.isc_rate / 100)
        if tax_code == "1000" or treatment in FREE_TAXED:
            line.igv_base = sale_value + line.isc
            line.igv = r2(line.igv_base * IGV_RATE)
        else:
            line.igv_base = sale_value
        if p.get("isPlasticBag") and not free:
            line.icbper_bags = int(qty)
            line.icbper = r2(qty * ICBPER_RATE)
        if not free:
            line.unit_price = (sale_value + line.isc + line.igv) / qty
        t.lines.append(line)
        if free:
            t.free_value += sale_value
            t.free_igv += line.igv
            continue
        t.line_extension += sale_value
        if tax_code == "1000":
            t.taxed += sale_value
            t.igv += line.igv
        elif tax_code == "9997":
            t.exempt += sale_value
        elif tax_code == "9998":
            t.non_taxable += sale_value
        t.isc += line.isc
        t.icbper += line.icbper
    t.tax_inclusive = t.line_extension + t.tax_total
    t.payable = t.tax_inclusive
    return t


# --- Business rules the JSON Schema cannot express (422 layer) ---------------------------------------
def business_errors(kind: str, doc: dict, products: dict[str, dict] | None = None) -> list[str]:
    """Returns the internal error codes (regulatory-matrix §G) a request would get."""
    errors: list[str] = []
    if kind != "issue":
        return errors
    products = products or load_products()
    buyer_doc = doc["buyer"]["document"]
    if buyer_doc["type"] == "6" and not ruc_is_valid(buyer_doc["number"]):
        errors.append("INVALID_RUC")
    for it in doc["items"]:
        if it["productCode"] not in products:
            errors.append("UNCLASSIFIED_PRODUCT")
    if errors:
        return errors
    totals = calculate(doc["items"], products)
    if doc["type"] == "03" and totals.payable > RECEIPT_ID_THRESHOLD and buyer_doc["type"] == "0":
        errors.append("RECEIPT_REQUIRES_DOCUMENT")
    pt = doc["paymentTerms"]
    if pt["type"] == "CREDIT":
        out = D(pt["outstandingAmount"])
        if sum(D(i["amount"]) for i in pt["installments"]) != out or out > totals.payable:
            errors.append("INSTALLMENTS_MISMATCH")
    return errors
