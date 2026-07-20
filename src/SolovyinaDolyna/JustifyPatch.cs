using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace SolovyinaDolyna
{
    // Justify для тексту діалогів (мод-мова): розтягуємо пробіли так, щоб правий край був рівний.
    // Перехоплюємо SpriteText.drawString ТІЛЬКИ коли він викликаний з DialogueBox.draw для основного
    // тексту (characterPosition != 999999 — друкарська машинка; варіанти відповідей не чіпаємо).
    internal static class JustifyPatch
    {
        private static bool _inDialogueDraw;
        private static bool _rendering; // guard від рекурсії: наш рендер сам кличе drawString

        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.draw), new[] { typeof(SpriteBatch) }),
                prefix: new HarmonyMethod(typeof(JustifyPatch), nameof(DialogueDrawPrefix)),
                finalizer: new HarmonyMethod(typeof(JustifyPatch), nameof(DialogueDrawFinalizer)));
            harmony.Patch(
                original: AccessTools.Method(typeof(SpriteText), nameof(SpriteText.drawString)),
                prefix: new HarmonyMethod(typeof(JustifyPatch), nameof(DrawStringPrefix)));
        }

        private static void DialogueDrawPrefix() => _inDialogueDraw = true;
        private static Exception DialogueDrawFinalizer(Exception __exception)
        { _inDialogueDraw = false; return __exception; }

        private static bool DrawStringPrefix(SpriteBatch b, string s, int x, int y,
            int characterPosition, int width, int height, float alpha, float layerDepth,
            bool junimoText, int drawBGScroll, string placeHolderScrollWidthText, Color? color)
        {
            if (!_inDialogueDraw || _rendering
                || LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod
                || width <= 0 || characterPosition == 999999 || string.IsNullOrEmpty(s))
                return true; // не наш випадок — ванільний рендер

            _rendering = true;
            try
            {
                DrawJustified(b, s, x, y, characterPosition, width, alpha, layerDepth, junimoText, color);
                return false; // ванільний drawString не викликаємо
            }
            catch
            {
                return true; // будь-що пішло не так — падаємо назад на ванільний рендер
            }
            finally
            {
                _rendering = false;
            }
        }

        private static void DrawJustified(SpriteBatch b, string s, int x, int y,
            int charLimit, int width, float alpha, float layerDepth, bool junimoText, Color? color)
        {
            int lineH = SpriteText.getHeightOfString("\u0410\u0431"); // висота одного рядка
            int spaceW = SpriteText.getWidthOfString("\u0430 \u0430") - 2 * SpriteText.getWidthOfString("\u0430");
            if (spaceW <= 0)
                spaceW = (int)(8f * SpriteText.fontPixelZoom / 3f);

            int drawn = 0; // скільки символів вихідного рядка вже "надруковано" (для машинки)
            int curY = y;

            foreach (string paragraph in s.Split('\n'))
            {
                var words = paragraph.Split(' ');
                var line = new List<(string w, int px)>();
                int lineW = 0;

                void FlushLine(bool lastLine)
                {
                    if (line.Count == 0) { curY += lineH; return; }
                    int gaps = line.Count - 1;
                    float add = 0f;
                    if (!lastLine && gaps > 0)
                    {
                        float extra = width - lineW;
                        add = Math.Min(extra / gaps, spaceW * 1.5f); // не розтягуємо до карикатури
                        if (add < 0f) add = 0f;
                    }
                    float cx = x;
                    foreach (var (w, px) in line)
                    {
                        if (drawn >= charLimit) return;
                        string part = (charLimit - drawn >= w.Length) ? w : w.Substring(0, charLimit - drawn);
                        if (part.Length > 0)
                            SpriteText.drawString(b, part, (int)cx, curY, 999999, -1, 999999, alpha, layerDepth, junimoText, -1, "", color);
                        drawn += w.Length + 1; // +1 за пробіл після слова
                        cx += px + spaceW + add;
                    }
                    curY += lineH;
                }

                foreach (string w in words)
                {
                    int px = SpriteText.getWidthOfString(w);
                    int projected = (line.Count == 0) ? px : lineW + spaceW + px;
                    if (line.Count > 0 && projected > width)
                    {
                        FlushLine(lastLine: false);
                        if (drawn >= charLimit) return;
                        line.Clear();
                        lineW = px;
                    }
                    else
                        lineW = projected;
                    line.Add((w, px));
                }
                FlushLine(lastLine: true); // останній рядок абзацу не розтягуємо
                if (drawn >= charLimit) return;
            }
        }
    }
}
