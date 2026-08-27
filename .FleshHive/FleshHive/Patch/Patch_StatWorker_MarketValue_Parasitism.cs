using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(StatWorker_MarketValue), nameof(StatWorker_MarketValue.GetValueUnfinalized))]
public static class Patch_StatWorker_MarketValue_Parasitism
{
    public static void Postfix(StatRequest req, ref float __result)
    {
        if (req.Thing is not Pawn pawn || pawn.health?.hediffSet == null)
        {
            return;
        }

        if (pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is not ParasitismSystem system)
        {
            return;
        }

        foreach (ParasitismHediff hediff in system.ParasitismHediffs)
        {
            if (hediff.flesh == null)
            {
                continue;
            }

            switch (FleshBeastKindUtility.SizeOf(hediff.flesh.kindDef))
            {
                case FleshBeastSize.Small:
                    __result += 120f;
                    break;
                case FleshBeastSize.Medium:
                    __result += 300f;
                    break;
                case FleshBeastSize.Large:
                    __result += 800f;
                    break;
                case FleshBeastSize.Giant:
                    __result += 1800f;
                    break;
            }
        }
    }
}
