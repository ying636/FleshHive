using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnComponentsUtility))]
public static class Patch_PawnComponents_Parasitism
{
    [HarmonyPatch("AddComponentsForSpawn")]
    [HarmonyPostfix]
    public static void AddComponentsForSpawn_Postfix(Pawn pawn)
    {
        if (pawn.abilities == null && pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_ParasitismSystem) == true)
        {
            pawn.abilities = new Pawn_AbilityTracker(pawn);
        }
    }
}
