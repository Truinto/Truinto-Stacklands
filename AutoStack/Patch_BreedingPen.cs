using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStackNS
{
    /// <summary>
    /// Animals stay in breeding pen.
    /// </summary>
    [HarmonyPatch(typeof(BreedingPen), nameof(BreedingPen.BreedAnimals))]
    public static class Patch_BreedingPen
    {
        public static bool Prefix(BreedingPen __instance)
        {
            var cardData = WorldManager.instance.CreateCard(__instance.transform.position, __instance.MyGameCard.Child.CardData.Id, faceUp: true, checkAddToStack: false, playSound: true);
            WorldManager.instance.StackSendCheckTarget(__instance.MyGameCard, cardData.MyGameCard, __instance.OutputDir);
            QuestManager.instance.SpecialActionComplete("breed_" + cardData.Id);
            return false;
        }
    }
}
