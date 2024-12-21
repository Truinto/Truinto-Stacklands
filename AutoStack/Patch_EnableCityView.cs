namespace AutoStackNS
{
    /// <summary>
    /// Enable all city views, if road builder is crafted (normally only roads).
    /// </summary>
    [HarmonyPatch]
    public static class Patch_EnableCityView
    {
        /// <summary>
        /// Draw connectors on non-city boards.
        /// </summary>
        [HarmonyPatch(typeof(CardConnector), nameof(CardConnector.Update))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);

            tool.Seek(typeof(CardConnector), nameof(CardConnector.ConnectionType));
            tool.InsertAfter(patch);
            tool.Seek(typeof(CardConnector), nameof(CardConnector.ConnectionType));
            tool.InsertAfter(patch);

            return tool;

            static ConnectionType patch(ConnectionType __stack, CardConnector __instance)
            {
                if (__instance.Parent.CardData.Id is not "time_machine")
                    return ConnectionType.Transport;
                return __stack;
            }
        }

        /// <summary>
        /// Make screen buttons show up.
        /// </summary>
        [HarmonyPatch(typeof(GameScreen), nameof(GameScreen.Update))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            while (!tool.IsLast)
            {
                if (tool.Calls(typeof(GameScreen), nameof(GameScreen.EnergyViewButton)) || tool.Calls(typeof(GameScreen), nameof(GameScreen.SewageViewButton)))
                {
                    tool.Offset(2);
                    if (tool.IsLoadConstant(0))
                        tool.Current.opcode = OpCodes.Ldc_I4_1;
                }
                tool++;
            }
            return tool;
        }
    }
}
