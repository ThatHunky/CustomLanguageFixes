using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;

namespace BundleFixTestHarness
{
    // Тільки для розробки. Вдає мовний пак, який гейтить переклад клунків станом сейва:
    // реєструє мод-мову «test» і, ПОКИ ввімкнена умова, віддає Data/Bundles з маркером у полі 6.
    //
    // Головне: сам харнес НІЧОГО не інвалідує. Саме так поводиться пак із дефолтним
    // update-rate CP (OnDayStart) — і саме цю прогалину має закрити BundlePatch, інвалідуючи
    // Data/Bundles на вході до Ремісничої кімнати. Тобто харнес не допомагає фіксу, а перевіряє його.
    //
    // Content Patcher і справжній пак не потрібні: гейт перемикається консольною командою.
    public class ModEntry : Mod
    {
        private const string LangId = "BundleFixTest";
        private const string LangCode = "test";
        private const string ButtonAsset = "ThatHunky.BundleFixTestHarness/button";
        private const string Marker = "[TEST] ";

        private bool _packCondition;
        private readonly Queue<Action> _pending = new Queue<Action>();

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            // консольні команди приходять з іншого потоку — стан гри чіпаємо лише в геймлупі
            helper.Events.GameLoop.UpdateTicked += (_, _) =>
            {
                while (true)
                {
                    Action next;
                    lock (_pending)
                    {
                        if (_pending.Count == 0)
                            return;
                        next = _pending.Dequeue();
                    }
                    next();
                }
            };

            helper.ConsoleCommands.Add("bft_on", "Увімкнути умову «пака» (переклад клунків). Асет НЕ інвалідується — навмисно.", (_, _) => Enqueue(() =>
            {
                _packCondition = true;
                Monitor.Log("Умова «пака» УВІМКНЕНА. Нічого не інвалідував — назви мусять лишитися старими до тригера.", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("bft_off", "Вимкнути умову «пака».", (_, _) => Enqueue(() =>
            {
                _packCondition = false;
                Monitor.Log("Умова «пака» вимкнена.", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("bft_names", "Показати мову, локацію і назви клунків, як їх бачить гра.", (_, _) => Enqueue(ReportNames));
            helper.ConsoleCommands.Add("bft_warp", "Телепорт до Ремісничої кімнати (тригер фікса).", (_, _) => Enqueue(() =>
            {
                if (!Context.IsWorldReady)
                {
                    Monitor.Log("Сейв не завантажено.", LogLevel.Warn);
                    return;
                }
                // (32, 9) — плитка, куди сама гра ставить гравця в CommunityCenter.loadArea
                Game1.warpFarmer("CommunityCenter", 32, 9, false);
                Monitor.Log("Варп до CommunityCenter відправлено.", LogLevel.Info);
            }));
            helper.ConsoleCommands.Add("bft_invalidate", "Інвалідувати Data/Bundles вручну — самоперевірка харнеса, а не фікса.", (_, _) => Enqueue(() =>
            {
                helper.GameContent.InvalidateCache("Data/Bundles");
                Monitor.Log("Data/Bundles інвалідовано вручну.", LogLevel.Info);
            }));
        }

        private void Enqueue(Action action)
        {
            lock (_pending)
                _pending.Enqueue(action);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // мод-мова, щоб CurrentLanguageCode став LanguageCode.mod (без цього фікс спить)
            if (e.NameWithoutLocale.IsEquivalentTo("Data/AdditionalLanguages"))
            {
                e.Edit(asset => asset.GetData<List<ModLanguage>>().Add(new ModLanguage
                {
                    Id = LangId,
                    LanguageCode = LangCode,
                    ButtonTexture = ButtonAsset,
                    UseLatinFont = true,
                    TimeFormat = "[HOURS_12]:[MINUTES] [AM_PM]",
                    ClockTimeFormat = "[HOURS_12]:[MINUTES] [AM_PM]",
                    ClockDateFormat = "[DAY_OF_WEEK] [DAY_OF_MONTH]"
                }));
                return;
            }

            // кнопка мови в меню: 174×78 (два стани по 39 рядків), генеруємо, щоб не тягти PNG
            if (e.NameWithoutLocale.IsEquivalentTo(ButtonAsset))
            {
                e.LoadFrom(() =>
                {
                    var tex = new Texture2D(Game1.graphics.GraphicsDevice, 174, 78);
                    var px = new Color[174 * 78];
                    for (int y = 0; y < 78; y++)
                    {
                        var row = (y < 39) ? new Color(170, 60, 150) : new Color(215, 110, 195);
                        for (int x = 0; x < 174; x++)
                            px[y * 174 + x] = row;
                    }
                    tex.SetData(px);
                    return tex;
                }, AssetLoadPriority.Medium);
                return;
            }

            // «переклад» клунків: маркер у полі 6, поле 0 НЕ чіпаємо — інакше
            // UpdateBundleDisplayNames() не зматчить бандл і фікс тихо нічого не зробить
            if (e.NameWithoutLocale.StartsWith("Data/Bundles") && _packCondition)
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    foreach (var key in new List<string>(data.Keys))
                    {
                        var f = data[key].Split('/');
                        if (f.Length < 7)
                            Array.Resize(ref f, 7);
                        f[6] = Marker + f[0];
                        data[key] = string.Join("/", f);
                    }
                });
            }
        }

        private void ReportNames()
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Сейв не завантажено.", LogLevel.Warn);
                return;
            }

            Monitor.Log($"мова: {LocalizedContentManager.CurrentLanguageCode} / пак-код: {LocalizedContentManager.CurrentModLanguage?.LanguageCode ?? "—"}", LogLevel.Info);
            if (LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.mod)
                Monitor.Log("УВАГА: активна не мод-мова, фікс за задумом спить. Вибери «test» у меню мов.", LogLevel.Warn);
            Monitor.Log($"умова «пака»: {(_packCondition ? "УВІМКНЕНА" : "вимкнена")} / локація: {Game1.currentLocation?.NameOrUniqueName ?? "—"}", LogLevel.Info);

            int marked = 0, total = 0;
            var samples = new List<string>();
            foreach (var pair in Game1.netWorldState.Value.BundleData)
            {
                var f = pair.Value.Split('/');
                string baseName = (f.Length > 0) ? f[0] : "?";
                string display = (f.Length > 6) ? f[6] : "(поля 6 немає)";
                total++;
                if (display.StartsWith(Marker))
                    marked++;
                if (samples.Count < 3)
                    samples.Add($"{pair.Key}: «{baseName}» → «{display}»");
            }
            Monitor.Log($"назв із маркером: {marked}/{total}", (total > 0 && marked == total) ? LogLevel.Info : LogLevel.Warn);
            foreach (var s in samples)
                Monitor.Log("    " + s, LogLevel.Info);
        }
    }
}
