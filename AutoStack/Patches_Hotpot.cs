using Shared;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AutoStackNS
{
    /// <summary>
    /// Because <see cref="Hotpot.MaxFoodValue"/> is a private field, it won't keep the prefab.<br/>
    /// Instead we patch the constructor.
    /// </summary>
    [HarmonyPatch(typeof(Hotpot), MethodType.Constructor)]
    public static class Patch_HotpotMaxValue
    {
        public static void Postfix(Hotpot __instance)
        {
            __instance.MaxFoodValue = AutoStack.Instance.Config.GetValue<int>("hotpot_capacity");
        }
    }

    /// <summary>
    /// Make Hotpots act like chests for all food (item magnet).<br/>
    /// Also fixes credit cards (magnet dollars).
    /// </summary>
    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.TrySendToChest))]
    public static class Patch_HotpotIsChest
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            var card_list = tool.GetLocal(typeof(List<GameCard>));

            tool.Last();
            tool.Rewind(card_list, loadIsTrue_storeIsFalse: false);
            tool.InsertBefore(patch);

            return tool;

            static List<GameCard> patch(List<GameCard> stack, GameCard card)
            {
                if (stack.Count == 0)
                {
                    if (card.CardData is Food food
                                    && food.FoodValue > 0
                                    && food.MyCardType == CardType.Food)
                    {
                        return WorldManager.instance.AllCards.FindAll(f => f.CardData is Hotpot hotpot && hotpot.FoodValue < hotpot.MaxFoodValue);
                    }
                    else if (card.CardData is Dollar dollar)
                    {
                        return WorldManager.instance.AllCards.FindAll(f => f.CardData is Creditcard credit && credit.DollarCount < credit.MaxDollarCount);
                    }
                }
                return stack;
            }
        }
    }

    /// <summary>
    /// Allows Hotpot to work while parent is Mess Hall.
    /// </summary>
    [HarmonyPatch(typeof(Hotpot), nameof(Hotpot.UpdateCard))]
    public static class Patch_HotpotMessHall
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);

            tool.Seek(OpCodes.Isinst, typeof(HeavyFoundation));
            tool.Set(OpCodes.Call, ((Delegate)patch).GetMethodInfo()); // allow more parent objects

            tool.Seek(OpCodes.Isinst, typeof(MessHall));
            tool.NextJumpAlways();

            return tool;

            static bool patch(CardData __stack)
            {
                return __stack is HeavyFoundation or MessHall or Food;
            }
        }
    }
}
