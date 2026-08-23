using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

[HarmonyPatch(typeof(Lord), nameof(Lord.AddPawn))]
public static class Patch_Lord_AddPawn_Parasitism
{
    public static void Postfix(Lord __instance, Pawn p)
    {
        CacheLord(__instance, p);
    }

    public static void CacheLord(Lord lord, Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.GetFirstHediff<ParasitismSystem>() is not { } system)
        {
            return;
        }
        foreach (ParasitismHediff hediff in system.ParasitismHediffs)
        {
            if (hediff.lord == null)
            {
                hediff.lord = lord;
            }
        }
    }
}

[HarmonyPatch(typeof(Lord), nameof(Lord.AddPawns))]
public static class Patch_Lord_AddPawns_Parasitism
{
    public static void Postfix(Lord __instance, IEnumerable<Pawn> pawns)
    {
        foreach (Pawn pawn in pawns)
        {
            Patch_Lord_AddPawn_Parasitism.CacheLord(__instance, pawn);
        }
    }
}
