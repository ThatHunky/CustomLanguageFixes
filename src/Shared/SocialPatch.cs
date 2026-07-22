using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace CustomLanguage.Shared
{
    // SocialPage.drawFarmerSlot (слот гравця/фермера) для статусу «самотній» читає ЛИШЕ ключ
    // SocialPage_Relationship_Single_Female — або дослівно, або .Split('/') для мов із гендерними
    // перекладами — і НІКОЛИ не бере ..._Single_Male. Тож мод-мова, де _Female = «(незаміжня)»
    // без слеша, показує «(незаміжня)» навіть на чоловічому фермері (особливо помітно на власному
    // персонажі). Ванільний баг, ІДЕНТИЧНИЙ на мобільному й десктопі (звірено декомпілом обох),
    // тому патч спільний. NPC малює drawNPCSlot, де стать береться правильно — там баг не проявляється.
    //
    // Фікс: поки малюється слот фермера, підміняємо LoadString(..._Single_Female) на правильний
    // за статтю рядок ПАКА — _Male для чоловіка, _Female для жінки. Рядок без слеша, тож
    // .Split('/').First()/.Last() у гендерній гілці повертає його ж — працює за будь-якого
    // ShouldUseGenderedCharacterTranslations() і для будь-якої мод-мови (не лише uk).
    internal static class SocialPatch
    {
        private const string SingleFemaleTail = "SocialPage_Relationship_Single_Female";
        private const string SingleFemaleKey = "Strings\\StringsFromCSFiles:SocialPage_Relationship_Single_Female";
        private const string SingleMaleKey = "Strings\\StringsFromCSFiles:SocialPage_Relationship_Single_Male";

        private static Func<bool> _enabled;
        private static bool _active;    // активне лише всередині нашого drawFarmerSlot
        private static string _single;  // правильний за статтю рядок для поточного фермера

        public static void Apply(Harmony harmony, Func<bool> enabled)
        {
            _enabled = enabled;
            harmony.Patch(
                original: AccessTools.Method(typeof(SocialPage), nameof(SocialPage.drawFarmerSlot),
                    new[] { typeof(SpriteBatch), typeof(int) }),
                prefix: new HarmonyMethod(typeof(SocialPatch), nameof(FarmerSlotPrefix)),
                finalizer: new HarmonyMethod(typeof(SocialPatch), nameof(FarmerSlotFinalizer))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(LocalizedContentManager), nameof(LocalizedContentManager.LoadString),
                    new[] { typeof(string) }),
                prefix: new HarmonyMethod(typeof(SocialPatch), nameof(LoadStringPrefix))
            );
        }

        private static void FarmerSlotPrefix(SocialPage __instance, int i, ref bool __state)
        {
            __state = false;
            if (_enabled != null && !_enabled())
                return;
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                return;
            try
            {
                var entry = __instance.GetSocialEntry(i);
                if (entry == null || !entry.IsPlayer)
                    return;
                // кешуємо потрібний рядок ПОКИ _active == false — інакше LoadString нижче перехопив би сам себе
                _single = Game1.content.LoadString(entry.Gender == Gender.Male ? SingleMaleKey : SingleFemaleKey);
                _active = true;
                __state = true;
            }
            catch { }
        }

        private static Exception FarmerSlotFinalizer(bool __state, Exception __exception)
        {
            if (__state)
                _active = false;
            return __exception;
        }

        private static bool LoadStringPrefix(string path, ref string __result)
        {
            if (!_active || path == null || _single == null)
                return true;
            if (path.EndsWith(SingleFemaleTail, StringComparison.Ordinal))
            {
                __result = _single;
                return false; // ванільний LoadString не викликаємо
            }
            return true;
        }
    }
}
