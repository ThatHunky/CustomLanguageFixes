using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Mobile24hClockFix
{
    public class ModEntry : Mod
    {
        // пряме поле, а не property-setter: не тригерить OnLanguageChange / інвалідацію контенту
        private static readonly FieldInfo LangField = AccessTools.Field(
            typeof(LocalizedContentManager), "_currentLangCode");

        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony("ThatHunky.Mobile24hClockFix");
            harmony.Patch(
                original: AccessTools.Method(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw),
                    new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(DrawPrefix)),
                finalizer: new HarmonyMethod(typeof(ModEntry), nameof(DrawFinalizer))
            );
        }

        // __state запам'ятовує, чи підміняли — щоб finalizer коректно відкотив
        private static void DrawPrefix(ref bool __state)
        {
            __state = false;
            var current = (LocalizedContentManager.LanguageCode)LangField.GetValue(null);
            if (current == LocalizedContentManager.LanguageCode.mod)
            {
                // безпечно: назви днів беруться з кешу _shortDayDisplayName,
                // а de/24h-гілка draw() не робить жодного LoadString
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.de);
                __state = true;
            }
        }

        // finalizer, а не postfix => мова відкотиться навіть якщо draw кине exception
        private static Exception DrawFinalizer(bool __state, Exception __exception)
        {
            if (__state)
                LangField.SetValue(null, LocalizedContentManager.LanguageCode.mod);
            return __exception;
        }
    }
}
