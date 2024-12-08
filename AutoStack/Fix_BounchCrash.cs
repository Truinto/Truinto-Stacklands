namespace AutoStackNS
{
    /// <summary>
    /// Prevent crash when two cards try to bounce onto each other.
    /// </summary>
    [HarmonyPatch]
    public static class Fix_BounchCrash
    {
        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.StackSend))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            var target = tool.GetLocal(typeof(GameCard));
            tool.Seek(target, loadIsTrue_storeIsFalse: false);  //GameCard gameCard = null;
            tool.Seek(target, loadIsTrue_storeIsFalse: false);  //gameCard = allCard;
            tool.Rewind(f => f.IsBranch()); //if (vector.magnitude <= 2f && vector.magnitude <= num)
            var label = tool.GetTargetLabel();
            tool.InsertJump(patch, label, before: false);

            return tool;

            static bool patch([LocalParameter(type: typeof(GameCard), indexByType: 1)] GameCard allCard)
            {
                if (allCard.GetRootCard().BounceTarget != null)
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.StackSendTo))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        public static bool Prefix2(GameCard myCard, GameCard target)
        {
            if (target.GetRootCard().BounceTarget == myCard)
                return false;
            return true;
        }
    }
}
