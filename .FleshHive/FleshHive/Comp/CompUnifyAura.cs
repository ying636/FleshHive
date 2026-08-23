using System.Collections.Generic;
using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompProperties_FH_UnifyAura : CompProperties
{
    public CompProperties_FH_UnifyAura()
    {
        this.compClass = typeof(CompUnifyAura);
    }
}

public class CompUnifyAura : ThingComp
{
    private Pawn PawnOwner => this.parent as Pawn;

    private List<Pawn> CachedAffectedPawns => cachedAffectedPawns ??= new List<Pawn>();

    public override void CompTick()
    {
        base.CompTick();
        if (!this.parent.Spawned)
        {
            return;
        }
        tickCounter++;
        if (tickCounter < RefreshInterval)
        {
            return;
        }
        tickCounter = 0;
        RefreshAura();
    }

    private void RefreshAura()
    {
        Pawn caster = PawnOwner;
        if (caster == null || !caster.Spawned)
        {
            ClearAll();
            return;
        }

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
            ApplyUnification(pawn);
        }

        foreach (Pawn pawn in CachedAffectedPawns)
        {
            if (pawn != null && pawn.Spawned && !newAffected.Contains(pawn))
            {
                RemoveUnification(pawn);
            }
        }

        CachedAffectedPawns.Clear();
        CachedAffectedPawns.AddRange(newAffected);
    }

    private void ApplyUnification(Pawn pawn)
    {
        Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Unification);
        if (hediff == null)
        {
            pawn.health.AddHediff(FleshHiveDefOf.FH_Unification);
        }
    }

    private void RemoveUnification(Pawn pawn)
    {
        Hediff hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Unification);
        if (hediff != null)
        {
            pawn.health.RemoveHediff(hediff);
        }
    }

    private void ClearAll()
    {
        foreach (Pawn pawn in CachedAffectedPawns)
        {
            if (pawn != null && pawn.Spawned)
            {
                RemoveUnification(pawn);
            }
        }
        CachedAffectedPawns.Clear();
    }

    private const int RefreshInterval = 60;

    private int tickCounter;

    private List<Pawn> cachedAffectedPawns;
}
