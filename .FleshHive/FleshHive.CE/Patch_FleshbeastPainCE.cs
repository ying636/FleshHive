using HarmonyLib;
using Verse;

namespace FleshHive.CE;

[HarmonyPatch(typeof(HediffSet), "CalculatePain")]
public static class Patch_FleshbeastPainCE
{
    [HarmonyPostfix]
    public static void Postfix(Pawn ___pawn, ref float __result)
    {
        if (___pawn.TryGetComp<CompFleshBeastCache>() != null)
        {
            __result = 0f;
        }
    }
}
