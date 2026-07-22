"""Витягує мобільні StardewValley.dll + StardewValley.GameData.dll з APK у libs/.

Мобільна гра 1.6.15.x зібрана .NET-Android: асембліки лежать не у класичному
Xamarin `assemblies.blob` (який читає pyxamstore), а всередині ELF-файлу
`lib/<arch>/libassemblies.<arch>.blob.so` у форматі AssemblyStore — один заголовок
`XABA` + N блоків `XALZ` (кожен = magic 'XALZ' + u32 store_index + u32 uncompressed_len
+ LZ4-block дані). pyxamstore цей формат не бере, тож розпаковуємо самі.

Потрібен пакет lz4:  py -m pip install lz4

Використання:
    py tools/extract_mobile_assemblies.py <apk> <out_dir>
напр. py tools/extract_mobile_assemblies.py stardew.apk src/CustomLanguageFixes/libs
"""
import os
import struct
import sys
import zipfile

import lz4.block


def find_blob_so(apk_path):
    """Повертає (ім'я, байти) для libassemblies.*.blob.so, надаючи перевагу arm64-v8a."""
    with zipfile.ZipFile(apk_path) as z:
        cands = [n for n in z.namelist()
                 if n.startswith("lib/") and "libassemblies" in n and n.endswith(".blob.so")]
        if not cands:
            sys.exit("libassemblies.<arch>.blob.so не знайдено в APK — це точно .NET-Android збірка?")
        cands.sort(key=lambda n: (0 if "arm64" in n else 1, n))
        return cands[0], z.read(cands[0])


def unpack_assemblies(blob):
    """Розпаковує всі XALZ-блоки; повертає список байтів валідних PE (починаються з 'MZ')."""
    offs = []
    i = blob.find(b"XALZ")
    while i != -1:
        offs.append(i)
        i = blob.find(b"XALZ", i + 1)
    pes = []
    for k, off in enumerate(offs):
        end = offs[k + 1] if k + 1 < len(offs) else len(blob)
        _idx, unclen = struct.unpack_from("<II", blob, off + 4)
        try:
            raw = lz4.block.decompress(blob[off + 12:end], uncompressed_size=unclen)
        except Exception:
            continue  # хвостовий блок може впертись у трейлер ELF — нам він не потрібен
        if raw[:2] == b"MZ":
            pes.append(raw)
    return pes


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    apk_path, out_dir = sys.argv[1], sys.argv[2]
    os.makedirs(out_dir, exist_ok=True)

    name, blob = find_blob_so(apk_path)
    print(f"store: {name} ({len(blob):,} bytes)")
    pes = unpack_assemblies(blob)
    print(f"розпаковано PE-асемблік: {len(pes)}")

    # Імена в цьому форматі не зберігаються плейнтекстом — впізнаємо за вмістом:
    # головний асембліка містить мобільний тип MobileScrollbox; GameData — свою назву модуля.
    targets = {}
    for raw in pes:
        if "StardewValley.dll" not in targets and b"MobileScrollbox" in raw:
            targets["StardewValley.dll"] = raw
        if ("StardewValley.GameData.dll" not in targets
                and b"StardewValley.GameData.dll" in raw and b"MobileScrollbox" not in raw):
            targets["StardewValley.GameData.dll"] = raw

    for fname, raw in targets.items():
        with open(os.path.join(out_dir, fname), "wb") as f:
            f.write(raw)
        print(f"  SAVED {fname}: {len(raw):,} bytes -> {out_dir}")

    missing = {"StardewValley.dll", "StardewValley.GameData.dll"} - targets.keys()
    if missing:
        sys.exit(f"не знайдено: {', '.join(sorted(missing))}")
    print("готово. Додай ще StardewModdingAPI.dll + SMAPI.Toolkit.CoreInterfaces.dll з інсталятора SMAPI.")


if __name__ == "__main__":
    main()
