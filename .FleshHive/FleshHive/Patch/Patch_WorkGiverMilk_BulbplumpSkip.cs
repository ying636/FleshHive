using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(WorkGiver_GatherAnimalBodyResources), nameof(WorkGiver_GatherAnimalBodyResources.ShouldSkip))]
public static class Patch_WorkGiverMilk_BulbplumpSkip
{
    [HarmonyPostfix]
    public static void Postfix(WorkGiver_GatherAnimalBodyResources __instance, Pawn pawn, ref bool __result)
    {
        if (!__result || __instance is not WorkGiver_Milk)
        {
            return;
        }
        foreach (Pawn target in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
        {
            if (target.def.defName == "FH_Bulbplump" && target.TryGetComp<CompMilkable>()?.ActiveAndFull == true)
            {
                __result = false;
                return;
            }
        }
    }
}
