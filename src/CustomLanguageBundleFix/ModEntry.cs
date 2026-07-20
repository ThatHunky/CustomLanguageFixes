using CustomLanguage.Shared;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguageBundleFix
{
    public class ModConfig
    {
        // Єдина опція мода — аварійний вимикач у config.json, у GMCM не виводиться:
        // вимикати сам фікс нема сенсу, а назви, вже локалізовані в поточній сесії,
        // повернуться англійськими лише після перезавантаження сейва.
        public bool BundleNamesFix { get; set; } = true;
    }

    public class ModEntry : Mod
    {
        private ModConfig _config;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            BundlePatch.Apply(helper, this.Monitor, () => _config.BundleNamesFix);
            // зміна мови посеред сесії (на ПК меню мов саме фаєрить OnLanguageChange)
            LocalizedContentManager.OnLanguageChange += _ =>
            {
                if (Context.IsWorldReady)
                    BundlePatch.RefreshBundleNames();
            };
        }
    }
}
