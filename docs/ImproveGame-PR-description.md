# PR: Apply HUD clock time format for any mod language, not just Thai

**Title:** `Apply HUD clock time format for any mod language, not just Thai`

**Body:**

## Problem
The mobile `DayTimeMoneyBox.draw()` has no `LanguageCode.mod` case in its time-format switch, so any custom language falls into the default branch: a 12-hour clock with no AM/PM marker. ImproveGame already fixes this via `DayTimeMoneyBoxThaiFormat`, but the patch is only applied when the loaded language pack is `ELL.StardewValleyTHAI`:

```csharp
if (targetModLanguage?.Id == "ELL.StardewValleyTHAI")
    DayTimeMoneyBoxThaiFormat.ApplyPatch(Instance.harmony);
```

Other language packs that correctly define `TimeFormat`/`ClockTimeFormat` in `Data/AdditionalLanguages` (e.g. the Ukrainian translation `Pereclaw.ukrainizacija`, which sets `"ClockTimeFormat": "[HOURS_24_00]:[MINUTES]"`) still get the broken vanilla clock.

## Fix
1. **ModEntry.cs** — apply the patch whenever the loaded mod language defines a non-empty `ClockTimeFormat` or `TimeFormat`, instead of matching a hardcoded pack ID. Packs that don't define a format are unaffected.
2. **DayTimeMoneyBoxThaiFormat.cs** — use `ClockTimeFormat` for the HUD clock (matching what desktop `DayTimeMoneyBox` does), falling back to `TimeFormat`.

## Testing
Verified against decompiled `StardewValley.dll` 1.6.15.3 (Android): `Game1.getTimeOfDayString()` already honors `CurrentModLanguage.TimeFormat` for dialogue text, so only the HUD path needs this. A standalone Harmony patch using the same approach was confirmed working in-game with the Ukrainian language pack (24h clock renders correctly, day names unaffected).
