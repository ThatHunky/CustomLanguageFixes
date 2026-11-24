using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguage.Shared
{
    // Логіка DID.Grammar_fix (Ukrainian Grammar Fix 1.2.0): гра підставляє назви будівель,
    // тварин та інструментів у середину речення з великої літери, а в укр. перекладі вони
    // мають іти з малої; кілька рядків магазину Марні мають зайві великі літери, а одному
    // бракує крапки в кінці. Спільний для Android і ПК (усі перевантаження LoadString і
    // Game1.drawObjectDialogue(string) звірено в обох збірках). Гейт саме на uk — інші
    // мод-мови не чіпаємо.
    internal static class GrammarPatch
    {
        // Рядки, у яких усі літери після першої мають бути малими (назви тварин у Марні)
        private static readonly HashSet<string> SentenceCaseKeys = new()
        {
            "Strings\\StringsFromCSFiles:Utility.cs.5926",
            "Strings\\StringsFromCSFiles:Utility.cs.5931",
            "Strings\\StringsFromCSFiles:Utility.cs.5936",
            "Strings\\StringsFromCSFiles:Utility.cs.5940",
            "Strings\\StringsFromCSFiles:Utility.cs.5944",
            "Strings\\StringsFromCSFiles:Utility.cs.5947",
            "Strings\\StringsFromCSFiles:Utility.cs.5950"
        };

        private const string UpgradeKey = "Strings\\UI:Carpenter_SelectBuilding_Upgrade";          // розширення будівлі у Робін
        private const string ToolReadyKey = "Strings\\StringsFromCSFiles:ToolReady";               // інструмент готовий у Клінта
        private const string AnimalBuildingKey = "Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11355"; // будівля під тварину
        private const string NotEnoughMoneyKey = "Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11325"; // «бракує грошей»

        private static Func<bool> _enabled;
        private static IMonitor _log;
        private static ITranslationHelper _i18n;

        public static void Apply(Harmony harmony, IMonitor log, ITranslationHelper i18n, Func<bool> enabled)
        {
            _enabled = enabled;
            _log = log;
            _i18n = i18n;

            // 1. Розширення будівлі у Робін і готовий інструмент у Клінта (1 аргумент)
            TryPatch(harmony, typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString),
                new[] { typeof(string), typeof(object) }, prefix: nameof(LoadString1Prefix));
            // 2. Вибір будівлі під тварину у Марні (2 аргументи)
            TryPatch(harmony, typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString),
                new[] { typeof(string), typeof(object), typeof(object) }, prefix: nameof(LoadString2Prefix));
            // 3. Назви тварин у Марні — рядки без підстановок
            TryPatch(harmony, typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString),
                new[] { typeof(string) }, postfix: nameof(LoadStringPostfix));
            // 4. Крапка в кінці PurchaseAnimalsMenu.cs.11325 у діалозі ДжоджаМарту
            TryPatch(harmony, typeof(Game1), nameof(Game1.drawObjectDialogue),
                new[] { typeof(string) }, prefix: nameof(DrawObjectDialoguePrefix));
        }

        private static void TryPatch(Harmony harmony, Type type, string method, Type[] args, string prefix = null, string postfix = null)
        {
            string signature = $"{type.Name}.{method}({string.Join(", ", Array.ConvertAll(args, t => t.Name))})";
            try
            {
                var original = AccessTools.Method(type, method, args);
                if (original == null)
                {
                    _log?.Log(_i18n.Get("log.grammar-method-missing", new { method = signature }), LogLevel.Warn);
                    return;
                }
                harmony.Patch(
                    original: original,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(GrammarPatch), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(GrammarPatch), postfix)
                );
            }
            catch (Exception ex)
            {
                _log?.Log(_i18n.Get("log.grammar-patch-failed", new { method = signature, error = ex.Message }), LogLevel.Error);
            }
        }

        // Спершу дешевий гейт на мову — LoadString викликається постійно
        private static bool IsActive()
        {
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                return false;
            if (!string.Equals(LocalizedContentManager.CurrentModLanguage?.LanguageCode, "uk", StringComparison.OrdinalIgnoreCase))
                return false;
            return _enabled == null || _enabled();
        }

        // Гра зазвичай передає "\\", але інші моди можуть передавати "/"
        private static string Normalize(string path) => path?.Replace('/', '\\') ?? string.Empty;

        private static string ToLowerFirst(string value) =>
            value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value.Substring(1);

        private static void LoadString1Prefix(string path, ref object sub1)
        {
            try
            {
                if (!IsActive())
                    return;
                string key = Normalize(path);
                if (key != UpgradeKey && key != ToolReadyKey)
                    return;
                if (sub1 is string name && name.Length > 0)
                    sub1 = ToLowerFirst(name);
            }
            catch { } // падаємо тихо — гра покаже ванільний рядок
        }

        private static void LoadString2Prefix(string path, ref object sub1, ref object sub2)
        {
            try
            {
                if (!IsActive() || Normalize(path) != AnimalBuildingKey)
                    return;
                if (sub1 is string building && building.Length > 0)
                    sub1 = ToLowerFirst(building);
                if (sub2 is string animal && animal.Length > 0)
                    sub2 = ToLowerFirst(animal);
            }
            catch { }
        }

        private static void LoadStringPostfix(string path, ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result) || !IsActive() || !SentenceCaseKeys.Contains(Normalize(path)))
                    return;
                __result = char.ToUpperInvariant(__result[0]) + __result.Substring(1).ToLowerInvariant();
            }
            catch { }
        }

        private static void DrawObjectDialoguePrefix(ref string dialogue)
        {
            try
            {
                if (string.IsNullOrEmpty(dialogue) || !IsActive() || Game1.currentLocation?.Name != "JojaMart")
                    return;
                if (dialogue == Game1.content.LoadString(NotEnoughMoneyKey) && !dialogue.EndsWith("."))
                    dialogue += ".";
            }
            catch { }
        }
    }
}
