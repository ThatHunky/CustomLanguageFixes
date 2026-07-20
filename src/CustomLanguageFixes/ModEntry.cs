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
using SObject = StardewValley.Object;

namespace CustomLanguageFixes
{
    public class ModConfig
    {
        // "" = авто (перша мод-мова), "en" = англійська, або Id мод-мови (напр. "Pereclaw.ukrainizacija")
        public string PreferredLanguage { get; set; } = "";
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
            RecipePatch.Apply(harmony);  // (Рецепт)/(Креслення) — тільки для укр. мод-мови
            LangMenuPatch.Apply(harmony); // мод-мови у вбудованому меню вибору мов
            FontPatch.Apply(harmony);     // зум шрифта пака не затирається shrinkFont'ом
            JustifyPatch.Apply(harmony);  // рівний правий край тексту діалогів (justify)
            BundlePatch.Apply(helper, this.Monitor, () => true); // локалізовані назви клунків одразу

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

        private static void SwitchTo(ModLanguage lang)
        {
            try
            {
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

                Config.PreferredLanguage = lang?.Id ?? "en";
                H.WriteConfig(Config);
                Log.Log($"Мову перемкнуто на: {(lang?.Id ?? "en")}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Log.Log("Помилка перемикання мови: " + ex.Message, LogLevel.Error);
            }
        }

        private static void CycleLanguage()
        {
            var langs = GetModLanguages();
            if (langs.Count == 0)
                return;
            // цикл: en -> langs[0] -> langs[1] -> ... -> en
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
            {
                SwitchTo(langs[0]);
                return;
            }
            int i = langs.FindIndex(l => l.Id == LocalizedContentManager.CurrentModLanguage?.Id);
            if (i >= 0 && i < langs.Count - 1)
                SwitchTo(langs[i + 1]);
            else
                SwitchTo(null); // en
        }

        private static string CurrentLabel()
        {
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod)
                return (LocalizedContentManager.CurrentModLanguage?.LanguageCode ?? "mod").ToUpperInvariant();
            return LocalizedContentManager.CurrentLanguageCode.ToString().ToUpperInvariant();
        }

    }

    // --- Годинник: на час draw() підміняємо mod -> de, гра сама малює 24h ---
    internal static class ClockPatch
    {
        private static readonly FieldInfo LangField = AccessTools.Field(
            typeof(LocalizedContentManager), "_currentLangCode");

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw),
                    new[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(ClockPatch), nameof(DrawPrefix)),
                finalizer: new HarmonyMethod(typeof(ClockPatch), nameof(DrawFinalizer))
            );
        }

        private static void DrawPrefix(ref bool __state)
        {
            __state = false;
            var current = (LocalizedContentManager.LanguageCode)LangField.GetValue(null);
            if (current == LocalizedContentManager.LanguageCode.mod)
            {
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.de);
                __state = true;
            }
        }

        private static Exception DrawFinalizer(bool __state, Exception __exception)
        {
            if (__state)
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.mod);
            return __exception;
        }
    }

    // --- Рецепти: логіка DID.RecipeUkrainizacija, тільки для укр. мод-мови ---
    internal static class RecipePatch
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(SObject), nameof(SObject.DisplayName)),
                postfix: new HarmonyMethod(typeof(RecipePatch), nameof(Postfix))
            );
        }

        public static void Postfix(SObject __instance, ref string __result)
        {
            try
            {
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                // суфікси українські — не чіпаємо інші мод-мови (тайську і т.д.)
                if (!string.Equals(LocalizedContentManager.CurrentModLanguage?.LanguageCode, "uk", StringComparison.OrdinalIgnoreCase))
                    return;
                if (__instance.isRecipe == null || !__instance.isRecipe.Value)
                    return;

                string suffix = Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12657");
                if (__result.EndsWith(suffix))
                {
                    string ua = (__instance.Category == -7) ? " (\u0420\u0435\u0446\u0435\u043f\u0442)" : " (\u041a\u0440\u0435\u0441\u043b\u0435\u043d\u043d\u044f)";
                    __result = __result.Substring(0, __result.Length - suffix.Length) + ua;
                }
            }
            catch { }
        }
    }
}
