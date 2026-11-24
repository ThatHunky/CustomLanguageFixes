using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguage.Shared
{
    // Логіка DID.Grammar_fix (Ukrainian Grammar Fix 1.2.0): гра підставляє назви будівель,
    // тварин та інструментів у середину речення з великої літери, а в укр. перекладі вони
    // мають іти з малої; кілька рядків-підказок у Марні мають зайві великі літери, а одному
    // бракує крапки в кінці. Спільний для Android і ПК (усі перевантаження LoadString і
    // Game1.drawObjectDialogue(string) звірено в обох збірках). Гейт саме на uk — інші
    // мод-мови не чіпаємо.
    internal static class GrammarPatch
    {
        // Рядки-підказки у Марні, у яких усі літери після першої мають бути малими
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
            // 3. Підказки у Марні — рядки без підстановок
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

        // Падаємо тихо (гра покаже ванільний рядок), але один раз лишаємо слід у лозі
        private static void LogError(string where, Exception ex) =>
            _log?.LogOnce(_i18n.Get("log.grammar-error", new { method = where, error = ex.Message }), LogLevel.Warn);

        // Чи стоїть {index} на початку речення у форматі пака: тоді назву лишаємо з великої.
        // Формат беремо з того самого менеджера; LoadString(string) не зачіпає ці ключі.
        private static bool StartsSentence(LocalizedContentManager content, string path, int index)
        {
            string format = content.LoadString(path) ?? string.Empty;
            int pos = format.IndexOf("{" + index + "}", StringComparison.Ordinal);
            if (pos < 0)
                return true; // підстановки в рядку нема — нема що виправляти
            string before = format.Substring(0, pos).TrimEnd().TrimEnd('«', '"', '„', '“', '(').TrimEnd();
            return before.Length == 0 || ".!?…^".IndexOf(before[before.Length - 1]) >= 0;
        }

        private static object LowerIfMidSentence(LocalizedContentManager content, string path, int index, object sub) =>
            sub is string name && name.Length > 0 && !StartsSentence(content, path, index) ? ToLowerFirst(name) : sub;

        // Перша літера велика, решта мала — крім [токенів] і {підстановок}: ключі в них
        // регістрозалежні, і «[localizedtext ...]» гра вже не розв'яже
        private static string SentenceCase(string value)
        {
            var sb = new System.Text.StringBuilder(value.Length);
            int square = 0, curly = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '[') square++;
                else if (c == ']' && square > 0) square--;
                else if (c == '{') curly++;
                else if (c == '}' && curly > 0) curly--;
                if (square > 0 || curly > 0 || c == ']' || c == '}')
                    sb.Append(c);
                else
                    sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        private static void LoadString1Prefix(LocalizedContentManager __instance, string path, ref object sub1)
        {
            try
            {
                if (!IsActive())
                    return;
                string key = Normalize(path);
                if (key != UpgradeKey && key != ToolReadyKey)
                    return;
                sub1 = LowerIfMidSentence(__instance, path, 0, sub1);
            }
            catch (Exception ex) { LogError(nameof(LoadString1Prefix), ex); }
        }

        private static void LoadString2Prefix(LocalizedContentManager __instance, string path, ref object sub1, ref object sub2)
        {
            try
            {
                if (!IsActive() || Normalize(path) != AnimalBuildingKey)
                    return;
                sub1 = LowerIfMidSentence(__instance, path, 0, sub1);
                sub2 = LowerIfMidSentence(__instance, path, 1, sub2);
            }
            catch (Exception ex) { LogError(nameof(LoadString2Prefix), ex); }
        }

        private static void LoadStringPostfix(string path, ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result) || !IsActive() || !SentenceCaseKeys.Contains(Normalize(path)))
                    return;
                __result = SentenceCase(__result);
            }
            catch (Exception ex) { LogError(nameof(LoadStringPostfix), ex); }
        }

        // Той самий рядок показують ДжоджаМарт (кінотеатр), автомат-кран у кінотеатрі і (на ПК)
        // меню Марні — порівняння з рядком і так обмежує патч одним повідомленням, тож без гейту на локацію
        private static void DrawObjectDialoguePrefix(ref string dialogue)
        {
            try
            {
                if (string.IsNullOrEmpty(dialogue) || !IsActive())
                    return;
                if (dialogue == Game1.content.LoadString(NotEnoughMoneyKey) && !dialogue.EndsWith(".", StringComparison.Ordinal))
                    dialogue += ".";
            }
            catch (Exception ex) { LogError(nameof(DrawObjectDialoguePrefix), ex); }
        }
    }
}
