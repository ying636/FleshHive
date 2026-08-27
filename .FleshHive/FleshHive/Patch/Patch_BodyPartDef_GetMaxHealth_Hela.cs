using HarmonyLib;
using UnityEngine;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(BodyPartDef), nameof(BodyPartDef.GetMaxHealth))]
public static class Patch_BodyPartDef_GetMaxHealth_Hela
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref float __result)
    {
        Hediff_Hela? hela = Hediff_Hela.GetCached(pawn);
        if (hela != null)
        {
            __result = Mathf.CeilToInt(__result * hela.BodyPartHealthFactor);
        }

        if (pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_MeldGrowth)
            is Hediff_MeldGrowth growth)
        {
            __result = Mathf.CeilToInt(__result * (1f + growth.Level * 0.25f));
        }

        if (pawn.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hediff_Upgrade_Robust) == true)
        {
            __result = Mathf.CeilToInt(__result * 6f);
        }
    }
}
