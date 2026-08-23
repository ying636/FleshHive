using RimWorld;
using Verse;

namespace FleshHive;

public class CompAbilityEffect_TitanDevastatingStrike : CompAbilityEffect
{
    public new CompProperties_AbilityTitanDevastatingStrike Props =>
        (CompProperties_AbilityTitanDevastatingStrike)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        Pawn caster = parent.pawn;
        if (!caster.health.hediffSet.HasHediff(Props.hediffDef))
        {
            caster.health.AddHediff(Props.hediffDef);
        }
    }
}
