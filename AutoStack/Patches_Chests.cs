namespace AutoStackNS
{
    /// <summary>
    /// Makes ResourceChest always count as 1 card (like when playing on the cities board).
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ResourceChestLimit
    {
        public static MethodBase TargetMethod()
        {
            return typeof(WorldManager).GetMethod(nameof(WorldManager.GetCardCount), 0, BindingFlags.Instance | BindingFlags.Public, null, [], []);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(OpCodes.Ldstr, "cities");
            tool++; // should call string.op_Equality
            tool.InsertAfter(patch);
            return tool;

            static bool patch(bool __stack)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Resource Chest allow more card types.
    /// </summary>
    [HarmonyPatch(typeof(ResourceChest), nameof(ResourceChest.CanHaveCard))]
    public static class Patch_ResourceChestAllowed
    {
        public static bool Prefix(CardData otherCard, ResourceChest __instance, ref bool __result)
        {
            if (__instance.HeldCardId is not (null or "") && otherCard.Id != __instance.HeldCardId)
                __result = false;
            else if (otherCard.Id == "gold" || otherCard.Id == "shell" || otherCard is Dollar)
                __result = false;
            else if (otherCard is Food or Equipable)
                __result = true;
            else if (otherCard.MyCardType is CardType.Resources)
                __result = true;
            else
                __result = false;
            return false;
        }
    }

    /// <summary>
    /// Resources output instantly and limit to 2 units per type.
    /// </summary>
    [HarmonyPatch(typeof(ResourceChest), nameof(ResourceChest.UpdateCard))]
    public static class Patch_ResourceChestOutput
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(typeof(GameCard), nameof(GameCard.StartTimer));
            tool.ReplaceCall(patch);
            return tool;

            static void patch(GameCard instance, float time, TimerAction a, string status, string actionId, bool withStatusBar, bool skipWorkerEnergyCheck, bool skipDamageOnOffCheck, ResourceChest __instance)
            {
                // normal behavior for junctions
                var target_card = __instance.outputConnector.GetConnectedGameCard();
                if (target_card.CardData is Junction or FilteredJunction)
                {
                    instance.StartTimer(time, a, status, actionId, withStatusBar, skipWorkerEnergyCheck, skipDamageOnOffCheck);
                    return;
                }

                // wait for custom timer
                if (instance.TimerRunning && instance.TimerActionId is "wait_one_second")
                    return;

                // cancel any other timer
                instance.StopTimer();

                // stop if game is paused
                if (WorldManager.instance.TimeScale <= 0f)
                    return;

                // stop if target is working
                if (target_card.TimerRunning && target_card.TimerActionId is not "stop_energy")
                    return;

                // stop if target is satisfied
                if (target_card.GetStackCount() >= 10 || GetStackCardCount(target_card, __instance.HeldCardId) >= 2)
                    return;

                __instance.OutputCard();

                instance.StartTimer(1f, () => { }, "wait_one_second", "wait_one_second", withStatusBar: false);
            }
        }

        public static int GetStackCardCount(GameCard card, string id)
        {
            int counter = 0;

            var gameCard = card; // count itself
            while (gameCard != null)
            {
                if (gameCard.CardData.Id == id)
                    counter++;
                gameCard = gameCard.Parent;
            }

            gameCard = card.Child;
            while (gameCard != null)
            {
                if (gameCard.CardData.Id == id)
                    counter++;
                gameCard = gameCard.Child;
            }

            return counter;
        }
    }
}
