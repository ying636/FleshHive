using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_FissionmeldUndying : HediffCompProperties
{
    public HediffCompProperties_FissionmeldUndying()
    {
        this.compClass = typeof(HediffComp_FissionmeldUndying);
    }

    public HediffDef? undyingHediff;

    public int requiredTwistedFlesh = 200;

    public int downedConsumeIntervalTicks = 60;
}

public class HediffComp_FissionmeldUndying : HediffComp
{
    public new HediffCompProperties_FissionmeldUndying Props => (HediffCompProperties_FissionmeldUndying)this.props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (this.Pawn.IsHashIntervalTick(Props.downedConsumeIntervalTicks))
        {
            MaintainUndyingHediff();
            ConsumeWhileDowned();
        }
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        MaintainUndyingHediff();
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        RemoveUndyingHediff();
    }

    private void MaintainUndyingHediff()
    {
        if (Props.undyingHediff == null)
        {
            return;
        }

        Hediff? hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.undyingHediff);
        if (TwistedFleshUtility.GetCurrentTwistedFlesh(this.Pawn) >= Props.requiredTwistedFlesh)
        {
            if (hediff == null)
            {
                this.Pawn.health.AddHediff(Props.undyingHediff);
            }
        }
        else if (hediff != null)
        {
            this.Pawn.health.RemoveHediff(hediff);
        }
    }

    private void ConsumeWhileDowned()
    {
        if (!this.Pawn.Downed || Props.undyingHediff == null || !this.Pawn.health.hediffSet.HasHediff(Props.undyingHediff))
        {
            return;
        }

        TwistedFleshUtility.ConsumeTwistedFlesh(this.Pawn, 1);
        MaintainUndyingHediff();
    }

    private void RemoveUndyingHediff()
    {
        if (Props.undyingHediff == null || this.Pawn?.health?.hediffSet == null)
        {
            return;
        }

        Hediff? hediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.undyingHediff);
        if (hediff != null)
        {
            this.Pawn.health.RemoveHediff(hediff);
        }
    }
}
