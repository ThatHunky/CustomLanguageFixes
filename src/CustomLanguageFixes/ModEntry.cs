using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomLanguage.Shared;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;

namespace CustomLanguageFixes
{
    public class ModConfig
    {
        // "" = авто (перша мод-мова), "en" = англійська, або Id мод-мови (напр. "Pereclaw.ukrainizacija")
        public string PreferredLanguage { get; set; } = "";
        public string Clock { get; set; } = "24h";      // "24h" | "12h" (обидва з дп/пп для мод-мов)
        public bool FontZoomFix { get; set; } = true;
        public bool JustifyDialogue { get; set; } = true;

        // Не виводяться в GMCM — лише аварійні вимикачі в config.json.
        public bool RecipeSuffix { get; set; } = true;   // (Рецепт)/(Креслення), діє лише для uk
        // Зміна на льоту не дає повного ефекту: меню мов перечитує це при наступному
        // відкритті, а вже локалізовані назви клунків повернуться англійськими
        // тільки після перезавантаження сейва.
        public bool LanguageMenu { get; set; } = true;
        public bool BundleNamesFix { get; set; } = true;
        public bool SocialSingleFix { get; set; } = true; // «холостий»/«незаміжня» на слоті фермера за статтю
    }

    public class ModEntry : Mod
    {
        internal static IMonitor Log;
        internal static ModConfig Config;
        internal static IModHelper H;

        public override void Entry(IModHelper helper)
        {
            Log = this.Monitor;
            H = helper;
            Config = helper.ReadConfig<ModConfig>();

            var harmony = new Harmony("ThatHunky.CustomLanguageFixes");
            ClockPatch.Apply(harmony);   // 24h HUD-годинник для мод-мов
            RecipePatch.Apply(harmony, () => Config.RecipeSuffix);  // (Рецепт)/(Креслення) — тільки для укр. мод-мови
            LangMenuPatch.Apply(harmony); // мод-мови у вбудованому меню вибору мов
            FontPatch.Apply(harmony);     // зум шрифта пака не затирається shrinkFont'ом
            JustifyPatch.Apply(harmony);  // рівний правий край тексту діалогів (justify)
            SocialPatch.Apply(harmony, () => Config.SocialSingleFix);   // стать у статусі «самотній» на слоті фермера (Social page)
            BundlePatch.Apply(helper, this.Monitor, () => Config.BundleNamesFix); // локалізовані назви клунків одразу

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;     // меню налаштувань (GMCM), якщо є
            helper.Events.Content.AssetReady += OnAssetReady;          // застосувати мову при старті
            LocalizedContentManager.OnLanguageChange += OnLanguageChanged; // запам'ятати вибір з вбудованого меню
        }

        private static void OnLanguageChanged(LocalizedContentManager.LanguageCode code)
        {
            // юзер вибрав щось у вбудованому меню (в т.ч. English) — поважаємо і запам'ятовуємо
            string chosen = code == LocalizedContentManager.LanguageCode.mod
                ? LocalizedContentManager.CurrentModLanguage?.Id ?? ""
                : code.ToString();
            if (Config.PreferredLanguage != chosen)
            {
                Config.PreferredLanguage = chosen;
                H.WriteConfig(Config);
            }
            if (Context.IsWorldReady)
                BundlePatch.RefreshBundleNames(); // мову змінили посеред сесії — перелокалізувати клунки
        }

        // ---------- меню налаштувань ----------

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var gmcm = H.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return; // GMCM не встановлено — лишається config.json

            // Default у GMCM скидає перемикачі, але НЕ пам'ять вибору мови — цієї опції в меню не видно
            gmcm.Register(this.ModManifest,
                () => Config = new ModConfig { PreferredLanguage = Config.PreferredLanguage },
                () => H.WriteConfig(Config));
            gmcm.AddTextOption(this.ModManifest, () => Config.Clock, v => Config.Clock = v,
                () => H.Translation.Get("config.clock.name"), () => H.Translation.Get("config.clock.desc"),
                new[] { "24h", "12h" },
                v => H.Translation.Get("config.clock.value-" + v)); // у списку показуємо мовою гри
            gmcm.AddBoolOption(this.ModManifest, () => Config.FontZoomFix, v => Config.FontZoomFix = v,
                () => H.Translation.Get("config.font-zoom.name"), () => H.Translation.Get("config.font-zoom.desc"));
            gmcm.AddBoolOption(this.ModManifest, () => Config.JustifyDialogue, v => Config.JustifyDialogue = v,
                () => H.Translation.Get("config.justify.name"), () => H.Translation.Get("config.justify.desc"));
            // LanguageMenu, BundleNamesFix і RecipeSuffix у меню не виводимо: перші дві вимикати
            // нема сенсу (і зміна на льоту не дає повного ефекту — див. коментарі в ModConfig),
            // а суфікс «(Рецепт)» — частина українського перекладу, він має просто працювати.
            // Усі три лишаються в config.json як аварійний вимикач при конфлікті з іншим модом.
        }

        // ---------- мовна логіка ----------

        private static List<ModLanguage> GetModLanguages()
        {
            try { return Game1.content.Load<List<ModLanguage>>("Data\\AdditionalLanguages") ?? new(); }
            catch { return new(); }
        }

