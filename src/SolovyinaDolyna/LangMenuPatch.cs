using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Menus;

namespace SolovyinaDolyna
{
    // Інтегрує мод-мови у вбудоване мобільне LanguageSelectionMenu (кнопка-бульбашка на титулці).
    // Вбудований список — хардкод 12 мов; докидаємо свої рядки знизу, розширюємо скрол і ловимо тапи.
    internal static class LangMenuPatch
    {
        private static readonly AccessTools.FieldRef<LanguageSelectionMenu, int> ButtonHeightF =
            AccessTools.FieldRefAccess<LanguageSelectionMenu, int>("buttonHeight");
        private static readonly AccessTools.FieldRef<LanguageSelectionMenu, Rectangle> MainBoxF =
            AccessTools.FieldRefAccess<LanguageSelectionMenu, Rectangle>("mainBox");
        private static readonly AccessTools.FieldRef<LanguageSelectionMenu, MobileScrollbox> ScrollAreaF =
            AccessTools.FieldRefAccess<LanguageSelectionMenu, MobileScrollbox>("scrollArea");
        private static readonly AccessTools.FieldRef<LanguageSelectionMenu, MobileScrollbar> ScrollbarF =
            AccessTools.FieldRefAccess<LanguageSelectionMenu, MobileScrollbar>("newScrollbar");

        private static List<ModLanguage> _langs = new();
        private static Texture2D[] _textures;
        private const int VanillaCount = 12;

        public static void Apply(Harmony harmony)
        {
            var t = typeof(LanguageSelectionMenu);
            harmony.Patch(AccessTools.Method(t, "SetupButtons"),
                postfix: new HarmonyMethod(typeof(LangMenuPatch), nameof(SetupPostfix)));
            harmony.Patch(AccessTools.Method(t, nameof(LanguageSelectionMenu.draw), new[] { typeof(SpriteBatch) }),
                postfix: new HarmonyMethod(typeof(LangMenuPatch), nameof(DrawPostfix)));
            harmony.Patch(AccessTools.Method(t, nameof(LanguageSelectionMenu.releaseLeftClick)),
                postfix: new HarmonyMethod(typeof(LangMenuPatch), nameof(ClickPostfix)));
        }

        private static void SetupPostfix(LanguageSelectionMenu __instance)
        {
            try
            {
                _langs = Game1.content.Load<List<ModLanguage>>("Data\\AdditionalLanguages") ?? new();
            }
            catch { _langs = new(); }
            if (_langs.Count == 0) { _textures = null; return; }

            // підвантажити ButtonTexture кожного пака (як на десктопі), null = fallback на текст
            _textures = new Texture2D[_langs.Count];
            for (int i = 0; i < _langs.Count; i++)
            {
                try { _textures[i] = Game1.content.Load<Texture2D>(_langs[i].ButtonTexture); }
                catch { _textures[i] = null; }
            }

            // розширити скрол-контент: ванільний maxYOffset рахований під 12 кнопок
            int bh = ButtonHeightF(__instance);
            int n = VanillaCount + _langs.Count;
            var mainBox = MainBoxF(__instance);
            int visibleH = mainBox.Height;
            bool needScroll = n * bh > visibleH - 32; // контент вищий за видиму рамку
            if (needScroll)
            {
                int contentOverflow = (int)((float)bh * ((float)n - (float)(visibleH - 32) / (float)bh));
                var bar = ScrollbarF(__instance) ?? new MobileScrollbar(mainBox.X + __instance.buttonWidth, mainBox.Y + 16, 24, visibleH - 36, 0, 32);
                ScrollbarF(__instance) = bar;
                ScrollAreaF(__instance) = new MobileScrollbox(mainBox.X, mainBox.Y, __instance.buttonWidth, visibleH,
                    contentOverflow, new Rectangle(mainBox.X + 16, mainBox.Y + 16, __instance.buttonWidth, visibleH - 32), bar);
            }
        }

        private static Rectangle RowBounds(LanguageSelectionMenu m, int i)
        {
            int bh = ButtonHeightF(m);
            var box = MainBoxF(m);
            int yOff = ScrollAreaF(m)?.getYOffsetForScroll() ?? 0;
            return new Rectangle(box.X + 16, yOff + box.Y + 16 + bh * (VanillaCount + i), m.buttonWidth, bh);
        }

        private static void DrawPostfix(LanguageSelectionMenu __instance, SpriteBatch b)
        {
            if (_langs.Count == 0) return;
            bool isMod = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.mod;

            // кліпимо тим самим скісором, що й ванільні кнопки — ряди плавно виїжджають з-під рамки
            var area = ScrollAreaF(__instance);
            area?.setUpForScrollBoxDrawing(b);
            try
            {
                for (int i = 0; i < _langs.Count; i++)
                {
                    var r = RowBounds(__instance, i);
                    bool active = isMod && LocalizedContentManager.CurrentModLanguage?.Id == _langs[i].Id;
                    var tint = active ? Color.LightYellow : Color.White;
                    var tex = _textures?[i];
                    if (tex != null)
                        b.Draw(tex, r, new Rectangle(0, 0, tex.Width, tex.Height / 2), tint); // верхня половина = normal
                    else
                    {
                        IClickableMenu.drawTextureBox(b, r.X, r.Y, r.Width, r.Height, tint);
                        string label = (_langs[i].LanguageCode ?? "mod").ToUpperInvariant();
                        var size = Game1.dialogueFont.MeasureString(label);
                        b.DrawString(Game1.dialogueFont, label,
                            new Vector2(r.X + (r.Width - size.X) / 2, r.Y + (r.Height - size.Y) / 2), Game1.textColor);
                    }
                }
            }
            finally
            {
                area?.finishScrollBoxDrawing(b);
            }
        }

        private static void ClickPostfix(LanguageSelectionMenu __instance, int x, int y)
        {
            if (_langs.Count == 0) return;
            var area = ScrollAreaF(__instance);
            if (area != null && area.havePanelScrolled)
                return; // це був скрол, не тап
            for (int i = 0; i < _langs.Count; i++)
            {
                if (!RowBounds(__instance, i).Contains(x, y))
                    continue;
                Game1.playSound("select");
                LocalizedContentManager.SetModLanguage(_langs[i]);
                var prefs = new StartupPreferences();
                prefs.loadPreferences(false, true);
                prefs.savePreferences(false, true);
                ModEntry.Config.PreferredLanguage = _langs[i].Id;
                ModEntry.H.WriteConfig(ModEntry.Config);
                ModEntry.Log.Log($"Мову вибрано в меню: {_langs[i].Id}", StardewModdingAPI.LogLevel.Info);
                __instance.exitThisMenu();
                return;
            }
        }
    }
}
