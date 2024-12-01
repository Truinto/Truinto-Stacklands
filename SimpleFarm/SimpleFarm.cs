global using HarmonyLib;

namespace SimpleFarmNS
{
    public class SimpleFarm : Mod
    {
        public void Awake()
        {
            Logger.Log($"Awake!");
        }

        public override void Ready()
        {
            //WorldManager.instance.CardDataPrefabs

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
                        var card_in = subprint.RequiredCards[0];
                        if (subprint.ExtraResultCards.Length == 1)
                        {
                            if (card_in is not ("stick"))
                                subprint.ExtraResultCards[0] = card_in;
                        }
                        else
                        {
                            Array.Resize(ref subprint.ExtraResultCards, subprint.ExtraResultCards.Length - 1);
                        }

                        // if CardsToRemove is null or empty, then the game will destroy all RequiredCard that are not structures
                        // thus we use a non existing id, which is catched by the game's code
                        subprint.CardsToRemove = ["removenocard"];
                    }

                    Logger.Log($"{subprint.RequiredCards?.Join()} => {subprint.ExtraResultCards?.Join()} || {subprint.CardsToRemove?.Join()}");
                }
            }

            Logger.Log($"Ready!");
        }

        internal bool PatchSafe(Type patch)
        {
            try
            {
                Harmony.CreateClassProcessor(patch).Patch();
                return true;
            } catch (Exception e)
            {
                Logger.LogException(e.ToString());
                return false;
            }
        }
    }
}