        private void OnAssetReady(object sender, AssetReadyEventArgs e)
        {
            if (!e.Name.IsEquivalentTo("Data/AdditionalLanguages"))
                return;
            var langs = GetModLanguages();
            if (langs.Count == 0)
            {
                if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod)
                    SwitchTo(null); // відкат на en, щоб не зависнути на битій мові
                return;
            }

            // застосувати збережений вибір користувача
            if (!string.IsNullOrEmpty(Config.PreferredLanguage) && !langs.Any(l => l.Id == Config.PreferredLanguage))
                return; // юзер свідомо вибрав вбудовану мову (en/de/...) — не чіпаємо
            var target = string.IsNullOrEmpty(Config.PreferredLanguage)
                ? langs[0]
                : langs.FirstOrDefault(l => l.Id == Config.PreferredLanguage) ?? langs[0];
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod
                || LocalizedContentManager.CurrentModLanguage?.Id != target.Id)
                SwitchTo(target);
        }

        // Спільна логіка перемикання мови — і для авто-застосування на старті, і для тапу в
        // меню мов (LangMenuPatch). PreferredLanguage виставляємо ДО зміни мови, щоб OnLanguageChanged
        // побачив його вже актуальним і не переписував config удруге; запис робимо тут один раз.
        internal static void SwitchTo(ModLanguage lang, string logKey = "log.language-switched")
        {
            try
            {
                Config.PreferredLanguage = lang?.Id ?? "en";
                if (lang == null)
                {
                    LocalizedContentManager.CurrentLanguageCode = LocalizedContentManager.LanguageCode.en; // property => OnLanguageChange => перезавантаження рядків
                }
                else
                {
                    // mod→mod (напр. білоруська→українська) не змінює код мови, тому
                    // OnLanguageChange не спрацює і гра сама не перезавантажить рядки/резолвер
                    bool silentSwitch = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod
                        && LocalizedContentManager.CurrentModLanguage?.Id != lang.Id;
                    LocalizedContentManager.SetModLanguage(lang);
                    if (silentSwitch)
                        Game1.game1?.TranslateFields(); // робить localizedAssetNames.Clear() + перевантажує кешовані рядки/шрифти
                }

                var prefs = new StartupPreferences();
                prefs.loadPreferences(false, true);
                prefs.savePreferences(false, true);

                H.WriteConfig(Config);
                Log.Log(H.Translation.Get(logKey, new { id = lang?.Id ?? "en" }), LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log.Log(H.Translation.Get("log.language-switch-failed", new { error = ex.Message }), LogLevel.Error);
            }
        }

    }

    // --- Годинник ---
    // Мобільний DayTimeMoneyBox.draw() — форкнутий код, у чиєму switch по мовах нема гілки mod.
    // Через це кастомна мова малює 12-годинний час БЕЗ дп/пп: суфікс дописується лише для
    // en/it/ja/zh, тож «9:00» неможливо відрізнити від «21:00».
    // 24h: підміняємо mod -> de (німецька гілка вже 24-годинна, суфікс не потрібен).
    // 12h: підміняємо mod -> en (англійська гілка дописує суфікс), але тоді й LoadString
    //      резолвив би АНГЛІЙСЬКИЙ асет — тому на час малювання підставляємо дп/пп мовного пака.
    internal static class ClockPatch
    {
        private static readonly FieldInfo LangField = AccessTools.Field(
            typeof(LocalizedContentManager), "_currentLangCode");

        private const string AmKey = "DayTimeMoneyBox.cs.10370";
        private const string PmKey = "DayTimeMoneyBox.cs.10371";

        private static bool _substituteAmPm;   // активне лише всередині нашого draw
        private static string _am, _pm, _cachedFor;

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw),
                    new[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(ClockPatch), nameof(DrawPrefix)),
                finalizer: new HarmonyMethod(typeof(ClockPatch), nameof(DrawFinalizer))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString),
                    new[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(ClockPatch), nameof(LoadStringPrefix))
            );
        }

        private static void DrawPrefix(ref bool __state)
        {
            __state = false;
            var current = (LocalizedContentManager.LanguageCode)LangField.GetValue(null);
            if (current != LocalizedContentManager.LanguageCode.mod)
                return;

            if (string.Equals(ModEntry.Config.Clock, "12h", StringComparison.OrdinalIgnoreCase))
            {
                CacheAmPm(); // поки мова ще mod — інакше дістанемо англійські рядки
                _substituteAmPm = true;
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.en);
            }
            else
            {
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.de);
            }
            __state = true;
        }

        private static Exception DrawFinalizer(bool __state, Exception __exception)
        {
            if (__state)
            {
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.mod);
                _substituteAmPm = false;
            }
            return __exception;
        }

        private static void CacheAmPm()
        {
            string id = LocalizedContentManager.CurrentModLanguage?.Id ?? "";
            if (_cachedFor == id)
                return;
            try
            {
                _am = Game1.content.LoadString("Strings\\StringsFromCSFiles:" + AmKey);
                _pm = Game1.content.LoadString("Strings\\StringsFromCSFiles:" + PmKey);
                _cachedFor = id;
            }
            catch { _am = _pm = null; }
        }

        private static bool LoadStringPrefix(string path, ref string __result)
        {
            if (!_substituteAmPm || path == null)
                return true;
            if (path.EndsWith(AmKey, StringComparison.Ordinal) && _am != null)
            {
                __result = _am;
                return false;
            }
            if (path.EndsWith(PmKey, StringComparison.Ordinal) && _pm != null)
            {
                __result = _pm;
                return false;
            }
            return true;
        }
    }
}
