# Custom Language Fixes

> **Status: in development, not yet released on Nexus Mods.** Previously known as «Солов'їна Долина» (Solovyina Dolyna) — still the Ukrainian name of the project.

Two SMAPI mods that fix what Stardew Valley breaks for **custom (mod) languages** — any language pack, not just Ukrainian. Everything is gated on «a custom language is active», so vanilla languages are never touched.

| Mod | Platform | What it's for |
|---|---|---|
| **Custom Language Fixes (Android)** | Android (SMAPI [fork by NRTnarathip](https://github.com/NRTnarathip/SMAPI-Android-1.6)) | The mobile port forks a lot of UI code and forgets the `mod` language branch — seven separate breakages |
| **Custom Language Bundle Fix** | PC (Windows/Linux/macOS) | On PC only one thing breaks: Junimo bundle names stay English |

Tested on Stardew Valley **1.6.15.3** (Android build 245, SMAPI 4.3.2) and **1.6.15** (PC).

## Custom Language Fixes (Android)

| Feature | Problem | Fix |
|---|---|---|
| **24h HUD clock** | The mobile `DayTimeMoneyBox.draw()` is forked code with no `mod` branch in its language switch, so custom languages fall through to the 12h default | Harmony prefix swaps `_currentLangCode` from `mod` to `de` (whose branch is already 24h) for the duration of the draw call; a finalizer restores it |
| **Custom languages in the language menu** | The built-in mobile `LanguageSelectionMenu` only lists 12 hardcoded languages | Postfixes on `SetupButtons`/`draw`/`releaseLeftClick` append a row per installed pack (using each pack's own `ButtonTexture`), extend the scroll area, and handle taps. Multiple packs each get their own row |
| **Language memory** | Auto-switching forced the pack language back even when the player deliberately picked another one | The choice (including vanilla languages) is written to `config.json` and to the game's preferences, and respected on startup |
| **Localized «(Recipe)» suffix** | The `(Recipe)` suffix on item names was left untranslated | Postfix on `Object.DisplayName`: food (category −7) gets «(Рецепт)», everything else «(Креслення)». Only runs for packs with language code `uk` — other languages are unaffected. Ported from DID's desktop mod RecipeUkrainizacija |
| **Font zoom** | Mobile `SpriteText.SetFontPixelZoom()` has no `mod` branch and overwrites the pack's zoom (3.3 → 3) | Postfix restores the pack's `FontPixelZoom` after every recalculation |
| **Justified dialogue** | Greedy word wrap leaves a ragged right edge, which is very visible with long words | Custom renderer on top of the vanilla one: spaces stretch to even out the edge (max 1.5× space width), the last line of a paragraph is left alone, typewriter effect and response options behave as usual, and any exception falls back to vanilla silently |
| **Junimo bundle names** | Bundles stay English until you save and reload: `Data/Bundles` is cached in English before the custom language is applied, and `NetWorldState`'s name cache is rebuilt only once per session | On `SaveLoaded` (and whenever the language changes mid-session) the mod clears the static localized-asset resolver, invalidates `Data/Bundles` + `Strings/BundleNames`, and marks the name cache dirty so `UpdateBundleDisplayNames()` re-reads the localized asset. It also listens for mid-session asset invalidations, because a pack may enable its bundle translation based on save state. Bundle progress and remixed bundles are never touched — `SetBundleData` is never called |

## Custom Language Bundle Fix (PC)

One feature: the Junimo bundle name fix described above. The rest of the Android list is mobile-only — the desktop game already handles custom languages correctly in its menus, clock, and font handling.

The bundle bug is shared, though: the desktop `NetWorldState` has the same `_bundleDataDirty` / `UpdateBundleDisplayNames()` internals, so both mods compile the same `src/Shared/BundlePatch.cs`.

## Installation

**Android:** install the [SMAPI Launcher](https://github.com/NRTnarathip/SMAPI-Android-1.6) and game 1.6.15.1+, unzip `CustomLanguageFixes-2.0.0.zip` from [releases/](releases/) into `StardewValley/Mods/` (next to your language pack), and restart through the launcher. Pick your language from the globe button on the title screen — custom languages are at the bottom of the list.

**PC:** install [SMAPI](https://smapi.io), unzip `CustomLanguageBundleFix-1.0.0.zip` into `Stardew Valley/Mods/`, run the game through SMAPI.

**Optional (Android):** install [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (Android port: NRTnarathip's [StardewValleyMods-Android](https://github.com/NRTnarathip/StardewValleyMods-Android)) to change settings in-game instead of editing `config.json`. The PC mod has no menu — its only setting is an escape hatch nobody needs to touch.

## Configuration

Every feature can be switched off. Edit `config.json` in the mod folder, or use the in-game GMCM menu.

**Custom Language Fixes (Android):**

| Setting | Default | In GMCM | Meaning |
|---|---|---|---|
| `Clock` | `"24h"` | yes | `"24h"` = 24-hour clock for custom languages; `"12h"` = vanilla behavior |
| `RecipeSuffix` | `true` | yes | Localized `(Recipe)` suffix (only active for `uk` packs) |
| `FontZoomFix` | `true` | yes | Keep the pack's font zoom |
| `JustifyDialogue` | `true` | yes | Justified dialogue text |
| `PreferredLanguage` | `""` | no | `""` = auto (first installed pack), `"en"` = English, or a pack ID such as `"Pereclaw.ukrainizacija"`. Set by picking a language in the game's language menu |
| `LanguageMenu` | `true` | no | Show custom languages in the built-in language menu |
| `BundleNamesFix` | `true` | no | Junimo bundle name fix |

The last three are config-only escape hatches for troubleshooting a mod conflict — there's no reason to turn them off in normal play, and flipping them mid-session doesn't fully take effect (the language menu re-reads its setting the next time it opens, and already-localized bundle names only revert after a save reload).

**Custom Language Bundle Fix (PC):** a single `BundleNamesFix` setting in `config.json`, `true` by default. No in-game menu — there is nothing worth switching off.

## Building

**Android** needs the *mobile* assemblies — desktop ones won't do, since the mobile UI code is forked. Extract them from the APK:

```bash
adb shell pm path com.chucklefish.stardewvalley
adb pull /data/app/.../base.apk
unzip base.apk -d apk/ && pyxamstore unpack -d apk/assemblies/
```

Put `StardewValley.dll`, `StardewValley.GameData.dll`, `StardewModdingAPI.dll` and `SMAPI.Toolkit.CoreInterfaces.dll` into `src/CustomLanguageFixes/libs/` (gitignored — game files are ConcernedApe's copyright), then:

```bash
cd src/CustomLanguageFixes && dotnet build -c Release
```

**PC** needs `Stardew Valley.dll`, `StardewValley.GameData.dll`, `MonoGame.Framework.dll` from your game folder plus the same two SMAPI DLLs, all in `src/CustomLanguageBundleFix/libs/`:

```bash
cd src/CustomLanguageBundleFix && dotnet build -c Release
```

Both SMAPI DLLs come from the official SMAPI installer (`internal/windows/install.dat` is a zip containing them). Targets: `net9.0` (Android), `net6.0` (PC), Harmony 2.3.3 from the SMAPI runtime.

Package releases with `python -X utf8 tools/pack_releases.py` — **never** with PowerShell `Compress-Archive`, which writes backslash paths that Android unpacks into broken files.

## Repository structure

```
src/Shared/                    — code shared by both mods (BundlePatch, GMCM API interface)
src/CustomLanguageFixes/       — Android mod (ModEntry + Clock, LangMenu, Recipe, Font, Justify patches)
src/CustomLanguageBundleFix/   — PC mod (bundle fix only)
legacy/Mobile24hClockFix/      — the original standalone clock fix, superseded by ClockPatch,
                                 kept as history and as a minimal example
releases/                      — built zips
tools/pack_releases.py         — release packaging (python zipfile, forward slashes)
docs/superpowers/specs/        — design docs
docs/superpowers/plans/        — implementation plans
docs/Chat-history.md           — full diagnosis history (decompilation, bug hunt, iterations)
docs/ImproveGame-PR-description.md — PR description for NRTnarathip/ImproveGame: drop the hardcoded
                                 Thai ID from the clock fix so it works for every custom language
tools/stardew-font-editor.html  — in-browser editor for the game's .fnt fonts;
                                 online: https://stardew-fonts.dobrovolskyi.com.ua
```

## Version history

- **2.0.1** — GMCM menu trimmed to the four settings worth changing (`Clock` renamed to «Time format»); the language-menu and bundle-fix switches stay in `config.json` only. PC mod (1.0.1) drops its GMCM menu entirely
- **2.0.0** — universal release: renamed from Солов'їна Долина, English-first with i18n (`default.json` + `uk.json`), per-feature config switches, in-game GMCM menu, and a separate PC mod sharing the bundle fix
- **1.5.2** — bundles round 3: packs may gate their bundle translation on save state, so `AssetsInvalidated` is now handled and names rebuilt mid-session
- **1.5.1** — bundles round 2: clear the static `localizedAssetNames` resolver (it survives `InvalidateCache`) and call `TranslateFields()` on mod→mod switches, which the game itself never does because the language code doesn't change
- **1.5.0** — localized Junimo bundle names without the save-and-relog ritual
- **1.4.0** — justified dialogue; renamed Ukrainizacija Plus → Солов'їна Долина
- **1.3.0** — font zoom fix (`SetFontPixelZoom`)
- **1.2.x** — custom languages in the built-in language menu (1.2.1 — smooth scrolling via the scrollbox scissor)
- **1.1.0** — language switcher, multiple packs, remembered choice
- **1.0.x** — 24h clock + Recipe/Blueprint suffix (port of DID.RecipeUkrainizacija to Android)

## Support

The mod is free and will stay free. If you'd like to say thanks:

[![Donate — monobank](https://img.shields.io/badge/Support-monobank-black?style=for-the-badge)](https://send.monobank.ua/jar/9WQuPLcBwx)

Card: `4874 1000 3082 2038`

## License

[MIT](LICENSE). The license covers only the code in this repository; game files (DLLs, assets) belong to ConcernedApe and are not included.

## Credits

- **Pereclaw** — the `Pereclaw.ukrainizacija` Ukrainian translation, with a perfectly written `content.json` (the bug was in the game, not the pack)
- **DID** — the original desktop RecipeUkrainizacija, and all the mobile testing
- **NRTnarathip** — SMAPI for Android, ImproveGame, and the GMCM port; the clock fix arguably belongs there (see `docs/ImproveGame-PR-description.md`)
- **spacechase0** — Generic Mod Config Menu

---

## Українською

**Два SMAPI-моди, які лагодять те, що гра ламає для кастомних (мод-) мов** — для будь-якого мовного пака, не лише українського. Раніше проєкт називався «Солов'їна Долина»; українська назва лишається.

- **Custom Language Fixes (Android)** — сім фіксів мобільного порту: 24-годинний годинник на HUD, мод-мови у вбудованому меню вибору мов, пам'ять вибору мови, суфікс «(Рецепт)/(Креслення)», зум шрифта пака, рівний правий край у діалогах і локалізовані назви клунків Джунімо.
- **Custom Language Bundle Fix (ПК)** — лише назви клунків: на десктопі решта працює з коробки.

**Статус:** у розробці, на Nexus ще не опубліковано.

**Встановлення (Android):** постав [SMAPI Launcher](https://github.com/NRTnarathip/SMAPI-Android-1.6), розпакуй `CustomLanguageFixes-2.0.0.zip` з [releases/](releases/) у `StardewValley/Mods/` поряд з мовним паком, перезапусти гру через лаунчер. Мова вибирається кнопкою-бульбашкою на титулці — кастомні мови внизу списку.

**Встановлення (ПК):** постав [SMAPI](https://smapi.io), розпакуй `CustomLanguageBundleFix-1.0.0.zip` у `Stardew Valley/Mods/`.

**Налаштування:** кожну фічу можна вимкнути в `config.json` (таблиця вище) або в грі через [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) — на Android є порт від NRTnarathip.

**Про клунки:** якщо мовний пак вмикає переклад клунків залежно від стану сейва (як укр. пак Pereclaw — це захист від [SMAPI #812](https://github.com/Pathoschild/SMAPI/issues/812)), то у свіжому сейві назви стануть українськими після першої ночі — мод ловить момент, коли пак вмикає переклад, і перебудовує назви сам. Раніше для цього треба було зберегтися, вийти в меню і зайти заново.

**Підтримати:** [монобанка](https://send.monobank.ua/jar/9WQuPLcBwx), картка `4874 1000 3082 2038`.
