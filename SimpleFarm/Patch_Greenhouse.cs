using Shared;
using System.Reflection;
using System.Reflection.Emit;

namespace SimpleFarmNS
{
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
            tool++; // this should be string.op_Equality
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
                    //for (int i = 0; i < __instance.allResultCards.Count; i++) __instance.allResultCards[i].SetFoil();
                }

                return stack || rootCard.CardData.Id is "garden" or "farm";
            }
        }
    }
}
