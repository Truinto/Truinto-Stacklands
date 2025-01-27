global using HarmonyLib;
using UnityEngine;

namespace SimpleFarmNS
{
    public class SimpleFarm : Mod
    {
        public static Mod Instance = null!;

        public void Awake()
        {
            Instance = this;
            CreateSetting("greenhouse_extra_food", false, restartAfterChange: true);

            PatchSafe(typeof(Patch_Greenhouse));
            Logger.Log($"Awake!");
        }

        public override void Ready()
        {
            Config.OnSave = OnSettingsChanged;
            bool extra_food = Config.GetValue<bool>("greenhouse_extra_food");

            //var cards = WorldManager.instance.CardDataPrefabs;

            var blueprints = WorldManager.instance.BlueprintPrefabs;
            foreach (var blueprint in blueprints)
            {
                if (blueprint is not BlueprintGrowth growth)
                    continue;

                foreach (var subprint in growth.Subprints)
                {
                    if (subprint.RequiredCards != null
                        && subprint.RequiredCards.Length == 2
                        && subprint.RequiredCards[1] is "garden" or "farm" or "greenhouse"
                        && subprint.ExtraResultCards != null
                        && subprint.ExtraResultCards.Length > 0)
                    {
                        // replace food trees and bushes with just the food
                        var card_in = subprint.RequiredCards[0];
                        if (subprint.ExtraResultCards.Length == 1)
                        {
                            if (card_in is "stick")
                                subprint.ExtraResultCards = ["stick", "tree"];
                            else
                                subprint.ExtraResultCards = [card_in, card_in];
                        }

                        // greenhouse boosts
                        if (subprint.RequiredCards[1] is "greenhouse")
                        {
                            int foodvalue = WorldManager.instance.GetCardPrefab(card_in, false) is Food food ? food.FoodValue : 0;
                            if (foodvalue == 1 || extra_food)
                            {
                                Array.Resize(ref subprint.ExtraResultCards, subprint.ExtraResultCards.Length + 1);
                                subprint.ExtraResultCards[^1] = card_in;
                            }
                            //subprint.Time = Math.Max(10f, subprint.Time - 10f);
                        }
                    }

                    Logger.Log($"{subprint.RequiredCards?.Join()} => {subprint.ExtraResultCards?.Join()}");
                }
            }

            Logger.Log($"Ready!");
        }

        public void OnSettingsChanged()
        {
        }

        #region Helpers

        public static T GetSetting<T>(string key)
        {
            return Instance.Config.GetValue<T>(key);
        }

        public static void Log(string message)
        {
            Instance.Logger.Log(message);
        }

        private string? Loc;
        internal ConfigEntry<T> CreateSetting<T>(string name, T defaultValue, bool restartAfterChange = false)
        {
            if (Loc == null)
            {
                try
                {
                    string locPath = System.IO.Path.Combine(this.Path, "localization.tsv");
                    if (File.Exists(locPath))
                        Loc = File.ReadAllText(locPath);
                } catch (Exception) { }
                Loc ??= "";
            }

            var config = Config.GetEntry<T>(name, defaultValue);
            if (Loc.Contains(name + "\t", StringComparison.Ordinal))
                config.UI.NameTerm = name;
            else
                config.UI.Name = name;
            if (Loc.Contains(name + "_tt\t", StringComparison.Ordinal))
                config.UI.TooltipTerm = name + "_tt";
            config.UI.RestartAfterChange = restartAfterChange;
            return config;
        }

        internal void LoadFallbackTranslation()
        {
            try
            {
                var sokLoc = SokLoc.instance;
                string[][] array = SokLoc.ParseTableFromTsv(File.ReadAllText(System.IO.Path.Combine(this.Path, "localization.tsv")));
                int languageColumnIndex = SokLoc.GetLanguageColumnIndex(array, "English");
                if (languageColumnIndex == -1)
                    return;

                for (int i = 1; i < array.Length; i++)
                {
                    string term = array[i][0];
                    string fullText = array[i][languageColumnIndex];
                    term = term.Trim().ToLower();
                    if (string.IsNullOrEmpty(term))
                        continue;

                    if (!sokLoc.CurrentLocSet.TermLookup.ContainsKey(term))
                    {
                        SokTerm sokTerm = new SokTerm(sokLoc.CurrentLocSet, term, fullText);
                        sokLoc.CurrentLocSet.AllTerms.Add(sokTerm);
                        sokLoc.CurrentLocSet.TermLookup.Add(term, sokTerm);
                    }
                }
            } catch (Exception) { }
        }

        internal bool PatchSafe(Type patch, bool enabled = true)
        {
            if (!enabled)
                return false;
            try
            {
                Logger.Log($"Patching {patch.Name}");
                Harmony.CreateClassProcessor(patch).Patch();
                return true;
            } catch (Exception e)
            {
                Logger.LogException(e.ToString());
                return false;
            }
        }

        #endregion
    }
}
