## The things Stardew Valley breaks for custom (mod) languages on PC — fixed.

If you play with a community translation (a custom «mod» language), a few things quietly break on the desktop game. This mod fixes all three. It works with **any** language pack; with a built-in game language it does nothing at all.

## What it fixes

- **Junimo bundle names — immediately.** Community Center bundle names stay English until you save, quit to the title screen and reload, because the game caches Data/Bundles in English before your language is applied. They're now localized as soon as the save loads. **Your bundle progress is never touched** — only display names are refreshed, so remixed bundles and completion are safe.
- **Social page: single male farmers.** In gendered languages the social page showed single **male** farmers (your own character, co-op farmhands) with the **feminine** single status. It's a first-party game bug — the farmer slot only ever reads the female status key and splits it, so a pack with separate male/female strings gets the female form for men. Each farmer now shows the correct gendered status.
- **Localized «(Recipe)» suffix.** For Ukrainian packs only: the game uses one «(Recipe)» key for both cooking recipes and crafting blueprints, which Ukrainian has to tell apart. Food gets «(Рецепт)», everything else «(Креслення)». Other languages are unaffected.

## Requirements

- **SMAPI** 4.0 or newer.
- **A language pack** — any custom language mod. With a built-in game language this mod does nothing.

## Installation

Unzip into your **Mods** folder and run the game through SMAPI. Everything is on by default; the three toggles live in **config.json** if you ever hit a conflict with another mod.

*On Android?* Use «Custom Language Fixes» instead — it includes these fixes plus everything else the mobile port breaks for custom languages.

Source code and full technical write-up: [github.com/ThatHunky/CustomLanguageFixes](https://github.com/ThatHunky/CustomLanguageFixes). Licensed MIT.

Credits: **Pereclaw** for the Ukrainian translation this was diagnosed against, and **DID** for testing.

## Українською

Три речі, які гра ламає для кастомних (мод-) мов на ПК — виправлено:

- назви клунків Джунімо одразу після завантаження сейва, без ритуалу «збережись, вийди в меню, зайди знову» (прогрес клунків не зачіпається);
- правильний за статтю статус «самотній» на слоті фермера — чоловічі фермери більше не показують «незаміжня» (ванільний баг гри);
- суфікс «(Рецепт)»/«(Креслення)» для українських паків — гра не розрізняє їжу від креслень, а мод розрізняє.

Потрібні SMAPI 4.0+ і будь-який мовний пак. Усе ввімкнено за замовчуванням; вимикачі — у config.json.
