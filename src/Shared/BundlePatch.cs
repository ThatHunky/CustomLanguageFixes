using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguage.Shared
{
    // Локалізовані назви клунків Джунімо без ритуалу «збережись і перезайди».
    // Платформонезалежний код: SMAPI-івенти + ванільні NetWorldState/LocalizedContentManager
    // (десктопний NetWorldState звірено декомпілом — внутрішній стан ідентичний мобільному).
    internal static class BundlePatch
    {
        private static IModHelper _helper;
        private static IMonitor _log;
        private static Func<bool> _enabled;
        private static bool _rebuilding;

        public static void Apply(IModHelper helper, IMonitor log, Func<bool> enabled)
        {
            _helper = helper;
            _log = log;
            _enabled = enabled;
            helper.Events.GameLoop.SaveLoaded += (_, _) => RefreshBundleNames();
            // мовний пак може вмикати переклад клунків за умовою стану сейва (напр., Pereclaw
            // гейтить Data/Bundles.uk), тож CP підміняє асет посеред сесії — ловимо і перебудовуємо
            helper.Events.Content.AssetsInvalidated += (_, e) =>
            {
                if (!_enabled() || !Context.IsWorldReady || _rebuilding)
                    return;
                foreach (var n in e.Names)
                {
                    string name = n.Name.Replace('\\', '/');
                    if (name.StartsWith("Data/Bundles", StringComparison.OrdinalIgnoreCase))
                    {
                        RebuildNames();
                        break;
                    }
                }
            };
        }

        // перебудувати кеш назв зі свіжого асета (без інвалідацій — щоб не зациклитись)
        private static void RebuildNames()
        {
            try
            {
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                var ws = Game1.netWorldState?.Value;
                if (ws == null)
                    return;
                AccessTools.Field(ws.GetType(), "_bundleDataDirty")?.SetValue(ws, true);
                _ = ws.BundleData;
                _log.Log(_helper.Translation.Get("log.bundles-rebuilt"), LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _log.Log(_helper.Translation.Get("log.bundles-failed", new { error = ex.Message }), LogLevel.Warn);
            }
        }

        public static void RefreshBundleNames()
        {
            _rebuilding = true;
            try
            {
                if (!_enabled())
                    return;
                if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                    return;
                var ws = Game1.netWorldState?.Value;
                if (ws == null)
                    return;

                // статичний резолвер локалізованих асетів чиститься лише при зміні КОДУ мови
                // чи виході в меню; застиглий маршрут «Data\Bundles → база (en)» переживає
                // і перемикання mod→mod, і InvalidateCache — чистимо самі
                LocalizedContentManager.localizedAssetNames.Clear();

                _helper.GameContent.InvalidateCache("Data/Bundles");
                _helper.GameContent.InvalidateCache("Strings/BundleNames");

                // кеш назв позначаємо брудним — геттер сам перечитає локалізований асет через
                // UpdateBundleDisplayNames(). Прогрес/remixed не зачіпаються: SetBundleData НЕ викликаємо.
                AccessTools.Field(ws.GetType(), "_bundleDataDirty")?.SetValue(ws, true);
                _ = ws.BundleData;
                _log.Log(_helper.Translation.Get("log.bundles-refreshed"), LogLevel.Trace);
            }
            catch (Exception ex)
            {
                _log.Log(_helper.Translation.Get("log.bundles-failed", new { error = ex.Message }), LogLevel.Warn);
            }
            finally
            {
                _rebuilding = false;
            }
        }
    }
}
