using Shared;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoStackNS
{
    /// <summary>
    /// Selling with hotkey sends coins to stacks.
    /// </summary>
    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.Update))]
    public static class Patch_Hotkeys
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
                if (stack?.CardData is Dollar dollar)
                {
                    stack.RemoveFromParent();
                    WorldManager.instance.StackSend(stack, Vector3.right);
                }
                else if (stack?.GetRootCard() is GameCard root)
                {
                    WorldManager.instance.StackSend(root, Vector3.right);
                }

                return stack;
            }
        }

        public static void Postfix(WorldManager __instance)
        {
            if (InputController.instance.GetKeyDown(Key.X))
            {
                if (__instance.HoveredCard?.GetRootCard() is GameCard myCard
                    && myCard.CanBeDragged()
                    && myCard.BounceTarget == null)
                {
                    __instance.StackSend(myCard, Vector3.right);
                }
            }
        }
    }
}
