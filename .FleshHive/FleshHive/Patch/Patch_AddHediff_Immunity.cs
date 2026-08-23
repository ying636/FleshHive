using FleshHive.Effect;
using HarmonyLib;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect))]
public static class Patch_AddHediff_Immunity
{
    public static bool Prefix(HediffSet __instance, Hediff hediff)
    {
        Pawn pawn = __instance?.pawn;
        if (pawn == null || hediff?.def == null)
        {
            return true;
        }

        if (hediff.def.isBad && pawn.TryGetComp<CompFleshtitanInvulnerability>() != null)
        {
            return false;
        }

        if (pawn.health?.hediffSet?.hediffs is not { } hediffs)
        {
            return true;
        }

        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is HediffWithComps withComps && withComps.TryGetComp<HediffComp_Immunity>() is { } immunity)
            {
                if (immunity.Prop.hds.Contains(hediff.def))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
