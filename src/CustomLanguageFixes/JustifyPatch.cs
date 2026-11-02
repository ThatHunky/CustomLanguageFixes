using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace CustomLanguageFixes
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
            if (!ModEntry.Config.JustifyDialogue || !_inDialogueDraw || _rendering
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

            // Як і ванільний drawString: переводи рядка ОС викидаються ДО індексації машинки,
            // а розрив рядка в тексті гри — '^' (SpriteText.newLine), не '\n'.
            s = s.Replace(Environment.NewLine, "");
            // ванільний перенос спрацьовує, коли кінець слова >= x + width - 4 — тримаємо той самий край
            int maxW = width - 4;
            int curY = y;
            var line = new List<(string w, int px, int start)>();
            int lineW = 0;

            // false = машинка ще не дійшла до кінця рядка, далі малювати нічого
            bool FlushLine(bool lastLine)
            {
                bool more = true;
                int gaps = line.Count - 1;
                float add = 0f;
                if (!lastLine && gaps > 0)
                {
                    float extra = maxW - lineW;
                    add = Math.Min(extra / gaps, spaceW * 1.5f); // не розтягуємо до карикатури
                    if (add < 0f) add = 0f;
                }
                float cx = x;
                foreach (var (w, px, start) in line)
                {
                    if (start >= charLimit) { more = false; break; }
                    string part = (charLimit - start >= w.Length) ? w : w.Substring(0, charLimit - start);
                    SpriteText.drawString(b, part, (int)cx, curY, 999999, -1, 999999, alpha, layerDepth, junimoText, -1, "", color);
                    if (part.Length < w.Length) { more = false; break; }
                    cx += px + spaceW + add;
                }
                line.Clear();
                lineW = 0;
                curY += lineH;
                return more;
            }

            int pos = 0;
            while (pos <= s.Length)
            {
                int pEnd = s.IndexOf(SpriteText.newLine, pos);
                if (pEnd < 0) pEnd = s.Length;

                // слова абзацу з їхнім індексом у вихідному рядку (індекс потрібен машинці);
                // порожні «слова» від подвійних пробілів пропускаємо — проміжок однаково рахуємо самі
                int wStart = pos;
                while (wStart <= pEnd)
                {
                    int wEnd = s.IndexOf(' ', wStart, pEnd - wStart);
                    if (wEnd < 0) wEnd = pEnd;
                    if (wEnd > wStart)
                    {
                        string w = s.Substring(wStart, wEnd - wStart);
                        int px = SpriteText.getWidthOfString(w);
                        int projected = (line.Count == 0) ? px : lineW + spaceW + px;
                        if (line.Count > 0 && projected > maxW)
                        {
                            if (!FlushLine(lastLine: false)) return;
                            projected = px;
                        }
                        lineW = projected;
                        line.Add((w, px, wStart));
                    }
                    wStart = wEnd + 1;
                }

                // останній рядок абзацу не розтягуємо; порожній абзац (^^) — просто порожній рядок
                if (pos >= charLimit || !FlushLine(lastLine: true)) return;
                pos = pEnd + 1;
            }
        }
    }
}
