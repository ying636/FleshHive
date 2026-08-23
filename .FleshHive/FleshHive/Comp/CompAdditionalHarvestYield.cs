using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_AdditionalHarvestYield : CompProperties
{
    public CompProperties_AdditionalHarvestYield()
    {
        compClass = typeof(CompAdditionalHarvestYield);
    }

    public List<ThingDefCountClass> yields = [];
}

public class CompAdditionalHarvestYield : ThingComp
{
    public override IEnumerable<ThingDefCountClass> GetAdditionalHarvestYield()
    {
        foreach (ThingDefCountClass yield in Props.yields)
        {
            if (yield?.thingDef != null && yield.count > 0)
            {
                yield return yield;
            }
        }
    }

    private CompProperties_AdditionalHarvestYield Props => (CompProperties_AdditionalHarvestYield)props;
}
