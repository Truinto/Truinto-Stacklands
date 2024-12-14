using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStackNS
{
    /// <summary>
    /// Press shift while equipping items to keep villager type (mage, archer, ...)
    /// </summary>
    [HarmonyPatch]
    public static class Patch_VillagerTypeOverride
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return [
                AccessTools.Method(typeof(BaseVillager), nameof(BaseVillager.OnEquipItem)),
                AccessTools.Method(typeof(BaseVillager), nameof(BaseVillager.OnUnequipItem)),
                ];
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var tool = new TranspilerTool(instructions, generator, original);
            tool.Seek(typeof(BaseVillager), nameof(BaseVillager.CanOverrideCardFromEquipment));
            tool.InsertAfter(patch);
            return tool;

            static bool patch(bool __stack)
            {
                if (InputController.instance.GetKey(UnityEngine.InputSystem.Key.LeftShift))
                    return false;
                return __stack;
            }
        }
    }
}
