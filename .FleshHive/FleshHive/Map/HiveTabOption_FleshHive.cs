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
        if (map == null || !HasHungryHive(map))
        {
            return false;
        }

        Window_FleshHive.DrawHungryContents(inRect);
        return true;
    }

    private static bool HasHungryHive(Map map)
    {
        return map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshHive)
            .Concat(map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest))
            .OfType<ThingWithComps>()
            .Where(thing => thing.Faction == Faction.OfPlayer)
            .Any(FleshHiveHungerUtility.IsHungry);
    }
}
