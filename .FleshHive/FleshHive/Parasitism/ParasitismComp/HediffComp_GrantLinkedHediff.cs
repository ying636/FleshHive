using Verse;

namespace FleshHive;

public class HediffCompProperties_GrantLinkedHediff : HediffCompProperties
{
    public HediffCompProperties_GrantLinkedHediff()
    {
        this.compClass = typeof(HediffComp_GrantLinkedHediff);
    }

    public HediffDef hediff;
    public BodyPartDef bodyPart;
}

public class HediffComp_GrantLinkedHediff : HediffComp
{
    public HediffCompProperties_GrantLinkedHediff Props => (HediffCompProperties_GrantLinkedHediff)this.props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (this.Props.hediff == null || this.Pawn.health.hediffSet.HasHediff(this.Props.hediff))
        {
            return;
        }

        BodyPartRecord part = null;
        if (this.Props.bodyPart != null)
        {
            part = this.Pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(record => record.def == this.Props.bodyPart);
            if (part == null)
            {
                return;
            }
        }

        this.Pawn.health.AddHediff(this.Props.hediff, part);
        grantedByThisComp = true;
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        HediffComp_GrantLinkedHediff otherComp = GetOtherGrantComp();
        if (otherComp != null)
        {
            if (grantedByThisComp)
            {
                otherComp.grantedByThisComp = true;
            }
            return;
        }

        if (!grantedByThisComp || this.Props.hediff == null)
        {
            return;
        }

        Hediff grantedHediff = this.Pawn.health.hediffSet.GetFirstHediffOfDef(this.Props.hediff);
        if (grantedHediff != null)
        {
            this.Pawn.health.RemoveHediff(grantedHediff);
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref grantedByThisComp, "grantedByThisComp");
    }

    private HediffComp_GrantLinkedHediff GetOtherGrantComp()
    {
        foreach (Hediff hediff in this.Pawn.health.hediffSet.hediffs)
        {
            if (hediff == this.parent)
            {
                continue;
            }

            HediffComp_GrantLinkedHediff comp = hediff.TryGetComp<HediffComp_GrantLinkedHediff>();
            if (comp?.Props.hediff == this.Props.hediff)
            {
                return comp;
            }
        }
        return null;
    }

    private bool grantedByThisComp;
}
