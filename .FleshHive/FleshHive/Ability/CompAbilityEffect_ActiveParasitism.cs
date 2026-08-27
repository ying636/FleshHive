using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityActiveParasitism : CompProperties_AbilityEffect
{
    public CompProperties_AbilityActiveParasitism()
    {
        this.compClass = typeof(CompAbilityEffect_ActiveParasitism);
    }
}

public class CompAbilityEffect_ActiveParasitism : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        Pawn host = target.Pawn;
        if (!CanParasite(host, out string reason))
        {
            Messages.Message(reason, caster, MessageTypeDefOf.RejectInput, false);
            return;
        }

        ParasitismSystem? system = host.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            system = host.health.AddHediff(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        }
        if (system == null)
        {
            Log.Error($"[FleshHive] Active parasitism failed because {host} could not receive FH_ParasitismSystem.");
            Messages.Message("FH_ActiveParasitism_Failed".Translate(), host, MessageTypeDefOf.RejectInput, false);
            return;
        }
        if (!system.Parasite(caster))
        {
            Log.Error($"[FleshHive] Active parasitism failed after validation. Caster: {caster}; host: {host}.");
            Messages.Message("FH_ActiveParasitism_Failed".Translate(), host, MessageTypeDefOf.RejectInput, false);
        }
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return CanParasite(target.Pawn, out _)
               && base.CanApplyOn(target, dest);
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (!base.Valid(target, throwMessages))
        {
            return false;
        }
        if (CanParasite(target.Pawn, out string reason))
        {
            return true;
        }
        if (throwMessages)
        {
            Messages.Message(reason, this.parent.pawn, MessageTypeDefOf.RejectInput, false);
        }
        return false;
    }

    private bool CanParasite(Pawn host, out string reason)
    {
        Pawn caster = this.parent.pawn;
        if (host == null
            || host == caster
            || !host.Spawned
            || host.Dead
            || host.Faction != Faction.OfPlayer
            || host.MentalStateDef != null
            || !host.RaceProps.IsFlesh
            || host.health?.hediffSet == null)
        {
            reason = "FH_ActiveParasitism_InvalidTarget".Translate();
            return false;
        }

        ParasitismComp? comp = caster.TryGetComp<ParasitismComp>();
        if (comp?.Props.hediff == null)
        {
            reason = "FH_ActiveParasitism_Failed".Translate();
            return false;
        }

        ParasitismSystem? system = host.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        int usedCapacity = system?.Count ?? 0;
        int capacity = system?.Limit ?? Mathf.FloorToInt(host.GetStatValue(FleshHiveDefOf.FH_Stat_ParasitismCapacity));
        if (system?.ParasitismHediffs.Count >= 14 || capacity - usedCapacity < comp.Props.cost)
        {
            reason = "FleshParasitePod_InsufficientCapacity".Translate();
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
