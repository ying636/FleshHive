using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_Regeneration : HediffCompProperties
{
    public HediffCompProperties_Regeneration()
    {
        this.compClass = typeof(HediffComp_Regeneration);
    }

    public HediffDef regenHediff;

    public float regenSeverity = 0.5f;
}

public class HediffComp_Regeneration : HediffComp
{
    public new HediffCompProperties_Regeneration Props => (HediffCompProperties_Regeneration)this.props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (Props.regenHediff != null && !this.Pawn.health.hediffSet.HasHediff(Props.regenHediff))
        {
            this.Pawn.health.AddHediff(Props.regenHediff).Severity = Props.regenSeverity;
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        if (Props.regenHediff != null)
        {
            Hediff old = this.Pawn.health?.hediffSet?.GetFirstHediffOfDef(Props.regenHediff);
            if (old != null)
            {
                this.Pawn.health.RemoveHediff(old);
            }
        }
    }
}
