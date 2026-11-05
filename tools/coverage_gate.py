"""Fails (exit 1) when the line coverage of a Cobertura report is below the minimum.

Usage: python tools/coverage_gate.py <report.cobertura.xml> [minimum percent, default 80]
The report comes from `dotnet test --collect "Code Coverage" --settings coverage.runsettings`, which measures
Domain, Application and Infrastructure only (the classes generated from the XSD and the tests are excluded).
"""
from __future__ import annotations

import sys
import xml.etree.ElementTree as ET


def main() -> int:
    report = sys.argv[1]
    minimum = float(sys.argv[2]) if len(sys.argv) > 2 else 80.0
    root = ET.parse(report).getroot()
    total = float(root.get("line-rate", 0)) * 100
    print(f"{'Project':<32} {'lines':>7} {'branches':>9}")
    for package in root.iter("package"):
        print(f"{package.get('name'):<32} {float(package.get('line-rate', 0)) * 100:6.1f}% {float(package.get('branch-rate', 0)) * 100:8.1f}%")
    print(f"{'Total':<32} {total:6.1f}% {float(root.get('branch-rate', 0)) * 100:8.1f}%  (minimum {minimum:.0f}% of lines)")
    if total < minimum:
        print(f"FAIL: line coverage {total:.1f}% is below {minimum:.0f}%")
        return 1
    print("OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
