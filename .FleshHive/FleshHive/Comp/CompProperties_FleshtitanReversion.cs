using Verse;

namespace FleshHive;

public class CompProperties_FleshtitanReversion : CompProperties
{
    public CompProperties_FleshtitanReversion()
    {
        compClass = typeof(CompFleshtitanReversion);
    }

    public ThingDef wildTitanRace = null!;

    public ThingDef controlledTitanRace = null!;

    public ThingDef heartDef = null!;

    public ThingDef controlledHeartDef = null!;

    public int revertAfterTicks = 30000;
}
