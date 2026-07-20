# Custom Language Fixes — план імплементації

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перетворити «Солов'їну Долину» на два універсальні моди: Custom Language Fixes (Android, 2.0.0, всі фічі + config/GMCM/i18n) і Custom Language Bundle Fix (ПК, 1.0.0, лише клунки), зі спільним BundlePatch і перейменованим репозиторієм.

**Architecture:** Один репозиторій; `src/Shared/BundlePatch.cs` підключається через `<Compile Include>` у дві голови: `src/CustomLanguageFixes` (Android, збірка проти андроїдних DLL у `libs/`) і `src/CustomLanguageBundleFix` (ПК, Pathoschild.Stardew.ModBuildConfig). Фічі гейтяться runtime-перевірками конфіга (щоб GMCM-перемикачі діяли без перезапуску), Harmony-патчі ставляться завжди.

**Tech Stack:** C#, net9.0 (Android) / net6.0 (ПК), SMAPI 4.x, Lib.Harmony 2.3.3, GMCM API (опційно), python zipfile для пакування.

**Спека:** `docs/superpowers/specs/2026-07-20-universal-mod-design.md`.

## Global Constraints

- Ідентифікатори: Android `ThatHunky.CustomLanguageFixes` / `CustomLanguageFixes.dll` / v2.0.0; ПК `ThatHunky.CustomLanguageBundleFix` / `CustomLanguageBundleFix.dll` / v1.0.0. Author в обох: `ThatHunky & DID`.
- НІКОЛИ не викликати `SetBundleData` (ламає сейви — SMAPI issue #812). НІКОЛИ не пакувати zip через Compress-Archive (backslash-шляхи ламають Android) — лише python zipfile з forward slashes.
- НІКОЛИ не комітити: `libs/`, `.env`, файли гри (*.apks, декомпіл). `libs/` вже в .gitignore.
- Git-акаунт: ThatHunky (`credential.helper` вже налаштований через gh). Коміт-меседжі українською + трейлер `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Тестова інфраструктура відсутня (Harmony-мод без ігрового рантайму) — «тест» кожної таски = `dotnet build -c Release` з «Build succeeded» (0 Warning не вимагається, 0 Error обов'язково) + фінальний мануальний чекліст (розділ «Мануальне тестування»).
- Десктопний `NetWorldState` звірено декомпілом 2026-07-20: `_bundleDataDirty`/`BundleData`/`UpdateBundleDisplayNames()` ідентичні мобільним — спільний код валідний (scratchpad/pc_NetWorldState.decompiled.cs).

---

### Task 1: Перейменувати Android-проєкт (тека, csproj, namespace, manifest)

**Files:**
- Rename: `src/SolovyinaDolyna/` → `src/CustomLanguageFixes/` (включно з негітованою `libs/`)
- Rename: `SolovyinaDolyna.csproj` → `CustomLanguageFixes.csproj`
- Modify: усі `src/CustomLanguageFixes/*.cs` (namespace), `CustomLanguageFixes.csproj` (AssemblyName), `manifest.json` (повністю)

**Interfaces:**
- Produces: namespace `CustomLanguageFixes` з класами `ModEntry` (статики `Log`, `Config`, `H`), `ModConfig`, `ClockPatch`, `RecipePatch`, `BundlePatch`, `FontPatch`, `JustifyPatch`, `LangMenuPatch`; Harmony ID `ThatHunky.CustomLanguageFixes`.

- [ ] **Step 1: Перейменувати теку і csproj, прибрати стару збірку**

```bash
cd "/c/Users/sysadmin/Documents/SV/StardewUkr+"
git mv src/SolovyinaDolyna src/CustomLanguageFixes
git mv src/CustomLanguageFixes/SolovyinaDolyna.csproj src/CustomLanguageFixes/CustomLanguageFixes.csproj
rm -rf src/CustomLanguageFixes/obj src/CustomLanguageFixes/bin
```
(`git mv` теки переносить і негітовану `libs/` фізично; перевірити `ls src/CustomLanguageFixes/libs` — 3 DLL на місці.)

- [ ] **Step 2: Замінити namespace і Harmony ID**

```bash
cd src/CustomLanguageFixes
sed -i 's/namespace SolovyinaDolyna/namespace CustomLanguageFixes/' *.cs
sed -i 's/ThatHunky\.SolovyinaDolyna/ThatHunky.CustomLanguageFixes/' ModEntry.cs
```
У `CustomLanguageFixes.csproj` замінити `<AssemblyName>SolovyinaDolyna</AssemblyName>` → `<AssemblyName>CustomLanguageFixes</AssemblyName>`.

- [ ] **Step 3: Переписати manifest.json повністю**

```json
{
  "Name": "Custom Language Fixes (Android)",
  "Author": "ThatHunky & DID",
  "Version": "2.0.0",
  "Description": "Fixes everything the Android port breaks for custom (mod) languages: language selection menu, 24h clock, font zoom, justified dialogue, and Junimo bundle names.",
  "UniqueID": "ThatHunky.CustomLanguageFixes",
  "EntryDll": "CustomLanguageFixes.dll",
  "MinimumApiVersion": "4.0.0",
  "UpdateKeys": ["GitHub:ThatHunky/CustomLanguageFixes"]
}
```

- [ ] **Step 4: Збірка**

Run: `cd src/CustomLanguageFixes && dotnet build -c Release`
Expected: `Build succeeded`, артефакт `bin/Release/net9.0/CustomLanguageFixes.dll`.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "refactor: перейменування SolovyinaDolyna -> CustomLanguageFixes (2.0.0)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Винести BundlePatch у src/Shared (відв'язати від статиків ModEntry)

**Files:**
- Create: `src/Shared/BundlePatch.cs`
- Modify: `src/CustomLanguageFixes/ModEntry.cs` (видалити внутрішній клас BundlePatch, оновити виклики), `src/CustomLanguageFixes/CustomLanguageFixes.csproj` (підключити Shared)

**Interfaces:**
- Produces: `CustomLanguage.Shared.BundlePatch` зі статичними методами `Apply(IModHelper helper, IMonitor log, Func<bool> enabled)` і `RefreshBundleNames()`. Обидві голови викликають лише ці два методи.

- [ ] **Step 1: Створити src/Shared/BundlePatch.cs**

Повний вміст (логіка = поточна 1.5.2, лише DI замість статиків ModEntry; рядки поки укр. хардкодом — Task 4 замінить на i18n):

```csharp
using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguage.Shared
{
    // Локалізовані назви клунків Джунімо без ритуалу «збережись і перезайди».
    // Платформонезалежний код: SMAPI-івенти + ванільні NetWorldState/LocalizedContentManager
    // (десктопний NetWorldState звірено декомпілом — внутрішній стан ідентичний мобільному).
    internal static class BundlePatch
    {
        private static IModHelper _helper;
        private static IMonitor _log;
        private static Func<bool> _enabled;
        private static bool _rebuilding;

        public static void Apply(IModHelper helper, IMonitor log, Func<bool> enabled)
        {
            _helper = helper;
            _log = log;
            _enabled = enabled;
            helper.Events.GameLoop.SaveLoaded += (_, _) => RefreshBundleNames();
            // мовний пак може вмикати переклад клунків за умовою стану сейва (напр., Pereclaw
            // гейтить Data/Bundles.uk), тож CP підміняє асет посеред сесії — ловимо і перебудовуємо
            helper.Events.Content.AssetsInvalidated += (_, e) =>
            {
                if (!_enabled() || !Context.IsWorldReady || _rebuilding)
                    return;
                foreach (var n in e.Names)
                {
                    string name = n.Name.Replace('\\', '/');
                    if (name.StartsWith("Data/Bundles", StringComparison.OrdinalIgnoreCase))
                    {
                        RebuildNames();
                        break;
                    }
                }
            };
        }

        // перебудувати кеш назв зі свіжого асета (без інвалідацій — щоб не зациклитись)
        private static void RebuildNames()
        {
            try
            {
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                var ws = Game1.netWorldState?.Value;
                if (ws == null)
                    return;
                AccessTools.Field(ws.GetType(), "_bundleDataDirty")?.SetValue(ws, true);
                _ = ws.BundleData;
                _log.Log("Назви клунків перебудовано після оновлення асета.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _log.Log("Не вдалося перебудувати назви клунків: " + ex.Message, LogLevel.Warn);
            }
        }

        public static void RefreshBundleNames()
        {
            _rebuilding = true;
            try
            {
                if (!_enabled())
                    return;
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                var ws = Game1.netWorldState?.Value;
                if (ws == null)
                    return;

                // статичний резолвер локалізованих асетів чиститься лише при зміні КОДУ мови
                // чи виході в меню; застиглий маршрут «Data\Bundles → база (en)» переживає
                // і перемикання mod→mod, і InvalidateCache — чистимо самі
                LocalizedContentManager.localizedAssetNames.Clear();

                _helper.GameContent.InvalidateCache("Data/Bundles");
                _helper.GameContent.InvalidateCache("Strings/BundleNames");

                // кеш назв позначаємо брудним — геттер сам перечитає локалізований асет через
                // UpdateBundleDisplayNames(). Прогрес/remixed не зачіпаються: SetBundleData НЕ викликаємо.
                AccessTools.Field(ws.GetType(), "_bundleDataDirty")?.SetValue(ws, true);
                _ = ws.BundleData;
                _log.Log("Назви клунків перелокалізовано.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _log.Log("Не вдалося оновити назви клунків: " + ex.Message, LogLevel.Warn);
            }
            finally
            {
                _rebuilding = false;
            }
        }
    }
}
```

- [ ] **Step 2: Прибрати старий BundlePatch з ModEntry.cs і оновити виклики**

У `ModEntry.cs`: видалити весь `internal static class BundlePatch {...}`; додати `using CustomLanguage.Shared;`; у `Entry` замінити `BundlePatch.Apply(helper);` на `BundlePatch.Apply(helper, this.Monitor, () => true);` (лямбда стане `() => Config.BundleNamesFix` у Task 3). Виклик `BundlePatch.RefreshBundleNames()` в `OnLanguageChanged` лишити як є.

- [ ] **Step 3: Підключити Shared у csproj**

У `CustomLanguageFixes.csproj` в `<ItemGroup>` з референсами додати:
```xml
<Compile Include="..\Shared\**\*.cs" LinkBase="Shared" />
```

- [ ] **Step 4: Збірка**

Run: `cd src/CustomLanguageFixes && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "refactor: BundlePatch у src/Shared, DI замість статиків ModEntry

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Config-перемикачі на кожну фічу

**Files:**
- Modify: `src/CustomLanguageFixes/ModEntry.cs` (ModConfig + гейти Clock/Recipe + лямбда BundlePatch), `FontPatch.cs`, `JustifyPatch.cs`, `LangMenuPatch.cs`

**Interfaces:**
- Produces: `ModConfig` з полями `PreferredLanguage:string=""`, `Clock:string="24h"`, `LanguageMenu:bool=true`, `RecipeSuffix:bool=true`, `FontZoomFix:bool=true`, `JustifyDialogue:bool=true`, `BundleNamesFix:bool=true`. Гейти читають `ModEntry.Config` при кожному виклику (runtime, сумісно з GMCM).

- [ ] **Step 1: Розширити ModConfig**

```csharp
public class ModConfig
{
    // "" = авто (перша мод-мова), "en" = англійська, або Id мод-мови (напр. "Pereclaw.ukrainizacija")
    public string PreferredLanguage { get; set; } = "";
    public string Clock { get; set; } = "24h";      // "24h" | "12h" (12h = ваніль)
    public bool LanguageMenu { get; set; } = true;   // мод-мови у вбудованому меню мов
    public bool RecipeSuffix { get; set; } = true;   // (Рецепт)/(Креслення), діє лише для uk
    public bool FontZoomFix { get; set; } = true;
    public bool JustifyDialogue { get; set; } = true;
    public bool BundleNamesFix { get; set; } = true;
}
```

- [ ] **Step 2: Додати runtime-гейти (по одному рядку на початку відповідного методу)**

- `ClockPatch.DrawPrefix` — перший рядок тіла (до читання `_currentLangCode`):
  ```csharp
  if (!string.Equals(ModEntry.Config.Clock, "24h", StringComparison.OrdinalIgnoreCase)) return;
  ```
  (у `ClockPatch` додати `using System;` за потреби — `StringComparison` вже доступний через існуючий `using System;` в ModEntry.cs, ClockPatch у тому ж файлі.)
- `RecipePatch.Postfix` — перший рядок try-блоку: `if (!ModEntry.Config.RecipeSuffix) return;`
- `FontPatch.Postfix` — перший рядок: `if (!ModEntry.Config.FontZoomFix) return;`
- `JustifyPatch.DrawStringPrefix` — в умову бейлу першим доданком: `if (!ModEntry.Config.JustifyDialogue || !_inDialogueDraw || _rendering || ...) return true;`
- `LangMenuPatch.SetupPostfix` — перший рядок: `if (!ModEntry.Config.LanguageMenu) { _langs = new(); _textures = null; return; }` (draw/click самі бейляться на `_langs.Count == 0`).
- `ModEntry.Entry` — лямбда: `BundlePatch.Apply(helper, this.Monitor, () => Config.BundleNamesFix);`

- [ ] **Step 3: Збірка**

Run: `cd src/CustomLanguageFixes && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: config-перемикачі для всіх фіч (Clock 24h/12h, гейти патчів)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: i18n (default.json англійською + uk.json)

**Files:**
- Create: `src/CustomLanguageFixes/i18n/default.json`, `src/CustomLanguageFixes/i18n/uk.json`
- Modify: `src/CustomLanguageFixes/ModEntry.cs`, `LangMenuPatch.cs`, `src/Shared/BundlePatch.cs` (рядки логів → Translation)

**Interfaces:**
- Produces: ключі i18n (нижче) — ними ж користується Task 5 (GMCM) і Task 6 (ПК-мод копіює bundle-ключі та config-bundles).

- [ ] **Step 1: Створити i18n/default.json**

```json
{
  "log.language-switched": "Language switched to: {{id}}",
  "log.language-switch-failed": "Failed to switch language: {{error}}",
  "log.language-picked": "Language selected in menu: {{id}}",
  "log.bundles-refreshed": "Bundle names relocalized.",
  "log.bundles-rebuilt": "Bundle names rebuilt after asset update.",
  "log.bundles-failed": "Failed to update bundle names: {{error}}",

  "config.clock.name": "HUD clock format",
  "config.clock.desc": "24h shows a 24-hour clock for custom languages; 12h keeps vanilla behavior.",
  "config.language-menu.name": "Custom languages in language menu",
  "config.language-menu.desc": "Show installed custom language packs in the built-in language selection menu.",
  "config.recipe-suffix.name": "Localized (Recipe) suffix",
  "config.recipe-suffix.desc": "Translates the (Recipe) suffix on item names. Only active for Ukrainian language packs.",
  "config.font-zoom.name": "Font zoom fix",
  "config.font-zoom.desc": "Keeps the language pack's font zoom instead of letting the game reset it.",
  "config.justify.name": "Justified dialogue text",
  "config.justify.desc": "Stretches spaces so dialogue text has an even right edge.",
  "config.bundles.name": "Junimo bundle names fix",
  "config.bundles.desc": "Localizes Junimo bundle names immediately, without the save-and-relog ritual."
}
```

- [ ] **Step 2: Створити i18n/uk.json**

```json
{
  "log.language-switched": "Мову перемкнуто на: {{id}}",
  "log.language-switch-failed": "Помилка перемикання мови: {{error}}",
  "log.language-picked": "Мову вибрано в меню: {{id}}",
  "log.bundles-refreshed": "Назви клунків перелокалізовано.",
  "log.bundles-rebuilt": "Назви клунків перебудовано після оновлення асета.",
  "log.bundles-failed": "Не вдалося оновити назви клунків: {{error}}",

  "config.clock.name": "Формат годинника на HUD",
  "config.clock.desc": "24h — 24-годинний годинник для кастомних мов; 12h — як у ванілі.",
  "config.language-menu.name": "Мод-мови в меню вибору мов",
  "config.language-menu.desc": "Показувати встановлені мовні паки у вбудованому меню вибору мови.",
  "config.recipe-suffix.name": "Локалізований суфікс (Рецепт)",
  "config.recipe-suffix.desc": "Перекладає суфікс (Recipe) у назвах предметів. Діє лише для українських паків.",
  "config.font-zoom.name": "Фікс зуму шрифта",
  "config.font-zoom.desc": "Зберігає зум шрифта мовного пака, не даючи грі його скинути.",
  "config.justify.name": "Рівний край тексту діалогів",
  "config.justify.desc": "Розтягує пробіли, щоб правий край тексту діалогів був рівним.",
  "config.bundles.name": "Фікс назв клунків Джунімо",
  "config.bundles.desc": "Локалізує назви клунків одразу, без ритуалу «збережись і перезайди»."
}
```

- [ ] **Step 3: Перевести логи на Translation**

- `ModEntry.SwitchTo`: `Log.Log(H.Translation.Get("log.language-switched", new { id = lang?.Id ?? "en" }), LogLevel.Info);` та в catch: `Log.Log(H.Translation.Get("log.language-switch-failed", new { error = ex.Message }), LogLevel.Error);`
- `LangMenuPatch.ClickPostfix`: `ModEntry.Log.Log(ModEntry.H.Translation.Get("log.language-picked", new { id = _langs[i].Id }), StardewModdingAPI.LogLevel.Info);`
- `Shared/BundlePatch.cs`: три `_log.Log(...)` → `_log.Log(_helper.Translation.Get("log.bundles-rebuilt"), LogLevel.Trace);`, `..."log.bundles-refreshed"...`, `_log.Log(_helper.Translation.Get("log.bundles-failed", new { error = ex.Message }), LogLevel.Warn);` (обидва catch використовують `log.bundles-failed`).

- [ ] **Step 4: Збірка**

Run: `cd src/CustomLanguageFixes && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: i18n — англійська за замовчуванням, українська локалізація

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: GMCM — внутрішньоігрове меню налаштувань (опційна залежність)

**Files:**
- Create: `src/Shared/IGenericModConfigMenuApi.cs`
- Modify: `src/CustomLanguageFixes/ModEntry.cs` (GameLaunched + реєстрація)

**Interfaces:**
- Consumes: i18n-ключі `config.*` з Task 4.
- Produces: `CustomLanguage.Shared.IGenericModConfigMenuApi` (використовує і Task 6).

- [ ] **Step 1: Створити src/Shared/IGenericModConfigMenuApi.cs**

```csharp
using System;
using StardewModdingAPI;

namespace CustomLanguage.Shared
{
    // Мінімальна копія публічного API GMCM (spacechase0.GenericModConfigMenu) —
    // стандартний спосіб інтеграції без компайл-залежності. На Android GMCM
    // портований NRTnarathip, інтерфейс той самий.
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue,
            Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue,
            Func<string> name, Func<string> tooltip = null, string[] allowedValues = null,
            Func<string, string> formatAllowedValue = null, string fieldId = null);
    }
}
```

- [ ] **Step 2: Реєстрація в ModEntry**

В `Entry` додати `helper.Events.GameLoop.GameLaunched += OnGameLaunched;`, метод:

```csharp
private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
{
    var gmcm = H.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (gmcm == null)
        return;
    gmcm.Register(this.ModManifest, () => Config = new ModConfig(), () => H.WriteConfig(Config));
    gmcm.AddTextOption(this.ModManifest, () => Config.Clock, v => Config.Clock = v,
        () => H.Translation.Get("config.clock.name"), () => H.Translation.Get("config.clock.desc"),
        new[] { "24h", "12h" });
    gmcm.AddBoolOption(this.ModManifest, () => Config.LanguageMenu, v => Config.LanguageMenu = v,
        () => H.Translation.Get("config.language-menu.name"), () => H.Translation.Get("config.language-menu.desc"));
    gmcm.AddBoolOption(this.ModManifest, () => Config.RecipeSuffix, v => Config.RecipeSuffix = v,
        () => H.Translation.Get("config.recipe-suffix.name"), () => H.Translation.Get("config.recipe-suffix.desc"));
    gmcm.AddBoolOption(this.ModManifest, () => Config.FontZoomFix, v => Config.FontZoomFix = v,
        () => H.Translation.Get("config.font-zoom.name"), () => H.Translation.Get("config.font-zoom.desc"));
    gmcm.AddBoolOption(this.ModManifest, () => Config.JustifyDialogue, v => Config.JustifyDialogue = v,
        () => H.Translation.Get("config.justify.name"), () => H.Translation.Get("config.justify.desc"));
    gmcm.AddBoolOption(this.ModManifest, () => Config.BundleNamesFix,
        v => { Config.BundleNamesFix = v; if (v && Context.IsWorldReady) BundlePatch.RefreshBundleNames(); },
        () => H.Translation.Get("config.bundles.name"), () => H.Translation.Get("config.bundles.desc"));
}
```

- [ ] **Step 3: Збірка**

Run: `cd src/CustomLanguageFixes && dotnet build -c Release`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: інтеграція з GMCM — внутрішньоігрові перемикачі всіх фіч

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: ПК-мод Custom Language Bundle Fix

**Files:**
- Create: `src/CustomLanguageBundleFix/CustomLanguageBundleFix.csproj`, `ModEntry.cs`, `manifest.json`, `i18n/default.json`, `i18n/uk.json`

**Interfaces:**
- Consumes: `CustomLanguage.Shared.BundlePatch.Apply(IModHelper, IMonitor, Func<bool>)`, `.RefreshBundleNames()`, `IGenericModConfigMenuApi`.

- [ ] **Step 1: csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AssemblyName>CustomLanguageBundleFix</AssemblyName>
    <RootNamespace>CustomLanguageBundleFix</RootNamespace>
    <Nullable>disable</Nullable>
    <EnableHarmony>true</EnableHarmony>
    <GamePath>D:\SteamLibrary\steamapps\common\Stardew Valley</GamePath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.3.2" />
    <Compile Include="..\Shared\**\*.cs" LinkBase="Shared" />
  </ItemGroup>
</Project>
```
(ModBuildConfig сам знаходить SMAPI/гру, деплоїть у Mods десктопної гри при збірці і робить zip у bin — наш реліз-zip усе одно пакуємо своїм скриптом, Task 7.)

- [ ] **Step 2: manifest.json**

```json
{
  "Name": "Custom Language Bundle Fix",
  "Author": "ThatHunky & DID",
  "Version": "1.0.0",
  "Description": "Junimo bundle names show in your custom (mod) language immediately - no more save-and-relog ritual.",
  "UniqueID": "ThatHunky.CustomLanguageBundleFix",
  "EntryDll": "CustomLanguageBundleFix.dll",
  "MinimumApiVersion": "4.0.0",
  "UpdateKeys": ["GitHub:ThatHunky/CustomLanguageFixes"]
}
```

- [ ] **Step 3: ModEntry.cs**

```csharp
using System;
using CustomLanguage.Shared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CustomLanguageBundleFix
{
    public class ModConfig
    {
        public bool BundleNamesFix { get; set; } = true;
    }

    public class ModEntry : Mod
    {
        private ModConfig _config;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            BundlePatch.Apply(helper, this.Monitor, () => _config.BundleNamesFix);
            // зміна мови посеред сесії (на ПК меню мов саме фаєрить OnLanguageChange)
            LocalizedContentManager.OnLanguageChange += _ =>
            {
                if (Context.IsWorldReady)
                    BundlePatch.RefreshBundleNames();
            };
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return;
            gmcm.Register(this.ModManifest, () => _config = new ModConfig(), () => this.Helper.WriteConfig(_config));
            gmcm.AddBoolOption(this.ModManifest, () => _config.BundleNamesFix,
                v => { _config.BundleNamesFix = v; if (v && Context.IsWorldReady) BundlePatch.RefreshBundleNames(); },
                () => this.Helper.Translation.Get("config.bundles.name"),
                () => this.Helper.Translation.Get("config.bundles.desc"));
        }
    }
}
```

- [ ] **Step 4: i18n (лише bundle-ключі)**

`i18n/default.json`:
```json
{
  "log.bundles-refreshed": "Bundle names relocalized.",
  "log.bundles-rebuilt": "Bundle names rebuilt after asset update.",
  "log.bundles-failed": "Failed to update bundle names: {{error}}",
  "config.bundles.name": "Junimo bundle names fix",
  "config.bundles.desc": "Localizes Junimo bundle names immediately, without the save-and-relog ritual."
}
```
`i18n/uk.json`:
```json
{
  "log.bundles-refreshed": "Назви клунків перелокалізовано.",
  "log.bundles-rebuilt": "Назви клунків перебудовано після оновлення асета.",
  "log.bundles-failed": "Не вдалося оновити назви клунків: {{error}}",
  "config.bundles.name": "Фікс назв клунків Джунімо",
  "config.bundles.desc": "Локалізує назви клунків одразу, без ритуалу «збережись і перезайди»."
}
```

- [ ] **Step 5: Збірка**

Run: `cd src/CustomLanguageBundleFix && dotnet build -c Release`
Expected: `Build succeeded`; у виводі ModBuildConfig — деплой у `D:\SteamLibrary\...\Stardew Valley\Mods\CustomLanguageBundleFix`. Якщо збірка впаде на пошуку гри — перевірити `<GamePath>`.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: ПК-мод Custom Language Bundle Fix 1.0.0 (спільний BundlePatch)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Скрипт пакування релізів + два zip

**Files:**
- Create: `tools/pack_releases.py`
- Create (артефакти): `releases/CustomLanguageFixes-2.0.0.zip`, `releases/CustomLanguageBundleFix-1.0.0.zip`

- [ ] **Step 1: tools/pack_releases.py**

```python
"""Пакує реліз-zip обох модів. ТІЛЬКИ python zipfile: Compress-Archive пише
backslash-шляхи, які Android розпаковує у битi файли "Dir\\file"."""
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def pack(zip_name: str, folder: str, entries: list[tuple[str, str]]) -> None:
    out = ROOT / "releases" / zip_name
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for disk, arc in entries:
            z.write(ROOT / disk, f"{folder}/{arc}")
    print("packed:", out.name)
    with zipfile.ZipFile(out) as z:
        assert all("\\" not in n for n in z.namelist()), "backslash у шляхах!"
        print("  ", "\n   ".join(z.namelist()))

pack("CustomLanguageFixes-2.0.0.zip", "CustomLanguageFixes", [
    ("src/CustomLanguageFixes/bin/Release/net9.0/CustomLanguageFixes.dll", "CustomLanguageFixes.dll"),
    ("src/CustomLanguageFixes/manifest.json", "manifest.json"),
    ("src/CustomLanguageFixes/i18n/default.json", "i18n/default.json"),
    ("src/CustomLanguageFixes/i18n/uk.json", "i18n/uk.json"),
])
pack("CustomLanguageBundleFix-1.0.0.zip", "CustomLanguageBundleFix", [
    ("src/CustomLanguageBundleFix/bin/Release/net6.0/CustomLanguageBundleFix.dll", "CustomLanguageBundleFix.dll"),
    ("src/CustomLanguageBundleFix/manifest.json", "manifest.json"),
    ("src/CustomLanguageBundleFix/i18n/default.json", "i18n/default.json"),
    ("src/CustomLanguageBundleFix/i18n/uk.json", "i18n/uk.json"),
])
```

- [ ] **Step 2: Запуск і перевірка**

Run: `python -X utf8 tools/pack_releases.py`
Expected: `packed: CustomLanguageFixes-2.0.0.zip` і `packed: CustomLanguageBundleFix-1.0.0.zip`, у namelist кожен шлях з `/`, по 4 записи.

- [ ] **Step 3: Commit**

```bash
git add tools/pack_releases.py releases/CustomLanguageFixes-2.0.0.zip releases/CustomLanguageBundleFix-1.0.0.zip
git commit -m "build: скрипт пакування + релізи 2.0.0 (Android) і 1.0.0 (ПК)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: README — англійська основна + українська секція

**Files:**
- Modify: `README.md` (повний перепис)

- [ ] **Step 1: Переписати README.md**

Обов'язковий зміст (структура; формулювання адаптувати з поточного README, він уже містить усі технічні описи українською — перекласти):

1. `# Custom Language Fixes` — вступ: two SMAPI mods that fix what the game breaks for custom (mod) languages; status "in development, not yet on Nexus"; згадка «Солов'їна Долина» як попередньої назви й української назви проєкту.
2. `## Custom Language Fixes (Android)` — таблиця 7 фіч (переклад поточної таблиці: 24h clock, language menu, language memory, (Recipe) suffix for Ukrainian, font zoom, justified dialogue, Junimo bundle names) + примітка "all patches only run when a custom language is active".
3. `## Custom Language Bundle Fix (PC)` — one feature: bundle names; why PC needs only this.
4. `## Installation` — Android (SMAPI Launcher + zip у Mods) і PC (SMAPI + zip у Mods); note about GMCM optional.
5. `## Configuration` — таблиця полів config.json обох модів (з Task 3/6) + «або через GMCM в грі».
6. `## Building` — як зараз (Android DLL з APK у libs/, dotnet build; PC збирається сам через ModBuildConfig з GamePath).
7. `## Repository structure`, `## Version history` (2.0.0 — universal rename + config + GMCM + i18n + PC mod; зберегти стару історію 1.0–1.5.2 як є), `## Support` (монобанка + картка, як зараз), `## License` (MIT), `## Credits` (Pereclaw, DID, NRTnarathip + GMCM spacechase0).
8. `## Українською` — стислий переклад вступу, встановлення і конфігурації (решта — посилання на англійські секції).

- [ ] **Step 2: Commit**

```bash
git add README.md && git commit -m "docs: README англійською з українською секцією (Custom Language Fixes)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: Перейменувати GitHub-репо, запушити, оновити пам'ять

- [ ] **Step 1: Перейменувати репо і remote**

```bash
gh repo rename CustomLanguageFixes -R ThatHunky/SolovyinaDolyna --yes
git remote set-url origin https://github.com/ThatHunky/CustomLanguageFixes.git
```
Expected: `✓ Renamed repository ThatHunky/CustomLanguageFixes` (старі URL редіректяться).

- [ ] **Step 2: Push**

Run: `git push`
Expected: без 403 (акаунт ThatHunky через gh credential helper).

- [ ] **Step 3: Оновити пам'ять**

У `C:\Users\sysadmin\.claude\projects\C--Users-sysadmin-Documents-SV-StardewUkr-\memory\`: оновити `solovyina-dolyna-mod.md` (нові назви/UniqueID/версії, новий URL репо, факт двох модів і ПК-версії) та рядок у `MEMORY.md`.

- [ ] **Step 4: Надіслати zip користувачеві**

SendUserFile: обидва zip з `releases/` + нагадування DID: видалити стару теку `SolovyinaDolyna` з Mods, поставити GMCM-zip з релізів NRTnarathip (опційно), вибрати мову заново один раз.

---

## Мануальне тестування (після всіх тасок)

**Android (DID):** мова застосувалась автоматично; меню мов показує мод-мови; годинник 24h (і 12h після перемикання в GMCM); діалоги justify; клунки українською одразу на «старому» сейві та після ночі на «свіжому»; GMCM показує всі 7 опцій; лог без ERROR/WARN.
**ПК (локально):** гра з паком Pereclaw + наш мод; сейв із зустрінутими клунками → назви українською одразу після завантаження; лог: `Bundle names relocalized.`; прогрес клунків неушкоджений.
