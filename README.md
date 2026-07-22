# Custom Language Fixes

> **Status: the Android mod is [live on Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/49539); the PC mod is in progress.** Previously known as «Солов'їна Долина» (Solovyina Dolyna) — still the Ukrainian name of the project.

Two SMAPI mods that fix what Stardew Valley breaks for **custom (mod) languages** — any language pack, not just Ukrainian. Everything is gated on «a custom language is active», so vanilla languages are never touched.

| Mod | Platform | What it's for |
|---|---|---|
| **Custom Language Fixes (Android)** | Android (SMAPI [fork by NRTnarathip](https://github.com/NRTnarathip/SMAPI-Android-1.6)) | The mobile port forks a lot of UI code and forgets the `mod` language branch — eight fixes (five mobile-only, three shared with PC) |
| **Custom Language Bundle Fix** | PC (Windows/Linux/macOS) | The three cross-platform fixes: Junimo bundle names, the social-page single status, and the «(Recipe)» suffix |

Tested on Stardew Valley **1.6.15.3** (Android build 245, SMAPI 4.3.2) and **1.6.15** (PC).

## Custom Language Fixes (Android)

| Feature | Problem | Fix |
|---|---|---|
| **HUD clock** | The mobile `DayTimeMoneyBox.draw()` is forked code with no `mod` branch in its language switch. Custom languages fall through to a 12-hour clock **with no am/pm suffix** — 9:00 and 21:00 look identical | Harmony prefix swaps `_currentLangCode` for the duration of the draw call, and a finalizer restores it: to `de` for a 24-hour clock (default), or to `en` for a real 12-hour clock. In 12-hour mode the am/pm strings would then resolve to English, so the pack's own translations are substituted for those two keys while the swap is active |
| **Custom languages in the language menu** | The built-in mobile `LanguageSelectionMenu` only lists 12 hardcoded languages | Postfixes on `SetupButtons`/`draw`/`releaseLeftClick` append a row per installed pack (using each pack's own `ButtonTexture`), extend the scroll area, and handle taps. Multiple packs each get their own row |
| **Language memory** | Auto-switching forced the pack language back even when the player deliberately picked another one | The choice (including vanilla languages) is written to `config.json` and to the game's preferences, and respected on startup |
| **Localized «(Recipe)» suffix** | The `(Recipe)` suffix on item names was left untranslated | Postfix on `Object.DisplayName`: food (category −7) gets «(Рецепт)», everything else «(Креслення)». Only runs for packs with language code `uk` — other languages are unaffected. Ported from DID's desktop mod RecipeUkrainizacija |
| **Font zoom** | Mobile `SpriteText.SetFontPixelZoom()` has no `mod` branch and overwrites the pack's zoom (3.3 → 3) | Postfix restores the pack's `FontPixelZoom` after every recalculation |
| **Justified dialogue** | Greedy word wrap leaves a ragged right edge, which is very visible with long words | Custom renderer on top of the vanilla one: spaces stretch to even out the edge (max 1.5× space width), the last line of a paragraph is left alone, typewriter effect and response options behave as usual, and any exception falls back to vanilla silently |
| **Social page: single farmers** | Single **male** farmers (your own character, co-op farmhands) show the **feminine** single status in gendered languages: `SocialPage.drawFarmerSlot` only reads `..._Single_Female` and splits it on `/`, so a pack with separate `_Male`/`_Female` strings gets the female form for men. A first-party bug ([forum report](https://forums.stardewvalley.net/threads/53790/)), identical on mobile and desktop | While the farmer slot draws, substitute `LoadString(…Single_Female)` with the pack's gender-correct string (`_Male` for men). It has no `/`, so the game's split is a no-op — works for any custom language, on both platforms |
| **Junimo bundle names** | Bundles stay English until you save and reload: `Data/Bundles` is cached in English before the custom language is applied, and `NetWorldState`'s name cache is rebuilt only once per session | On `SaveLoaded` (and whenever the language changes mid-session) the mod clears the static localized-asset resolver, invalidates `Data/Bundles` + `Strings/BundleNames`, and marks the name cache dirty so `UpdateBundleDisplayNames()` re-reads the localized asset. It also listens for mid-session asset invalidations, because a pack may enable its bundle translation based on save state. Bundle progress and remixed bundles are never touched — `SetBundleData` is never called |

## Custom Language Bundle Fix (PC)

Three of the fixes above are shared game bugs — **bundle names**, the **social-page single status**, and the **«(Recipe)» suffix** — so the PC mod compiles them from `src/Shared/` (`BundlePatch`, `SocialPatch`, `RecipePatch`). The rest of the Android list is mobile-only: the desktop game handles custom languages correctly in its menus, clock, and font handling.

## Installation

**Android:** install the [SMAPI Launcher](https://github.com/NRTnarathip/SMAPI-Android-1.6) and game 1.6.15.1+, unzip `CustomLanguageFixes-2.2.0.zip` from [releases/](releases/) into `StardewValley/Mods/` (next to your language pack), and restart through the launcher. Pick your language from the globe button on the title screen — custom languages are at the bottom of the list.

**PC:** install [SMAPI](https://smapi.io), unzip `CustomLanguageBundleFix-1.1.0.zip` into `Stardew Valley/Mods/`, run the game through SMAPI.

**Optional (Android):** install [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (Android port: NRTnarathip's [StardewValleyMods-Android](https://github.com/NRTnarathip/StardewValleyMods-Android)) to change settings in-game instead of editing `config.json`. The PC mod has no menu — its only setting is an escape hatch nobody needs to touch.

## Configuration

Every feature can be switched off. Edit `config.json` in the mod folder, or use the in-game GMCM menu.

**Custom Language Fixes (Android):**

| Setting | Default | In GMCM | Meaning |
|---|---|---|---|
| `Clock` | `"24h"` | yes | `"24h"` = 21:00; `"12h"` = 9:00 with a localized am/pm suffix |
| `FontZoomFix` | `true` | yes | Keep the pack's font zoom |
| `JustifyDialogue` | `true` | yes | Justified dialogue text |
| `PreferredLanguage` | `""` | no | `""` = auto (first installed pack), `"en"` = English, or a pack ID such as `"Pereclaw.ukrainizacija"`. Set by picking a language in the game's language menu |
| `RecipeSuffix` | `true` | no | Localized `(Recipe)` suffix (only active for `uk` packs) |
| `LanguageMenu` | `true` | no | Show custom languages in the built-in language menu |
| `BundleNamesFix` | `true` | no | Junimo bundle name fix |
| `SocialSingleFix` | `true` | no | Correct single-status gender on the social page |

The last five are config-only escape hatches for troubleshooting a mod conflict — there's no reason to turn them off in normal play. Two of them wouldn't fully take effect mid-session anyway: the language menu re-reads its setting the next time it opens, and already-localized bundle names only revert after a save reload.

**Custom Language Bundle Fix (PC):** `config.json` only, no in-game menu — `BundleNamesFix`, `SocialSingleFix`, and `RecipeSuffix`, all `true` by default (same meanings as the Android table above).

## Building

**Android** needs the *mobile* assemblies — desktop ones won't do, since the mobile UI code is forked. Recent APKs (1.6.15.x) are built with .NET-Android, so the assemblies live inside `lib/<arch>/libassemblies.<arch>.blob.so` (an ELF AssemblyStore), *not* the classic Xamarin `assemblies.blob` that `pyxamstore` reads. Extract them with the bundled script:

```bash
py -m pip install lz4
py tools/extract_mobile_assemblies.py path/to/stardew.apk src/CustomLanguageFixes/libs/
```

That writes the mobile `StardewValley.dll` + `StardewValley.GameData.dll` straight into `libs/` (gitignored — game files are ConcernedApe's copyright). Add `StardewModdingAPI.dll` and `SMAPI.Toolkit.CoreInterfaces.dll` (from the SMAPI installer's `install.dat`, see below) to the same folder, then:

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
src/Shared/                    — code shared by both mods (Bundle, Social, Recipe patches + GMCM API interface)
src/CustomLanguageFixes/       — Android mod (ModEntry + Clock, LangMenu, Font, Justify + the shared patches)
src/CustomLanguageBundleFix/   — PC mod (shared Bundle/Social/Recipe patches)
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

- **2.2.0** (Android) / **1.1.0** (PC) — social-page single-status fix: single male farmers no longer show the feminine status (a first-party bug, [forum report](https://forums.stardewvalley.net/threads/53790/)). Both the social fix and the «(Recipe)» suffix move to shared code, so the PC mod now does three things (bundles, social, recipe), not one
- **2.1.0** — the 12-hour clock option now actually works: custom languages used to get 12-hour numbers with no am/pm at all, so the mod now swaps to the English branch and substitutes the pack's own am/pm strings. GMCM menu down to three settings with localized dropdown values; «(Recipe)» suffix is config-only now, since it's part of the translation rather than a preference. Mod renamed from «Custom Language Fixes (Android)» to «Custom Language Fixes»
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

- **Custom Language Fixes (Android)** — вісім фіксів: 24-годинний годинник на HUD, мод-мови у вбудованому меню вибору мов, пам'ять вибору мови, зум шрифта пака, рівний правий край у діалогах (суто мобільні), а також спільні з ПК — назви клунків Джунімо, статус «самотній» за статтю на слоті фермера і суфікс «(Рецепт)/(Креслення)».
- **Custom Language Bundle Fix (ПК)** — три спільні фікси: назви клунків, статус «самотній» на слоті фермера і суфікс рецептів. Решта з Android-списку — суто мобільна.

**Статус:** у розробці, на Nexus ще не опубліковано.

**Встановлення (Android):** постав [SMAPI Launcher](https://github.com/NRTnarathip/SMAPI-Android-1.6), розпакуй `CustomLanguageFixes-2.2.0.zip` з [releases/](releases/) у `StardewValley/Mods/` поряд з мовним паком, перезапусти гру через лаунчер. Мова вибирається кнопкою-бульбашкою на титулці — кастомні мови внизу списку.

**Встановлення (ПК):** постав [SMAPI](https://smapi.io), розпакуй `CustomLanguageBundleFix-1.1.0.zip` у `Stardew Valley/Mods/`.

**Налаштування:** кожну фічу можна вимкнути в `config.json` (таблиця вище) або в грі через [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) — на Android є порт від NRTnarathip.

**Про клунки:** якщо мовний пак вмикає переклад клунків залежно від стану сейва (як укр. пак Pereclaw — це захист від [SMAPI #812](https://github.com/Pathoschild/SMAPI/issues/812)), то у свіжому сейві назви стануть українськими після першої ночі — мод ловить момент, коли пак вмикає переклад, і перебудовує назви сам. Раніше для цього треба було зберегтися, вийти в меню і зайти заново.

**Підтримати:** [монобанка](https://send.monobank.ua/jar/9WQuPLcBwx), картка `4874 1000 3082 2038`.
