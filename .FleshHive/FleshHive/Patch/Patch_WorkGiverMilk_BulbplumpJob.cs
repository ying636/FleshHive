using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

[HarmonyPatch(typeof(WorkGiver_GatherAnimalBodyResources), nameof(WorkGiver_GatherAnimalBodyResources.HasJobOnThing))]
public static class Patch_WorkGiverMilk_BulbplumpJob
{
    [HarmonyPostfix]
    public static void Postfix(WorkGiver_GatherAnimalBodyResources __instance, Pawn pawn, Thing t, bool forced, ref bool __result)
    {
        if (__result || __instance is not WorkGiver_Milk || t is not Pawn target
            || target.def.defName != "FH_Bulbplump" || target.Faction != pawn.Faction)
        {
            return;
        }
        __result = target.TryGetComp<CompMilkable>()?.ActiveAndFull == true
            && !target.Downed && (target.roping == null || !target.roping.IsRopedByPawn)
            && target.CanCasuallyInteractNow() && pawn.CanReserve(target, 1, -1, null, forced);
    }
}
