using Verse;

namespace FleshHive;

public class CompProperties_SynapsePack : CompProperties
{
    public CompProperties_SynapsePack()
    {
        compClass = typeof(CompSynapsePack);
    }

    public HediffDef? nodeHediff;

    public int capacity = 100;
}
