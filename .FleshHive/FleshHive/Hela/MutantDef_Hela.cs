using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public class MutantDef_Hela : MutantDef
{
    public override void ResolveReferences()
    {
        base.ResolveReferences();
        List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
        drugWhitelist = thingDefs
            .Where(def => def.IsDrug)
            .ToList();

        foreach (CompProperties_Usable usable in thingDefs
                     .Where(def => def.comps != null)
                     .SelectMany(def => def.comps.OfType<CompProperties_Usable>()))
        {
            usable.allowedMutants ??= new List<MutantDef>();
            if (!usable.allowedMutants.Contains(this))
            {
                usable.allowedMutants.Add(this);
            }
        }
    }
}
