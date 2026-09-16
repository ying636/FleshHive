using HarmonyLib;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.ShouldBeDeadFromLethalDamageThreshold))]
public static class Patch_Pawn_HealthTracker_LethalDamageThreshold_Hela
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn_HealthTracker __instance, ref bool __result)
    {
        if (__instance.hediffSet.HasHediff(FleshHiveDefOf.FH_Hela))
        {
            __result = false;
            return false;
        }

        return true;
    }
}
