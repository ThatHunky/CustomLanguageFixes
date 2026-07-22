# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Two SMAPI mods for Stardew Valley that fix what the game breaks for **custom (mod) languages** — any language pack, not just Ukrainian. See [README.md](README.md) for the user-facing feature list, install steps, and version history. This file covers what you need to *work on* the code.

## Build, package, verify

There is **no solution file** and **no test suite**. Each mod is built from its own `.csproj`, and verification is manual in-game (Android device / emulator or PC through SMAPI) — there is no `dotnet test`.

```bash
# Android mod (net9.0)
cd src/CustomLanguageFixes && dotnet build -c Release

# PC mod (net6.0)
cd src/CustomLanguageBundleFix && dotnet build -c Release

# Package both release zips (run from repo root)
python -X utf8 tools/pack_releases.py
```

**Builds fail without the gitignored `libs/` folder.** Each project references game + SMAPI DLLs by `HintPath` into its own `src/<Mod>/libs/`. These are ConcernedApe's copyright and never committed. Critically, the **Android mod needs the *mobile* assemblies** (1.6.15.x ships them in a .NET-Android AssemblyStore `.so`, extracted with `tools/extract_mobile_assemblies.py` — see README "Building"), not desktop ones, because the mobile UI is a separate fork. The PC mod uses the desktop `Stardew Valley.dll` (note the space in the filename). NuGet handles Harmony 2.3.3 and MonoGame at compile time (`ExcludeAssets="runtime"` — the actual runtime copies come from SMAPI).

**Never package with PowerShell `Compress-Archive`** — it writes backslash paths that Android unzips into broken `Dir\file` names, and SMAPI silently ignores such a mod. `tools/pack_releases.py` uses Python `zipfile` with forward slashes and asserts no backslashes are present. This is the single most important build gotcha.

## Architecture

**One shared source file, two mods.** `src/Shared/*.cs` is not a shared DLL — each `.csproj` pulls it in with `<Compile Include="..\Shared\**\*.cs" LinkBase="Shared" />`, so the same source compiles into both assemblies against different game DLLs. `BundlePatch.cs` (bundle names), `SocialPatch.cs` (social-page single status), `RecipePatch.cs` (the `uk` recipe suffix), and `IGenericModConfigMenuApi.cs` (a minimal hand-copied GMCM interface — the standard no-dependency SMAPI integration) live there.

**The one invariant that makes everything safe:** every patch and behavior is gated on `LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod`. Vanilla languages fall through to unmodified game code. When adding any patch, this gate is mandatory — it is the reason the mod can touch forked UnityUI code without risk to normal players. (`FontPatch` documents a subtlety: `CurrentModLanguage` can stay non-null after switching *away* from a mod language, so gate on the *code*, not on that field.)

**Why two mods / the Android-vs-PC asymmetry.** The mobile port forks a lot of UI code and forgets the `mod` branch; the desktop game handles custom languages correctly *except* for three shared game bugs (bundle names, the social-page single status, the recipe suffix). So:
- **`src/CustomLanguageFixes/` (Android)** = four mobile-only patch classes (`ClockPatch`, `LangMenuPatch`, `FontPatch`, `JustifyPatch`) + the three shared patches.
- **`src/CustomLanguageBundleFix/` (PC)** = the three shared patches only (`BundlePatch`, `SocialPatch`, `RecipePatch`).

**`ModEntry.Entry` is the wiring hub** (Android version). It reads config, applies each patch's `Apply(harmony)`, and subscribes three SMAPI events that carry the cross-cutting language logic:
- `GameLaunched` → register the GMCM menu if that mod is present.
- `Content.AssetReady` (on `Data/AdditionalLanguages`) → apply the player's saved `PreferredLanguage` on startup, respecting a deliberate vanilla choice.
- `LocalizedContentManager.OnLanguageChange` → remember the player's menu pick into `config.json` + game prefs, and re-localize bundles mid-session.

