using RimWorld;
using Verse;

namespace FleshHive;

public class HediffCompProperties_TwistedFleshProducer : HediffCompProperties
{
    public HediffCompProperties_TwistedFleshProducer()
    {
        this.compClass = typeof(HediffComp_TwistedFleshProducer);
    }

    public int twistedFleshPerDay = 200;
}

public class HediffComp_TwistedFleshProducer : HediffComp
{
    public new HediffCompProperties_TwistedFleshProducer Props => (HediffCompProperties_TwistedFleshProducer)this.props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        Pawn pawn = this.Pawn;
        if (pawn == null || !pawn.Spawned)
        {
            return;
        }
        twistedFleshAccumulator += Props.twistedFleshPerDay / 60000f;
        if (twistedFleshAccumulator >= 1f)
        {
            twistedFleshAccumulator -= 1f;
            ParasitismSystem system = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
            if (system != null)
            {
                system.FillTwistedFlesh(1);
            }
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref twistedFleshAccumulator, "twistedFleshAccumulator");
    }

    private float twistedFleshAccumulator;
}
