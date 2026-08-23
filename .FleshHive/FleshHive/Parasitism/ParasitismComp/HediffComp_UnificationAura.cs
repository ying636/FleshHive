using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_UnificationAura : HediffCompProperties
{
    public HediffCompProperties_UnificationAura()
    {
        this.compClass = typeof(HediffComp_UnificationAura);
    }
}

public class HediffComp_UnificationAura : HediffComp
{
    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        Pawn pawn = this.Pawn;
        if (pawn == null || !pawn.Spawned)
        {
            return;
        }
        tickCounter++;
        if (tickCounter >= 60)
        {
            tickCounter = 0;
            RefreshAura(pawn);
        }
    }

    private void RefreshAura(Pawn caster)
    {
        Map map = caster.MapHeld;
        if (map == null)
        {
            ClearAll();
            return;
        }
        MapComponent_FleshHive mapComp = map.GetComponent<MapComponent_FleshHive>();
        if (mapComp == null)
        {
            ClearAll();
            return;
        }
        HashSet<Pawn> newAffected = new HashSet<Pawn>();
        foreach (Pawn pawn in mapComp.CachedFleshBeasts)
        {
            if (pawn == caster)
            {
                continue;
            }
            if (pawn.Faction != caster.Faction)
            {
                continue;
            }
            if (!pawn.Spawned || pawn.Dead)
            {
                continue;
            }
            newAffected.Add(pawn);
            if (!pawn.health.hediffSet.HasHediff(FleshHiveDefOf.FH_Unification))
            {
                pawn.health.AddHediff(FleshHiveDefOf.FH_Unification);
            }
        }
        foreach (Pawn pawn in cachedAffectedPawns)
        {
            if (pawn != null && pawn.Spawned && !newAffected.Contains(pawn))
            {
                Hediff old = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Unification);
                if (old != null)
                {
                    pawn.health.RemoveHediff(old);
                }
            }
        }
        cachedAffectedPawns = new List<Pawn>(newAffected);
    }

    private void ClearAll()
    {
        foreach (Pawn pawn in cachedAffectedPawns)
        {
            if (pawn != null && pawn.Spawned)
            {
                Hediff old = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Unification);
                if (old != null)
                {
                    pawn.health.RemoveHediff(old);
                }
            }
        }
        cachedAffectedPawns.Clear();
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        ClearAll();
    }

    private int tickCounter;
    private List<Pawn> cachedAffectedPawns = new List<Pawn>();
}
