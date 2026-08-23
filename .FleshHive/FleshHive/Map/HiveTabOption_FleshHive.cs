using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public abstract class HiveTabOption_FleshHive : HiveTabOption
{
    protected bool DrawHungryIfNeeded(Rect inRect)
    {
        Map map = Find.CurrentMap;
        if (map == null || !map.listerThings.AllThings
                .OfType<ThingWithComps>()
                .Where(thing => thing.Faction == Faction.OfPlayer
                    && (thing.def.defName == FleshHiveDefName || thing.def == FleshHiveDefOf.FH_FleshPrimaryNest))
                .Any(FleshHiveHungerUtility.IsHungry))
        {
            return false;
        }

        Window_FleshHive.DrawHungryContents(inRect);
        return true;
    }

    private const string FleshHiveDefName = "FH_FleshHive";
}
