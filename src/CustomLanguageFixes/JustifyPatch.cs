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
                // розкладку рахуємо ДО малювання: якщо щось кине — ще нічого не намальовано,
                // і ванільний рендер не накладеться на наш наполовину намальований текст
                var layout = GetLayout(s, width);
                DrawLayout(b, layout, x, y, characterPosition, alpha, layerDepth, junimoText, color);
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

        // Готова розкладка: слово, його індекс у рядку (для машинки), зсув X і номер рядка.
        private sealed class Layout
        {
            public readonly List<(string w, int start, float dx, int line)> Words = new();
            public int LineH;
        }

        // Кеш розкладки: текст і ширина діалогу між кадрами не міняються, а міряти кожне
        // слово щокадру — зайві алокації й GC на Android.
        private static Layout _cached;
        private static string _cachedS;
        private static int _cachedWidth;
        private static float _cachedZoom;
        private static object _cachedLang;

        private static Layout GetLayout(string s, int width)
        {
            float zoom = SpriteText.fontPixelZoom;
            object lang = LocalizedContentManager.CurrentModLanguage;
            if (_cached != null && width == _cachedWidth && zoom == _cachedZoom
                && ReferenceEquals(lang, _cachedLang) && s == _cachedS)
                return _cached;
            var layout = BuildLayout(s, width);
            (_cached, _cachedS, _cachedWidth, _cachedZoom, _cachedLang) = (layout, s, width, zoom, lang);
            return layout;
        }

        private static Layout BuildLayout(string s, int width)
        {
            var layout = new Layout();
            // крок між рядками, а не висота одного рядка: у спрайтовому шрифті (UseLatinFont)
            // висота рядка 16*zoom, а ванільний перенос зсуває на 18*zoom
            layout.LineH = SpriteText.getHeightOfString("\u0410^\u0431") - SpriteText.getHeightOfString("\u0410");
            int spaceW = SpriteText.getWidthOfString("\u0430 \u0430") - 2 * SpriteText.getWidthOfString("\u0430");
            if (spaceW <= 0)
                spaceW = (int)(8f * SpriteText.fontPixelZoom / 3f);

            // Як і ванільний drawString: переводи рядка ОС викидаються ДО індексації машинки,
            // а розрив рядка в тексті гри — '^' (SpriteText.newLine), не '\n'.
            s = s.Replace(Environment.NewLine, "");
            // ванільний перенос спрацьовує, коли кінець слова >= x + width - 4 — тримаємо той самий край
            int maxW = width - 4;
            int lineIdx = 0;
            var line = new List<(string w, int px, int start)>();
            int lineW = 0;

            void FlushLine(bool lastLine)
            {
                int gaps = line.Count - 1;
                float add = 0f;
                if (!lastLine && gaps > 0)
                {
                    float extra = maxW - lineW;
                    add = Math.Min(extra / gaps, spaceW * 1.5f); // не розтягуємо до карикатури
                    if (add < 0f) add = 0f;
                }
                float dx = 0f;
                foreach (var (w, px, start) in line)
                {
                    layout.Words.Add((w, start, dx, lineIdx));
                    dx += px + spaceW + add;
                }
                line.Clear();
                lineW = 0;
                lineIdx++;
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
                            FlushLine(lastLine: false);
                            projected = px;
                        }
                        lineW = projected;
                        line.Add((w, px, wStart));
                    }
                    wStart = wEnd + 1;
                }

                // останній рядок абзацу не розтягуємо; порожній абзац (^^) — просто порожній рядок
                FlushLine(lastLine: true);
                pos = pEnd + 1;
            }
            return layout;
        }

        private static void DrawLayout(SpriteBatch b, Layout layout, int x, int y,
            int charLimit, float alpha, float layerDepth, bool junimoText, Color? color)
        {
            foreach (var (w, start, dx, line) in layout.Words)
            {
                if (start >= charLimit)
                    return; // машинка ще не дійшла до цього слова
                string part = (charLimit - start >= w.Length) ? w : w.Substring(0, charLimit - start);
                SpriteText.drawString(b, part, (int)(x + dx), y + line * layout.LineH, 999999, -1, 999999,
                    alpha, layerDepth, junimoText, -1, "", color);
                if (part.Length < w.Length)
                    return;
            }
        }
    }
}
