using Shared;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

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
}
