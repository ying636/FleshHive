using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityScarletField : CompProperties_AbilityEffect
{
    public CompProperties_AbilityScarletField()
    {
        this.compClass = typeof(CompAbilityEffect_FH_ScarletField);
    }
}

public class CompAbilityEffect_FH_ScarletField : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Pawn caster = this.parent.pawn;

        CompScarletField thingComp = caster.TryGetComp<CompScarletField>();
        if (thingComp != null)
        {
            thingComp.Activate();
            return;
        }
        HediffComp_ScarletField hediffComp = HediffComp_ScarletField.FindOnPawn(caster);
        if (hediffComp != null)
        {
            hediffComp.Activate();
            return;
        }
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        Pawn caster = this.parent.pawn;
        if (caster == null || !caster.Spawned)
        {
            return false;
        }
        if (caster.TryGetComp<CompScarletField>() != null)
        {
            return true;
        }
        if (HediffComp_ScarletField.FindOnPawn(caster) != null)
        {
            return true;
        }
        return false;
    }
}
