using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoStackNS
{
    /// <summary>
    /// Make resources stack on resource camps.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_StackHarvestable
    {
        /// <summary>
        /// Worker compatibility.<br/>
        /// Allow harvestable on villager/worker.
        /// </summary>
        [HarmonyPatch(typeof(BaseVillager), nameof(BaseVillager.CanHaveCard))]
        [HarmonyPostfix]
        public static void Villager_CanHaveCard(CardData otherCard, ref bool __result)
        {
            __result = __result || otherCard is Worker;
            __result = __result || (otherCard is Harvestable && !otherCard.IsBuilding && otherCard.MyCardType is not CardType.Locations);
        }

        /// <summary>
        /// Give Workers (villagers from the cities board) the same stacking properties as villagers.<br/>
        /// Allow harvestable on villager/worker.
        /// </summary>
        [HarmonyPatch(typeof(Worker), nameof(Worker.CanHaveCard))]
        [HarmonyPostfix]
        public static void Worker_CanHaveCard(CardData otherCard, ref bool __result)
        {
            __result = __result || otherCard is BaseVillager || otherCard.MyCardType is CardType.Resources or CardType.Equipable || otherCard is Food { CanBePlacedOnVillager: true };
            __result = __result || (otherCard is Harvestable && !otherCard.IsBuilding && otherCard.MyCardType is not CardType.Locations);
        }

        /// <summary>
        /// Allow harvestables stack on each other regardless of type.
        /// </summary>
        [HarmonyPatch(typeof(Harvestable), nameof(Harvestable.CanHaveCard))]
        [HarmonyPostfix]
        public static void Harvestable_CanHaveCard(CardData otherCard, ref bool __result)
        {
            __result = __result || (otherCard is Harvestable && !otherCard.IsBuilding && otherCard.MyCardType is not CardType.Locations);
        }

        /// <summary>
        /// Allow harvestables on resource camps.
        /// </summary>
        [HarmonyPatch(typeof(CombatableHarvestable), nameof(CombatableHarvestable.CanHaveCard))]
        [HarmonyPostfix]
        public static void Camp_CanHaveCard(CardData otherCard, ref bool __result)
        {
            __result = __result || otherCard is Worker;
            __result = __result || (otherCard is Harvestable && !otherCard.IsBuilding && otherCard.MyCardType is not CardType.Locations);
        }

        /// <summary>
        /// Allow stacking while work is done.
        /// </summary>
        [HarmonyPatch(typeof(CardData), nameof(CardData.CanHaveCardsWhileHasStatus))]
        [HarmonyPostfix]
        public static void Cards_CanHaveCardsWhileHasStatus(CardData __instance, ref bool __result)
        {
            if (AllowStackingWhilstWorking(__instance))
                __result = true;
        }

        /// <summary>
        /// Send harvestables to resouce camps.
        /// </summary>
        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.TrySendToChest))]
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.LowerThanNormal)]
        public static IEnumerable<CodeInstruction> SendToChest_ExtraStacking(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
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
                    if (!card.CardData.IsBuilding && card.CardData is Harvestable deposit)
                    {
                        return WorldManager.instance.AllCards.FindAll(f => f.CardData.IsBuilding && ShouldStack(f.CardData, deposit));
                    }
                }
                return stack;
            }
        }

        /// <summary>
        /// Allow workers on resource camps.<br/>
        /// Re-sort villager/worker to the bottom, when resource camp isn't working.
        /// </summary>
        [HarmonyPatch(typeof(CombatableHarvestable), nameof(CombatableHarvestable.UpdateCard))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Camp_MoveWorker(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.InsertBefore(patch1);
            tool.Seek(typeof(GameCard), nameof(GameCard.CancelTimer));
            tool.InsertBefore(patch2);
            return tool;

            static void patch1(CombatableHarvestable __instance)
            {
                if (!__instance.MyGameCard.TimerRunning && __instance.MyGameCard.Parent == null)
                {
                    var gameCard = __instance.MyGameCard.Child;
                    while (gameCard?.CardData is BaseVillager or Worker && gameCard.Child?.CardData is not (null or BaseVillager or Worker))
                    {
                        var next = gameCard.Child;

                        // cut out gameCard
                        gameCard.Child.SetParent(gameCard.Parent);
                        gameCard.Parent = null;
                        gameCard.Child = null;
                        gameCard.StackUpdate = true;

                        // put gameCard at the bottom
                        gameCard.SetParent(next.GetLeafCard());

                        gameCard = next;
                    }
                }
            }

            static string patch2(string __stack, CombatableHarvestable __instance)
            {
                if (__instance.HasCardOnTop(out Worker worker))
                {
                    __instance.MyGameCard.StartTimer(worker.GetActionTimeModifier() * __instance.HarvestTime, __instance.CompleteHarvest, __instance.StatusText, "complete_harvest");
                    return "do_not_actually_cancel_timer";
                }
                return __stack;
            }
        }

        /// <summary>
        /// Re-sort villager/worker to the bottom, when resource camp isn't working.
        /// </summary>
        [HarmonyPatch(typeof(Harvestable), nameof(Harvestable.UpdateCard))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Camp2_MoveWorker(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.InsertBefore(patch1);
            return tool;

            static void patch1(Harvestable __instance)
            {
                if (__instance.Id is "well"
                    && !__instance.MyGameCard.TimerRunning
                    && __instance.MyGameCard.Parent == null)
                {
                    var gameCard = __instance.MyGameCard.Child;
                    while (gameCard?.CardData is BaseVillager or Worker && gameCard.Child?.CardData is not (null or BaseVillager or Worker))
                    {
                        var next = gameCard.Child;

                        // cut out gameCard
                        gameCard.Child.SetParent(gameCard.Parent);
                        gameCard.Parent = null;
                        gameCard.Child = null;
                        gameCard.StackUpdate = true;

                        // put gameCard at the bottom
                        gameCard.SetParent(next.GetLeafCard());

                        gameCard = next;
                    }
                }
            }
        }

        /// <summary>
        /// Stop empting harvestable from splitting stack.
        /// </summary>
        [HarmonyPatch(typeof(Harvestable), nameof(Harvestable.CompleteHarvest))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Quarry_FixSplitting(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(typeof(GameCard), nameof(GameCard.DestroyCard));
            tool.InsertBefore(patch);
            return tool;

            static void patch(Harvestable __instance)
            {
                if (__instance.MyGameCard.Parent?.CardData is Harvestable or CombatableHarvestable
                    && __instance.MyGameCard.Child?.CardData is BaseVillager or Worker)
                {
                    var parent = __instance.MyGameCard.Parent;
                    var child = __instance.MyGameCard.Child;
                    __instance.MyGameCard.RemoveFromStack();
                    child.SetParent(parent);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AllowStackingWhilstWorking(CardData card)
        {
            return card.MyGameCard.GetRootCard().CardData.Id is "lumbercamp" or "quarry" or "mine" or "gold_mine" or "well";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldStack(CardData building, CardData deposit)
        {
            // lumbercamp, quarry, mine, and gold_mine are CombatableHarvestable
            // sand_quarry catacombs cave forest jungle mountain old_tome old_village plains ruins
            // berrybush cotton_plant grape_vine sugar_cane tomato_plant

            return building.Id switch
            {
                "lumbercamp" => deposit.Id is "tree" or "apple_tree" or "olive_tree" or "banana_tree" or "driftwood",
                "quarry" => deposit.Id is "rock",
                "mine" => deposit.Id is "iron_deposit",
                "gold_mine" => deposit.Id is "gold_deposit",
                "well" => deposit.Id is "spring" or "berrybush" or "cotton_plant" or "grape_vine" or "sugar_cane" or "tomato_plant",
                _ => false
            };
        }

        private static void _Test1()
        {
            // readable copy from WorldManager.StackSend
            GameCard cardWithStatusInStack = null!;
            GameCard allCard = null!;
            GameCard initialParent = null!;
            GameCard myCard = null!;
            if ((cardWithStatusInStack == null || cardWithStatusInStack.CardData.CanHaveCardsWhileHasStatus())
                && allCard.GetCardInCombatInStack() == null
                && !allCard.BeingDragged
                && !allCard.IsChildOf(myCard)
                && !allCard.IsParentOf(myCard)
                && (initialParent == null || !allCard.IsChildOf(initialParent) && allCard != initialParent)
                && !allCard.HasChild
                && allCard.CardData.CanHaveCardOnTop(myCard.CardData)
                && allCard.CardData.Id == myCard.CardData.Id)
            {
                //gets closest allCard here
            }
        }

        private static void _Test2()
        {
            // idea abandoned
            //[HarmonyPatch(typeof(Harvestable), nameof(Harvestable.UpdateCard))]

            // get parent villagers as well
            //base.GetChildrenMatchingPredicate((CardData x) => x is BaseVillager || x is Worker, this.villagers);

            // allow HasCardOnBottom too; or just work on bottom card first?
            // if bottom card takes priority, adding any card will reset timer...
            // if card in the middle is destroyed, the stack will split...
            //if (this.villagers.Count >= this.RequiredVillagerCount && (base.HasCardOnTop<BaseVillager>() || base.HasCardOnTop<Worker>()) && flag)

            // make it so bottom cards have priority
            // check MyGameCard.GetCardWithStatusInStack() for running timer complete_harvest

            // apply buff to workers
            //num = list.Max((CardData v) => ((BaseVillager)v).GetActionTimeModifier(actionId, this));
        }
    }
}
