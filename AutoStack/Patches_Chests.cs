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

            static bool patch(bool stack)
            {
                return true;
            }
        }
    }

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
}
