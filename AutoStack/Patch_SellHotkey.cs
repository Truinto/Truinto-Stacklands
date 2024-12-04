using Shared;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AutoStackNS
{
    /// <summary>
    /// Selling with hotkey sends coins to stacks.
    /// </summary>
    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.Update))]
    public static class Patch_SellHotkey
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(typeof(WorldManager), nameof(WorldManager.SellCard));
            tool--;
            tool.Set(OpCodes.Ldc_I4_0); // change parameter checkAddToStack from true to false
            tool++;
            tool.InsertAfter(patch);
            return tool;

            static GameCard? patch(GameCard? stack)
            {
                if (stack?.GetRootCard() is GameCard root)
                    WorldManager.instance.StackSendCheckTarget(root, root, Vector3.right);
                return stack;
            }
        }
    }
}
