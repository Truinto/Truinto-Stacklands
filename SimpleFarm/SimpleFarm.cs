﻿global using HarmonyLib;
using Shared;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SimpleFarmNS
{
    public class SimpleFarm : Mod
    {
        public void Awake()
        {
            PatchSafe(typeof(Patch_Greenhouse));
            Logger.Log($"Awake!");
        }

        public override void Ready()
        {
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
                            if (foodvalue == 1)
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
    }

    /// <summary>
    /// Makes garden and farm behave like greenhouses. This is preferable to changing the Subprint, because of spoilage.
    /// </summary>
    [HarmonyPatch(typeof(BlueprintGrowth), nameof(BlueprintGrowth.BlueprintComplete))]
    public static class Patch_Greenhouse
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(OpCodes.Ldstr, "greenhouse");
            tool++; // this should be string.compare
            tool.InsertAfter(patch);
            return tool;

            static bool patch(bool stack, GameCard rootCard, List<GameCard> involvedCards, Subprint print, BlueprintGrowth __instance)
            {
                // if source card was foil, make result card foil and 2% chance on any other card being foil
                if (print.ExtraResultCards != null
                    && print.ExtraResultCards.Length > 0
                    && involvedCards.FirstOrDefault(f => f.CardData.Id == print.ExtraResultCards[0]) is GameCard source_crop)
                {
                    if (source_crop.CardData.IsFoil)
                    {
                        for (int i = 0; i < __instance.allResultCards.Count; i++)
                        {
                            if (i == 0 || UnityEngine.Random.value < 0.02f)
                                __instance.allResultCards[i].SetFoil();
                        }
                    }
                }

                return stack || rootCard.CardData.Id is "garden" or "farm";
            }
        }
    }
}
