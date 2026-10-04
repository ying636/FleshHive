using Verse;

namespace FleshHive;

public class ParasitismCompProperties : CompProperties
{
    public ParasitismCompProperties()
    {
        this.compClass = typeof(ParasitismComp);
    }

    public int cost = 1;
    public HediffDef hediff;
    [MustTranslate]
    public string effect;
    [MustTranslate]
    public string abilityLabel;
    [MustTranslate]
    public string abilityDescription;

    public bool drawIcon = true;

    public int twistedFleshCapacity;

    public bool synchronizeHost;
}

public class ParasitismComp : ThingComp
{
    public ParasitismCompProperties Props => (ParasitismCompProperties)this.props;

    public int TotalCost => Props.cost + (System?.Count ?? 0);

    public int ParasiteCount => 1 + (System?.ParasitismHediffs.Count ?? 0);

    private ParasitismSystem? System => (parent as Pawn)?.health?.hediffSet
        ?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
}
