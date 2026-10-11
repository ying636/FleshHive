using HarmonyLib;
using Verse;

namespace FleshHive.CE;

[HarmonyPatch(typeof(HediffSet), nameof(HediffSet.DirtyCache))]
internal static class Patch_ScarletFieldHediffs
{
    private static void Postfix(HediffSet __instance)
    {
        ScarletFieldMapIndex.Refresh(__instance.pawn);
    }
}
