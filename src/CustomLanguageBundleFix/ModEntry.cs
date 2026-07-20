using CustomLanguage.Shared;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CustomLanguageBundleFix
{
    public class ModConfig
    {
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
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return;

            gmcm.Register(this.ModManifest, () => _config = new ModConfig(), () => this.Helper.WriteConfig(_config));
            gmcm.AddBoolOption(this.ModManifest, () => _config.BundleNamesFix,
                v => { _config.BundleNamesFix = v; if (v && Context.IsWorldReady) BundlePatch.RefreshBundleNames(); },
                () => this.Helper.Translation.Get("config.bundles.name"),
                () => this.Helper.Translation.Get("config.bundles.desc"));
        }
    }
}
