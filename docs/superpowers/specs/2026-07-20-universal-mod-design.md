# Дизайн: Custom Language Fixes — універсалізація + ПК-версія

Дата: 2026-07-20. Статус: схвалено користувачем.

## Мета

Перетворити «Солов'їну Долину» (Android-мод для українського перекладу) на два універсальні моди
для **будь-якої** кастомної (мод-) мови:

1. **Custom Language Fixes (Android)** — повний набір фіч мобільного порта (наступник Солов'їної Долини).
2. **Custom Language Bundle Fix** — окремий мінімальний ПК-мод лише з фіксом назв клунків Джунімо.

Підстава: усі патчі вже гейтяться на `CurrentLanguageCode == mod`, а не на українську; на ПК
з мод-мовами ламаються лише клунки (меню мов, годинник, зум і justify там працюють з коробки).

## Назви та ідентифікатори

| | Android (повний) | ПК (мінімальний) |
|---|---|---|
| Name | Custom Language Fixes (Android) | Custom Language Bundle Fix |
| UniqueID | `ThatHunky.CustomLanguageFixes` | `ThatHunky.CustomLanguageBundleFix` |
| EntryDll | CustomLanguageFixes.dll | CustomLanguageBundleFix.dll |
| Стартова версія | 2.0.0 | 1.0.0 |
| Author | ThatHunky & DID | ThatHunky & DID |

- Обидві назви перевірені на Nexus (розділ Stardew Valley) 2026-07-20 через GraphQL-пошук — вільні.
- Репозиторій GitHub перейменовується `SolovyinaDolyna` → **`CustomLanguageFixes`**
  (GitHub редіректить старі URL автоматично).
- «Солов'їна Долина» лишається як українська назва в укр. секції README і в описі для укр. аудиторії.

## Архітектура: один репозиторій, спільне ядро, дві голови

```
src/
  Shared/
    BundlePatch.cs               ← фікс клунків, спільний для обох платформ
  CustomLanguageFixes/           ← Android: ModEntry + усі патчі (Clock, LangMenu, Recipe,
    CustomLanguageFixes.csproj      Font, Justify) + GMCM-реєстрація; збірка проти
    manifest.json                   андроїдних DLL у libs/ (як зараз)
    i18n/{default,uk}.json
    libs/  (gitignore)
  CustomLanguageBundleFix/       ← ПК: мінімальний ModEntry (BundlePatch + GMCM-toggle);
    CustomLanguageBundleFix.csproj  збірка через Pathoschild.Stardew.ModBuildConfig
    manifest.json                   (гру знаходить сам: D:\SteamLibrary\...\Stardew Valley)
    i18n/{default,uk}.json
```

- Спільні файли підключаються через `<Compile Include="..\Shared\*.cs" />`.
- **BundlePatch відв'язується від статиків ModEntry**: `Apply(IModHelper helper, IMonitor log)`;
  всередині — лише SMAPI API + ванільні типи (`NetWorldState`, `LocalizedContentManager.localizedAssetNames`,
  рефлексія по `_bundleDataDirty`). Логіка = поточна 1.5.2 (SaveLoaded-рефреш + AssetsInvalidated-хук +
  RefreshBundleNames при зміні мови в сесії).
- Укр-специфічний RecipePatch лишається в Android-моді, гейт `LanguageCode == "uk"` як зараз.

## Налаштування

`config.json` (Android-мод):

```json
{
  "PreferredLanguage": "",     // "" = авто (перша мод-мова), "en", або Id мод-мови
  "Clock": "24h",              // "24h" | "12h" (12h = патч вимкнено, поведінка ванілі)
  "LanguageMenu": true,        // мод-мови у вбудованому меню вибору мов
  "RecipeSuffix": true,        // (Рецепт)/(Креслення); діє лише для мов з кодом uk
  "FontZoomFix": true,
  "JustifyDialogue": true,
  "BundleNamesFix": true
}
```

- **GMCM-інтеграція (опційна залежність)** через `IGenericModConfigMenuApi`: якщо GMCM встановлено —
  всі перемикачі доступні в грі; зміни застосовуються без перезапуску там, де це можливо
  (Clock/Justify/Font/Recipe — одразу; LanguageMenu/BundleNamesFix — з наступного відкриття меню/рефрешу).
  GMCM на Android існує: порт NRTnarathip (`StardewValleyMods-Android/framework/GenericModConfigMenu`).
- Без GMCM все працює через `config.json`.
- ПК-мод: конфіг з одним перемикачем `BundleNamesFix` (+ той самий GMCM-toggle).

## i18n

- Рядки логів і GMCM-підписи — через SMAPI `helper.Translation` (`i18n/default.json` англійською,
  `i18n/uk.json` українською).
- manifest Description — англійською.
- README: основна частина англійською, внизу повна українська секція.

## Релізи

- `releases/CustomLanguageFixes-2.0.0.zip` (Android) — пакування python-скриптом zipfile
  з forward slashes (Compress-Archive заборонено — ламає шляхи на Android).
- `releases/CustomLanguageBundleFix-1.0.0.zip` (ПК).
- Nexus (після тестування): дві окремі сторінки, обидві з монобанкою.
- UpdateKeys: `GitHub:ThatHunky/CustomLanguageFixes` в обох manifest.

## Міграція тестувальників

- Стара тека `Mods/SolovyinaDolyna` видаляється вручну (UniqueID змінився).
- `PreferredLanguage` вибирається заново один раз.
- Для внутрішньоігрового меню DID ставить GMCM-zip з релізів NRTnarathip.

## Ризики та перевірки перед кодом

1. **ПК `NetWorldState`**: звірити декомпілом десктопної `Stardew Valley.dll` (D:\SteamLibrary), що
   `_bundleDataDirty` / `UpdateBundleDisplayNames()` збігаються з мобільними. Якщо ні — адаптувати
   рефлексію під обидві реалізації.
2. **GMCM API на Android-форку**: інтерфейс той самий, що на ПК; перевірити версію API в порту.
3. Фіча-гейти не змінюються — вбудованим мовам мод як не заважав, так і не заважає.

## Тестування

- Android: збірка → zip → DID (сейви «піпа» і «аоаооаа», пак Pereclaw; регресія всіх 7 фіч + GMCM).
- ПК: локально на десктопній грі з паком Pereclaw — сценарій клунків: новий сейв → зустріти клунки →
  назви українські одразу/після ночі; перевірити, що прогрес клунків не ламається (SMAPI issue #812:
  SetBundleData не викликаємо ніде).
