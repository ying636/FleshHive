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
        if (caster.TryGetComp<CompScarletField>() is CompScarletField thingComp)
        {
            return thingComp.Active || thingComp.CanActivate;
        }
        if (HediffComp_ScarletField.FindOnPawn(caster) is HediffComp_ScarletField hediffComp)
        {
            return hediffComp.Active || hediffComp.CanActivate;
        }
        return false;
    }
}
