using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public static class FleshBoxUtility
{
    public static IEnumerable<Building_FleshBox> GetCachedBoxes(Map map)
    {
        if (map == null)
        {
            yield break;
        }

        map.GetComponent<MapComponent_FleshHive>()?.CleanupInvalidFleshBoxes();
        MapFleshHive mapFleshHive = MapComponent_FleshHive.GetMapFleshHive(map);
        if (mapFleshHive == null)
        {
            yield break;
        }

        foreach (Building_FleshBox box in mapFleshHive.CachedFleshBoxes)
        {
            if (IsValidBox(box, map))
            {
                yield return box;
            }
        }
    }

    public static Thing FindThingOnClosestBox(Map map, IntVec3 targetCell, ThingDef thingDef, out Building_FleshBox box)
    {
        box = null;
        if (map == null || thingDef == null)
        {
            return null;
        }

        Thing closestThing = null;
        float closestDistance = float.MaxValue;
        foreach (Building_FleshBox cachedBox in GetCachedBoxes(map))
        {
            Thing thing = GetStoredThings(cachedBox).FirstOrDefault(storedThing => storedThing.def == thingDef && storedThing.stackCount > 0);
            if (thing == null)
            {
                continue;
            }

            float distance = cachedBox.Position.DistanceToSquared(targetCell);
            if (distance >= closestDistance)
            {
                continue;
            }

            box = cachedBox;
            closestThing = thing;
            closestDistance = distance;
        }

        return closestThing;
    }

    public static bool IsStoredInFleshBox(Thing thing)
    {
        if (thing?.Map == null || !thing.Spawned)
        {
            return false;
        }

        return GetCachedBoxes(thing.Map).Any(box => GetStoredThings(box).Contains(thing));
    }

    public static IEnumerable<Thing> GetStoredThings(Building_FleshBox box)
    {
        if (!IsValidBox(box, box?.Map))
        {
            yield break;
        }

        List<Thing> things = box.Position.GetThingList(box.Map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (thing == null ||
                thing == box ||
                thing.def.category == ThingCategory.Building ||
                thing.def.IsBlueprint ||
                thing.def.IsFrame)
            {
                continue;
            }

            yield return thing;
        }
    }

    private static bool IsValidBox(Building_FleshBox box, Map map)
    {
        return box != null && !box.Destroyed && box.Spawned && box.Map == map;
    }
}
