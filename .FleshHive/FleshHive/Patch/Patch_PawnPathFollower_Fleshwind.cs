using HarmonyLib;
using Verse;
using Verse.AI;

namespace FleshHive;

[HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.GetPawnCellBaseCostOverride))]
public static class Patch_PawnPathFollower_Fleshwind
{
    public static void Postfix(Pawn pawn, IntVec3 c, ref int? __result)
    {
        if (pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is ParasitismSystem system
            && system.HasFleshwind)
        {
            __result = 0;
        }
    }
}
