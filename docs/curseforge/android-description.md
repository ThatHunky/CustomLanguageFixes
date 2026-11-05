## Everything the Android port breaks for custom (mod) languages, fixed in one mod.

Stardew Valley's mobile port forks a lot of its UI code, and in several places that forked code simply has no branch for the **mod** language — the one every community translation uses. The result is a pile of small breakages that make custom translations feel broken. This mod fixes all of them.

It works with **any** language pack, not a specific one. Every patch is gated on «a custom language is active», so if you play in a built-in language this mod changes nothing at all.

## What it fixes

- **The HUD clock.** Custom languages fall through to a 12-hour clock with no am/pm suffix at all, so 9:00 and 21:00 look identical. You now get a proper 24-hour clock, or a real 12-hour clock with a localized am/pm suffix — your choice.
- **The language menu.** The built-in mobile language selection menu only lists 12 hardcoded languages, so installed packs are invisible. They're now added to the list, each with its own button graphic, and the menu scrolls properly. Multiple packs each get their own row.
- **Language memory.** Your choice is remembered and respected on startup — including a deliberate switch back to a built-in language.
- **Font zoom.** The mobile font code overwrites the zoom your pack asks for, which throws off text measurement and leaves gaps at the end of lines. The pack's zoom is now restored after every recalculation.
- **Junimo bundle names.** Bundle names stay English until you save, quit to the menu and reload, because the bundle data is cached in English before your language is applied. They're now localized immediately, without the ritual. Bundle progress and remixed bundles are never touched.
- **Social page: single male farmers.** In gendered languages the social page showed single male farmers (your own character, co-op farmhands) with the feminine single status — a first-party game bug where the farmer slot only ever reads the female status key. Each farmer now shows the correct gendered status.
- **Justified dialogue text.** Optional: spaces are stretched so dialogue has an even right edge, which looks much better in languages with long words.
- **Localized «(Recipe)» suffix.** For Ukrainian packs only — other languages are unaffected.

## Requirements

- **SMAPI for Android** — the fork by NRTnarathip ([SMAPI-Android-1.6](https://github.com/NRTnarathip/SMAPI-Android-1.6)). This mod does **not** work on PC — see «Custom Language Bundle Fix» for the desktop version of the bundle fix.
- **A language pack** — any custom language mod. Without one this mod has nothing to do.
- **Generic Mod Config Menu** (optional) — to change settings in-game instead of editing config.json.

## Installation

1. Install the SMAPI Launcher and Stardew Valley 1.6.15.1 or newer.
2. Unzip the download into your **Mods** folder, next to your language pack.
3. Start the game through the SMAPI Launcher. Pick your language from the globe button on the title screen — custom languages are at the bottom of the list.

## Configuration

Three settings are available in the in-game menu (with Generic Mod Config Menu installed): time format, font zoom fix, and justified dialogue. Everything else is on by default and can be switched off in **config.json** if you ever hit a conflict with another mod.

## Credits

- **Pereclaw** — the Ukrainian translation this was originally built for. The bugs were in the game, not in the pack.
- **DID** — the original desktop RecipeUkrainizacija, and all the mobile testing.
- **NRTnarathip** — SMAPI for Android and the Generic Mod Config Menu port.
- **spacechase0** — Generic Mod Config Menu.

Source code and full technical write-up: [github.com/ThatHunky/CustomLanguageFixes](https://github.com/ThatHunky/CustomLanguageFixes). Licensed MIT — feel free to reuse the code, and translations of the mod's own text are very welcome.

## Українською

Мод лагодить усе, що мобільна версія гри ламає для кастомних (мод-) мов — для будь-якого мовного пака, не лише українського. Раніше проєкт називався «Солов'їна Долина».

Що виправлено: годинник на HUD (тепер або 24-годинний, або 12-годинний із дп/пп — раніше кастомні мови взагалі не мали позначки), мод-мови у вбудованому меню вибору мов, пам'ять вибору мови, зум шрифта пака, назви клунків Джунімо одразу без «збережись і перезайди», правильний за статтю статус «самотній» на слоті фермера, рівний правий край у діалогах і суфікс «(Рецепт)/(Креслення)» для українських паків.

Потрібні SMAPI для Android (форк NRTnarathip) і будь-який мовний пак. Налаштування — у config.json або в грі через Generic Mod Config Menu.
