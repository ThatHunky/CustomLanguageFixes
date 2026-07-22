using System;
using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace CustomLanguage.Shared
{
    // Логіка DID.RecipeUkrainizacija: для укр. мод-мови суфікс «(Recipe)» на назві предмета
    // замінюємо на «(Рецепт)» (їжа, Category -7) або «(Креслення)» (решта — креслення крафту).
    // База гри має один ключ на обидва випадки, тож розрізнити може лише мод. Спільний для
    // Android і ПК; гейт саме на uk — інші мод-мови (тайську тощо) не чіпаємо.
    internal static class RecipePatch
    {
        private static Func<bool> _enabled;

        public static void Apply(Harmony harmony, Func<bool> enabled)
        {
            _enabled = enabled;
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(SObject), nameof(SObject.DisplayName)),
                postfix: new HarmonyMethod(typeof(RecipePatch), nameof(Postfix))
            );
        }

        public static void Postfix(SObject __instance, ref string __result)
        {
            try
            {
                if (_enabled != null && !_enabled())
                    return;
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                // суфікси українські — не чіпаємо інші мод-мови (тайську і т.д.)
                if (!string.Equals(LocalizedContentManager.CurrentModLanguage?.LanguageCode, "uk", StringComparison.OrdinalIgnoreCase))
                    return;
                if (__instance.isRecipe == null || !__instance.isRecipe.Value)
                    return;

                string suffix = Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12657");
                if (!string.IsNullOrEmpty(suffix) && __result.EndsWith(suffix))
                {
                    string ua = (__instance.Category == -7) ? " (Рецепт)" : " (Креслення)";
                    __result = __result.Substring(0, __result.Length - suffix.Length) + ua;
                }
            }
            catch { }
        }
    }
}
