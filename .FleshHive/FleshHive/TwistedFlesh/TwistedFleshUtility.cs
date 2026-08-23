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
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return Mathf.FloorToInt(comp.CurrentTwistedFlesh);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return system.CurrentTwistedFlesh;
        }
        return 0;
    }

    public static int GetMaxTwistedFlesh(Pawn pawn)
    {
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return comp.MaxTwistedFlesh;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return system.MaxTwistedFlesh;
        }
        return 0;
    }

    public static bool CanConsumeTwistedFlesh(Pawn pawn, int amount)
    {
        if (HasInfiniteTwistedFlesh(pawn))
        {
            return true;
        }
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return comp.CanConsumeTwistedFlesh(amount);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return system.CanConsumeTwistedFlesh(amount);
        }
        return false;
    }

    public static bool ConsumeTwistedFlesh(Pawn pawn, int amount)
    {
        if (HasInfiniteTwistedFlesh(pawn))
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

    public static void FillTwistedFlesh(Pawn pawn, int amount)
    {
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            comp.FillTwistedFlesh(amount);
            return;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            system.FillTwistedFlesh(amount);
        }
    }

    public static int GetNeededAmount(Pawn pawn)
    {
        int targetAmount = GetTargetAmount(pawn);
        int currentAmount = GetCurrentTwistedFlesh(pawn);
        return targetAmount > currentAmount ? targetAmount - currentAmount : 0;
    }

    public static bool NeedsRefill(Pawn pawn, bool forced = false)
    {
        return (forced || GetAllowedToAutoRefill(pawn)) && GetNeededAmount(pawn) > 0;
    }

    public static bool HasTwistedFleshStorage(Pawn pawn)
    {
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return true;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        return system != null && system.MaxTwistedFlesh > 0;
    }

    private static int GetTargetAmount(Pawn pawn)
    {
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return Mathf.RoundToInt(comp.MaxTwistedFlesh * comp.TwistedFleshTargetValue);
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system != null)
        {
            return Mathf.RoundToInt(system.MaxTwistedFlesh * system.TwistedFleshTargetValue);
        }
        return 0;
    }

    private static bool GetAllowedToAutoRefill(Pawn pawn)
    {
        CompTwistedFlesh comp = pawn.TryGetComp<CompTwistedFlesh>();
        if (comp != null && comp.MaxTwistedFlesh > 0)
        {
            return comp.AllowAutoRefillTwistedFlesh;
        }
        ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        return system?.AllowAutoRefillTwistedFlesh == true;
    }

    private static bool HasInfiniteTwistedFlesh(Pawn pawn)
    {
        return pawn?.kindDef == FleshHiveDefOf.FH_Fissionmeld && pawn.Faction != null && !pawn.Faction.IsPlayer;
    }
}