Mobile-only patches live in the Android project: `ClockPatch` (in `ModEntry.cs`), `LangMenuPatch`, `FontPatch`, `JustifyPatch` (own files). Cross-platform patches live in `src/Shared/`: `BundlePatch`, `SocialPatch`, `RecipePatch`. Each is a `static class` with an `Apply(...)` method — the shared ones take a `Func<bool>` "enabled" toggle so each mod passes its own config field. Follow that shape for new patches. (The PC mod, unlike Android, creates its own `Harmony` instance in `ModEntry.Entry` for `SocialPatch`/`RecipePatch`; `BundlePatch` is event-based and needs none.)

**Config model split.** `ModConfig` (Android) has two tiers, and this split is intentional: `Clock`, `FontZoomFix`, `JustifyDialogue` are shown in GMCM; `PreferredLanguage`, `RecipeSuffix`, `LanguageMenu`, `BundleNamesFix`, `SocialSingleFix` are **`config.json`-only escape hatches** for troubleshooting mod conflicts (some don't fully take effect mid-session anyway — see the comments in `ModConfig`). Don't add config-only switches to the GMCM registration in `OnGameLaunched` without a reason. The PC `ModConfig` carries the three shared toggles (`BundleNamesFix`, `SocialSingleFix`, `RecipeSuffix`), all config-only.

**All user-facing strings go through i18n.** GMCM labels/tooltips and every log message are `H.Translation.Get(...)` keys defined in `i18n/default.json` (English) and `i18n/uk.json`. Never hardcode a display or log string.

## Conventions & non-obvious patterns

- **Code comments are written in Ukrainian.** Match that when editing existing files; the project's working language for inline commentary is Ukrainian even though user-facing text is English-first.
- **Swap-and-restore via Harmony prefix + finalizer.** `ClockPatch` reflects into the private `LocalizedContentManager._currentLangCode`, changes it in the prefix (`mod`→`de` for 24h, `mod`→`en` for 12h), and a *finalizer* restores it so it's put back even if `draw` throws. In 12h mode it must cache the pack's am/pm strings *before* the swap (while still `mod`) and substitute them via a `LoadString` prefix. Reuse this prefix/finalizer discipline for any temporary global-state change.
- **Reentrancy guards are load-bearing.** `JustifyPatch._rendering` (its custom renderer calls `drawString` again), `ClockPatch._substituteAmPm`, and `BundlePatch._rebuilding` (its rebuild would otherwise re-trigger `AssetsInvalidated` forever) all exist to prevent recursion/loops. Keep them.
- **Fail silent to vanilla.** Patches wrap risky work in `try/catch` and fall back to the unmodified path (`JustifyPatch` returns `true` to run the original; `RecipePatch` swallows) rather than crash the game.
- **Bundle fix never calls `SetBundleData`.** It only clears `localizedAssetNames`, invalidates the assets, and sets `_bundleDataDirty = true` so the getter re-reads. Calling `SetBundleData` would wipe bundle progress / remixed bundles — do not.

## Bumping a release version

Version strings live in **three** places and must move together: `src/CustomLanguageFixes/manifest.json`, `src/CustomLanguageBundleFix/manifest.json`, and the zip names + `net9.0`/`net6.0` output paths hardcoded in `tools/pack_releases.py`.

## Git workflow

Commit **each logical change as its own commit** — don't lump unrelated edits together — and push when the work is done. This repo works directly on `main` (solo project, no PR flow).

## Layout pointers

- `legacy/Mobile24hClockFix/` — the original standalone clock fix, superseded by `ClockPatch`; kept as history and a minimal single-patch example. Not built for release.
- `docs/superpowers/specs/` and `docs/superpowers/plans/` — design docs and implementation plans.
- `docs/Chat-history.md` — the full decompilation / bug-hunt diagnosis history behind these fixes.
- `docs/nexus/` — Nexus Mods release descriptions (BBCode) and the release checklist.
