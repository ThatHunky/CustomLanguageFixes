using CustomLanguage.Shared;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CustomLanguageBundleFix
{
    public class ModConfig
    {
        // Аварійні вимикачі в config.json (GMCM на ПК нема): у нормі чіпати не треба.
        public bool BundleNamesFix { get; set; } = true;
        public bool SocialSingleFix { get; set; } = true; // «холостий»/«незаміжня» на слоті фермера за статтю
        public bool RecipeSuffix { get; set; } = true;    // «(Рецепт)»/«(Креслення)», лише для uk
    }

    public class ModEntry : Mod
    {
        private ModConfig _config;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            BundlePatch.Apply(helper, this.Monitor, () => _config.BundleNamesFix);
            var harmony = new Harmony(this.ModManifest.UniqueID);
            SocialPatch.Apply(harmony, () => _config.SocialSingleFix); // стать у статусі «самотній» на слоті фермера
            RecipePatch.Apply(harmony, () => _config.RecipeSuffix);    // «(Рецепт)»/«(Креслення)» для укр. мод-мови
            // зміна мови посеред сесії (на ПК меню мов саме фаєрить OnLanguageChange)
            LocalizedContentManager.OnLanguageChange += _ =>
            {
                if (Context.IsWorldReady)
                    BundlePatch.RefreshBundleNames();
            };
        }
    }
}
