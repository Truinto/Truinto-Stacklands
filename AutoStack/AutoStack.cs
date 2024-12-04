global using HarmonyLib;

namespace AutoStackNS
{
    public class AutoStack : Mod
    {
        public static Mod Instance = null!;

        public void Awake()
        {
            Instance = this;

            CreateSetting("hotpot_capacity", 10000);
            CreateSetting("chest_capacity", 10000);
            CreateSetting("resourcechest_capacity", 10000);
            CreateSetting("resourcechest_cardlimit", false, restartAfterChange: true);
            Config.OnSave = OnSettingsChanged;

            PatchSafe(typeof(Patch_Hotpot));
            PatchSafe(typeof(Patch_SellHotkey));
            if (!Config.GetValue<bool>("resourcechest_cardlimit"))
                PatchSafe(typeof(Patch_ResourceChestLimit));
            Logger.Log($"Awake!");
        }

        public override void Ready()
        {
            //var cards = WorldManager.instance.CardDataPrefabs;
            //var blueprints = WorldManager.instance.BlueprintPrefabs;

            OnSettingsChanged();
            Loc = null;
            SokLoc.instance.LanguageChanged += LoadFallbackTranslation;
            Logger.Log($"Ready!");
        }

        public void OnSettingsChanged()
        {
            Logger.Log($"Updating capacities...");
            var cards = WorldManager.instance.CardDataPrefabs;
            foreach (var card in cards)
            {
                if (card is Hotpot pot) //hotpot
                    pot.MaxFoodValue = Config.GetValue<int>("hotpot_capacity"); // this is not copied to instances, because it's a private field!
                if (card is Chest chest)    //coin_chest, shell_chest
                    chest.MaxCoinCount = Config.GetValue<int>("chest_capacity");
                if (card is ResourceChest chest2)   //resource_chest
                    chest2.MaxResourceCount = Config.GetValue<int>("resourcechest_capacity");
            }
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

        internal bool PatchSafe(Type patch)
        {
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
