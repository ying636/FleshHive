using System.Collections.Generic;
using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class CompProperties_ProjectileEffecterCE : CompProperties_ProjectileEffecter
{
    public CompProperties_ProjectileEffecterCE()
    {
        compClass = typeof(Comp_ProjectileEffecterCE);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        if (!typeof(ProjectileCE).IsAssignableFrom(parentDef.thingClass))
        {
            yield return GetType().Name + " is only meant to be used on ProjectileCE derived Things";
        }
    }
}
