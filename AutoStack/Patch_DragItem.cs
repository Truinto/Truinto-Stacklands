using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace AutoStackNS
{
    [HarmonyPatch(typeof(GameCard), nameof(GameCard.Clicked))]
    public static class Patch_ClickItem
    {
        public static bool Prefix(GameCard __instance)
        {
            AutoStack.Log($"clicked: {__instance.CardData.Id}");
            return true;
        }
    }

    /// <summary>
    /// - drag resources out of chests<br/>
    /// </summary>
    [HarmonyPatch(typeof(GameCard), nameof(GameCard.StartDragging))]
    public static class Patch_DragItem
    {
        public static int DragCount = 10;
        public static bool Prefix(GameCard __instance)
        {
            AutoStack.Log($"dragging: {__instance.CardData.Id}");

            if (__instance.CardData is ResourceChest chest && chest.ResourceCount > 0)
            {
                int count;
                if (InputController.instance.GetKey(Key.LeftCtrl))
                    count = 1;
                else if (InputController.instance.GetKey(Key.LeftShift))
                    count = DragCount;
                else
                    return true;

                var topcard = chest.RemoveResources(count);
                WorldManager.instance.DraggingDraggable = topcard;
                topcard.StartDragging();
                return false;
            }
            return true;
        }
    }
}
