"""Пакує реліз-zip обох модів.

ТІЛЬКИ python zipfile: PowerShell Compress-Archive пише backslash-шляхи, які
Android розпаковує у битi файли "Dir\\file" — SMAPI такий мод ігнорує.
"""
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def pack(zip_name: str, folder: str, entries: list[tuple[str, str]]) -> None:
    out = ROOT / "releases" / zip_name
    out.parent.mkdir(exist_ok=True)
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for disk, arc in entries:
            z.write(ROOT / disk, f"{folder}/{arc}")
    with zipfile.ZipFile(out) as z:
        names = z.namelist()
        assert all("\\" not in n for n in names), f"backslash у шляхах: {names}"
        print(f"packed: {out.name}")
        for n in names:
            print("   ", n)


pack("CustomLanguageFixes-2.1.0.zip", "CustomLanguageFixes", [
    ("src/CustomLanguageFixes/bin/Release/net9.0/CustomLanguageFixes.dll", "CustomLanguageFixes.dll"),
    ("src/CustomLanguageFixes/manifest.json", "manifest.json"),
    ("src/CustomLanguageFixes/i18n/default.json", "i18n/default.json"),
    ("src/CustomLanguageFixes/i18n/uk.json", "i18n/uk.json"),
])
pack("CustomLanguageBundleFix-1.0.1.zip", "CustomLanguageBundleFix", [
    ("src/CustomLanguageBundleFix/bin/Release/net6.0/CustomLanguageBundleFix.dll", "CustomLanguageBundleFix.dll"),
    ("src/CustomLanguageBundleFix/manifest.json", "manifest.json"),
    ("src/CustomLanguageBundleFix/i18n/default.json", "i18n/default.json"),
    ("src/CustomLanguageBundleFix/i18n/uk.json", "i18n/uk.json"),
])
