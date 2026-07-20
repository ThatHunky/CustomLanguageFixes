using HarmonyLib;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace CustomLanguageFixes
{
    // Мобільний SpriteText.SetFontPixelZoom() не має гілки mod і затирає зум пака (3.3 -> 3)
    // при кожному shrinkFont(). Через це wrap міряється одним зумом, а малюється іншим -> діри
    // в кінці рядків діалогів. Відновлюємо зум пака після кожного перерахунку.
    internal static class FontPatch
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SpriteText), "SetFontPixelZoom"),
                postfix: new HarmonyMethod(typeof(FontPatch), nameof(Postfix))
            );
        }

        private static void Postfix()
        {
            if (!ModEntry.Config.FontZoomFix)
                return;
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                return; // гейт на mod, бо CurrentModLanguage може лишатись не-null після перемикання на вбудовану
            var lang = LocalizedContentManager.CurrentModLanguage;
            if (lang != null && lang.FontPixelZoom > 0f)
                SpriteText.fontPixelZoom = lang.FontPixelZoom;
        }
    }
}
