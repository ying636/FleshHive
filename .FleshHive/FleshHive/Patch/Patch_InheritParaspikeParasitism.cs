using HarmonyLib;
using RimWorld;
using Verse;

namespace FleshHive;

[HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
public static class Patch_InheritParaspikeParasitism
{
    public static void Postfix(bool __result, Pawn pawn, Thing motherOrEgg)
    {
        if (!__result || motherOrEgg is not Pawn mother || !HasParaspikeParasite(mother))
        {
            return;
        }

        AttachInheritedParaspike(pawn, mother);
    }

    private static void AttachInheritedParaspike(Pawn child, Pawn inheritingParent)
    {
        if (child.health?.hediffSet == null || HasParaspikeParasite(child))
        {
            return;
        }

        ParasitismSystem? system = child.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        bool addedSystem = system == null;
        if (system == null)
        {
            system = child.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        }

        if (system == null)
        {
            Log.Error($"[FleshHive] Failed to add parasitism system to newborn {child} while inheriting a paraspike.");
            return;
        }

        Pawn parasite = PawnGenerator.GeneratePawn(FleshHiveDefOf.FH_Paraspike, child.Faction ?? inheritingParent.Faction);
        if (!system.Parasite(parasite))
        {
            if (!parasite.Destroyed)
            {
                parasite.Destroy();
            }
            if (addedSystem && child.health.hediffSet.HasHediff(FleshHiveDefOf.FH_ParasitismSystem))
            {
                child.health.RemoveHediff(system);
            }
            Log.Error($"[FleshHive] Failed to attach inherited paraspike to newborn {child} from parent {inheritingParent}.");
            return;
        }

        foreach (ParasitismHediff parasitismHediff in system.ParasitismHediffs)
        {
            if (parasitismHediff.flesh == parasite)
            {
                parasitismHediff.allow = true;
                return;
            }
        }

        Log.Error($"[FleshHive] Inherited paraspike was attached to newborn {child}, but its parasitism hediff could not be found.");
    }

    private static bool HasParaspikeParasite(Pawn? parent)
    {
        if (parent?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) is not ParasitismSystem system)
        {
            return false;
        }

        foreach (ParasitismHediff parasitismHediff in system.ParasitismHediffs)
        {
            if (parasitismHediff.flesh?.kindDef == FleshHiveDefOf.FH_Paraspike)
            {
                return true;
            }
        }

        return false;
    }
}
