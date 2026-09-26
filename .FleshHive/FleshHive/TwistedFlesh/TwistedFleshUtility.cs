using UnityEngine;
using Verse;

namespace FleshHive;

public static class TwistedFleshUtility
{
    public static int GetCurrentTwistedFlesh(Pawn pawn)
    {
        if (HasInfiniteTwistedFlesh(pawn))
        {
            return 999999;
        }
        int storedInPack = CompSynapsePack.GetWorn(pawn)?.CurrentTwistedFlesh ?? 0;
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return storedInPack + Mathf.FloorToInt(comp.CurrentTwistedFlesh);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return storedInPack + system.CurrentTwistedFlesh;
        }
        return storedInPack;
    }

    public static int GetMaxTwistedFlesh(Pawn pawn)
    {
        int packCapacity = CompSynapsePack.GetWorn(pawn)?.MaxTwistedFlesh ?? 0;
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return packCapacity + comp.MaxTwistedFlesh;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return packCapacity + system.MaxTwistedFlesh;
        }
        return packCapacity;
    }

    public static bool CanConsumeTwistedFlesh(Pawn pawn, int amount)
    {
        if (HasInfiniteTwistedFlesh(pawn))
        {
            return true;
        }
        return amount >= 0 && GetCurrentTwistedFlesh(pawn) >= amount;
    }

    public static bool ConsumeTwistedFlesh(Pawn pawn, int amount)
    {
        if (HasInfiniteTwistedFlesh(pawn))
        {
            return true;
        }
        if (!CanConsumeTwistedFlesh(pawn, amount))
        {
            return false;
        }
        CompSynapsePack? pack = CompSynapsePack.GetWorn(pawn);
        int fromPack = Mathf.Min(amount, pack?.CurrentTwistedFlesh ?? 0);
        if (pack != null && fromPack > 0)
        {
            pack.ConsumeTwistedFlesh(fromPack);
            amount -= fromPack;
        }
        if (amount == 0)
        {
            return true;
        }
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return comp.ConsumeTwistedFlesh(amount);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return system.ConsumeTwistedFlesh(amount);
        }
        return false;
    }

    public static void FillTwistedFlesh(Pawn pawn, int amount, bool forced = false)
    {
        CompSynapsePack? pack = CompSynapsePack.GetWorn(pawn);
        if (pack != null && (forced || pack.AllowAutoRefillTwistedFlesh))
        {
            amount -= pack.FillTwistedFlesh(Mathf.Min(amount, pack.NeededAmount));
        }
        if (amount <= 0)
        {
            return;
        }
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            if (forced || comp.AllowAutoRefillTwistedFlesh)
            {
                comp.FillTwistedFlesh(amount);
            }
            return;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null && (forced || system.AllowAutoRefillTwistedFlesh))
        {
            system.FillTwistedFlesh(amount);
        }
    }

    public static int GetNeededAmount(Pawn pawn, bool forced = false)
    {
        CompSynapsePack? pack = CompSynapsePack.GetWorn(pawn);
        int needed = pack != null && (forced || pack.AllowAutoRefillTwistedFlesh) ? pack.NeededAmount : 0;
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return needed + (forced || comp.AllowAutoRefillTwistedFlesh
                ? Mathf.Max(0, Mathf.RoundToInt(comp.MaxTwistedFlesh * comp.TwistedFleshTargetValue)
                    - Mathf.FloorToInt(comp.CurrentTwistedFlesh))
                : 0);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        return needed + (system != null && (forced || system.AllowAutoRefillTwistedFlesh)
            ? Mathf.Max(0, Mathf.RoundToInt(system.MaxTwistedFlesh * system.TwistedFleshTargetValue)
                - system.CurrentTwistedFlesh)
            : 0);
    }

    public static bool NeedsRefill(Pawn pawn, bool forced = false)
    {
        return GetNeededAmount(pawn, forced) > 0;
    }

    public static bool HasTwistedFleshStorage(Pawn pawn)
    {
        if (CompSynapsePack.GetWorn(pawn)?.MaxTwistedFlesh > 0)
        {
            return true;
        }
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return true;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        return system != null && system.MaxTwistedFlesh > 0;
    }

    private static bool HasInfiniteTwistedFlesh(Pawn pawn)
    {
        return pawn?.kindDef == FleshHiveDefOf.FH_Fissionmeld && pawn.Faction != null && !pawn.Faction.IsPlayer;
    }
}
