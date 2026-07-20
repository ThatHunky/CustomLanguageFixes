# Солов'їна Долина (Solovyina Dolyna)

> **Статус: у розробці, ще не опубліковано.** Реліз на Nexus Mods буде після фінального тестування. Технічна назва (manifest, DLL, UniqueID) — латинкою: `Solovyina Dolyna` / `ThatHunky.SolovyinaDolyna`.

SMAPI-мод для **Android-версії Stardew Valley**, який лагодить усе, що мобільний порт ламає для кастомних (мод-) мов — насамперед для українського перекладу [Pereclaw.ukrainizacija]. Один мод замість розсипу дрібних фіксів. «Солов'їна мова» + Stardew Valley = Солов'їна Долина.

Перевірено на: Stardew Valley **1.6.15.3** (Android build 245), SMAPI **4.3.2** ([форк NRTnarathip](https://github.com/NRTnarathip/SMAPI-Android-1.6)).

## Що робить

| Фіча | Проблема | Як полагоджено |
|---|---|---|
| **24h годинник на HUD** | Мобільний `DayTimeMoneyBox.draw()` — форкнутий код без гілки `mod` у switch по мовах: кастомна мова падає в default = 12h без am/pm | Harmony prefix: на час малювання HUD підміняється `_currentLangCode` `mod` → `de` (німецька гілка вже 24h), finalizer відкочує назад |
| **Мод-мови у меню вибору мов** | Вбудоване мобільне `LanguageSelectionMenu` показує лише 12 зашитих мов | Постфікси на `SetupButtons`/`draw`/`releaseLeftClick`: рядки мод-мов домальовуються після MAGYAR з рідними `ButtonTexture` паків, скрол розширено, тапи ловляться. Кілька мов-паків одночасно — кожен своїм рядком |
| **Пам'ять вибору мови** | Автоперемикання силою повертало укр., навіть якщо юзер свідомо вибрав іншу мову | Вибір (у т.ч. вбудованої мови) пишеться в `config.json` (`PreferredLanguage`) + у преференси гри й поважається при старті |
| **«(Рецепт)» / «(Креслення)»** | Суфікс «(Recipe)» у назвах предметів не перекладався | Постфікс на `Object.DisplayName`: їжа (Category −7) → «(Рецепт)», решта → «(Креслення)». Тільки для мов з кодом `uk` — тайцям та іншим не заважає. Логіка — порт мода DID.RecipeUkrainizacija під Android |
| **Зум шрифта** | Мобільний `SpriteText.SetFontPixelZoom()` без гілки `mod` затирає зум пака (3.3 → 3) | Постфікс відновлює `FontPixelZoom` пака після кожного перерахунку |
| **Justify у діалогах** | Greedy word-wrap лишає рваний правий край (українські слова довгі) | Власний рендерер поверх ванільного: пробіли розтягуються до рівного краю (макс 1.5× ширини пробілу), останній рядок абзацу не чіпається, друкарська машинка й варіанти відповідей працюють як ванільні, будь-який exception → мовчазний fallback |
| **Назви клунків Джунімо** | Клунки лишаються англійськими до збереження+перезаходу: `Data\Bundles` кешується англійським ще до застосування мод-мови, а кеш назв у `NetWorldState` перебудовується лише раз за сесію | На `SaveLoaded` (і при зміні мови в сесії) інвалідується кеш `Data/Bundles` + `Strings/BundleNames`, кеш назв позначається брудним → `UpdateBundleDisplayNames()` перечитує вже локалізований асет. Прогрес і remixed-бандли не зачіпаються |

Усі патчі гейтяться на `CurrentLanguageCode == mod` — вбудованим мовам мод нічого не змінює.

## Встановлення

1. Постав [SMAPI Launcher](https://github.com/NRTnarathip/SMAPI-Android-1.6) і гру 1.6.15.1+.
2. Розпакуй zip з [releases/](releases/) у `StardewValley/Mods/` (поряд з укр. перекладом).
3. Перезапусти гру через SMAPI Launcher. Мова вибирається у меню мов (бульбашка на титулці) — «УКРАЇНСЬКА» буде внизу списку.

## Збірка

Потрібні **андроїдні** DLL з APK (десктопні не підійдуть — мобільний UI форкнутий):

```bash
# витягнути APK з телефона і розпакувати асембліки (формат XALZ)
adb shell pm path com.chucklefish.stardewvalley
adb pull /data/app/.../base.apk
unzip base.apk -d apk/ && pyxamstore unpack -d apk/assemblies/
```

Поклади `StardewValley.dll`, `StardewValley.GameData.dll`, `StardewModdingAPI.dll` у `src/SolovyinaDolyna/libs/` (тека в .gitignore), далі:

```bash
cd src/SolovyinaDolyna && dotnet build -c Release
```

Таргет `net9.0`, Harmony 2.3.3 (з рантайму SMAPI, у збірку не пакується).

## Структура репозиторію

```
src/SolovyinaDolyna/     — сорси мода (ModEntry + ClockPatch + RecipePatch,
                           LangMenuPatch, FontPatch, JustifyPatch)
legacy/Mobile24hClockFix/— перший standalone-фікс годинника; функціонал повністю
                           поглинутий ClockPatch, лишений як історія/мінімальний приклад
releases/                — зібрані zip (поточний — ще під старою робочою назвою
                           UkrainizacijaPlus; перший zip з новою назвою з'явиться
                           з наступною збіркою)
docs/Chat-history.md     — повна історія діагностики (декомпіл, пошук бага, ітерації)
docs/ImproveGame-PR-description.md — опис PR до NRTnarathip/ImproveGame: зняти хардкод
                           тайського ID з фікса годинника, щоб він працював для всіх мод-мов
tools/stardew-font-editor.html — редактор .fnt-шрифтів гри (відкривається у браузері);
                           онлайн: https://stardew-fonts.dobrovolskyi.com.ua
```

## Історія версій

- **1.5.2** — клунки, раунд 3: пак вмикає переклад `Data/Bundles.uk` за умовою стану сейва (зустрів клунки чи ні), тож CP підміняє асет посеред сесії — тепер ловимо `AssetsInvalidated` і перебудовуємо назви одразу, а не лише при завантаженні сейва
- **1.5.1** — фікс фікса клунків: чистимо статичний `localizedAssetNames` (він переживає InvalidateCache); перемикання між двома мод-мовами тепер викликає `TranslateFields()` (гра сама цього не робить, бо код мови не змінюється)
- **1.5.0** — локалізовані назви клунків Джунімо без ритуалу «збережись і перезайди»
- **1.4.0** — justify для діалогів; перейменування Ukrainizacija Plus → Солов'їна Долина
- **1.3.0** — фікс зуму шрифта (`SetFontPixelZoom`)
- **1.2.x** — мод-мови у вбудованому меню мов (1.2.1 — плавний скрол через скісор скролбокса)
- **1.1.0** — перемикач мов, мульти-паки, пам'ять вибору
- **1.0.x** — 24h годинник + Рецепт/Креслення (порт DID.RecipeUkrainizacija під Android)

## Підтримати

Мод безплатний і таким лишиться. Якщо хочеться подякувати:

[![Донат — монобанка](https://img.shields.io/badge/%D0%9F%D1%96%D0%B4%D1%82%D1%80%D0%B8%D0%BC%D0%B0%D1%82%D0%B8-%D0%BC%D0%BE%D0%BD%D0%BE%D0%B1%D0%B0%D0%BD%D0%BA%D0%B0-black?style=for-the-badge)](https://send.monobank.ua/jar/9WQuPLcBwx)

Картка банки: `4874 1000 3082 2038`

## Ліцензія

Код мода — [MIT](LICENSE). Ліцензія покриває лише код у цьому репозиторії; файли гри (DLL, ресурси) належать ConcernedApe і в репо не входять.

## Подяки

- **Pereclaw** — український переклад `Pereclaw.ukrainizacija`, з ідеально прописаним `content.json` (баг був у грі, не в паку)
- **DID** — оригінальний RecipeUkrainizacija (десктоп)
- **NRTnarathip** — SMAPI для Android і ImproveGame; фікс годинника по-хорошому має жити там (див. `docs/ImproveGame-PR-description.md`)
