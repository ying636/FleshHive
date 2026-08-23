using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AbilityEffect_TwistedFleshCost : CompProperties_AbilityEffect
{
    public CompProperties_AbilityEffect_TwistedFleshCost()
    {
        this.compClass = typeof(CompAbilityEffect_TwistedFleshCost);
    }

    public int twistedFleshCost;
}

public class CompAbilityEffect_TwistedFleshCost : CompAbilityEffect
{
    public new CompProperties_AbilityEffect_TwistedFleshCost Props => (CompProperties_AbilityEffect_TwistedFleshCost)this.props;

    public override bool CanCast => TwistedFleshUtility.CanConsumeTwistedFlesh(this.parent.pawn, Props.twistedFleshCost);

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        TwistedFleshUtility.ConsumeTwistedFlesh(this.parent.pawn, Props.twistedFleshCost);
    }

    public override bool AICanTargetNow(LocalTargetInfo target)
    {
        return TwistedFleshUtility.CanConsumeTwistedFlesh(this.parent.pawn, Props.twistedFleshCost);
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (!TwistedFleshUtility.CanConsumeTwistedFlesh(this.parent.pawn, Props.twistedFleshCost))
        {
            reason = "FH_NotEnoughTwistedFlesh".Translate(Props.twistedFleshCost);
            return true;
        }
        return base.GizmoDisabled(out reason);
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return TwistedFleshUtility.CanConsumeTwistedFlesh(this.parent.pawn, Props.twistedFleshCost)
               && base.CanApplyOn(target, dest);
    }
}
