using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityFleshRebirth : CompProperties_AbilityEffect
{
    public CompProperties_AbilityFleshRebirth()
    {
        this.compClass = typeof(CompAbilityEffect_FleshRebirth);
    }
}

public class CompAbilityEffect_FleshRebirth : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;
        caster.health.Notify_Resurrected();
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return HasCurableHealthProblem(this.parent.pawn);
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return HasCurableHealthProblem(this.parent.pawn)
               && base.CanApplyOn(target, dest);
    }

    private static bool HasCurableHealthProblem(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null)
        {
            return false;
        }

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff.def.forceRemoveOnResurrection
                || hediff is Hediff_MissingPart { Part: not null } missingPart && !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(missingPart.Part)
                || hediff.def.everCurableByItem && hediff.TryGetComp<HediffComp_Immunizable>() != null
                || hediff.def.everCurableByItem && (hediff.IsLethal || hediff.IsAnyStageLifeThreatening())
                || hediff.def.everCurableByItem && hediff is Hediff_Injury && !hediff.IsPermanent())
            {
                return true;
            }
        }

        return false;
    }
}
