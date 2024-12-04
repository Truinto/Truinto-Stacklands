namespace AutoStackNS
{
    /// <summary>
    /// Because <see cref="Hotpot.MaxFoodValue"/> is a private field, it won't keep the prefab.<br/>
    /// Instead we patch the constructor.
    /// </summary>
    [HarmonyPatch(typeof(Hotpot), MethodType.Constructor)]
    public static class Patch_Hotpot
    {
        public static void Postfix(Hotpot __instance)
        {
            __instance.MaxFoodValue = AutoStack.Instance.Config.GetValue<int>("hotpot_capacity");
        }
    }
}
