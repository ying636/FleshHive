using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FleshHive;

public static class FleshHopperUtility
{
    public static IEnumerable<Building_FleshHopper> GetCachedHoppers(Map map)
    {
        if (map == null)
        {
            yield break;
        }

        map.GetComponent<MapComponent_FleshHive>()?.CleanupInvalidHoppers();
        MapFleshHive mapFleshHive = MapComponent_FleshHive.GetMapFleshHive(map);
        if (mapFleshHive == null)
        {
            yield break;
        }

        foreach (Building_FleshHopper hopper in mapFleshHive.CachedFleshHoppers)
        {
            if (IsValidHopper(hopper, map))
            {
                yield return hopper;
            }
        }
    }

    public static Thing FindThingOnClosestHopper(Map map, IntVec3 targetCell, ThingDef thingDef, out Building_FleshHopper hopper)
    {
        hopper = null;
        if (map == null || thingDef == null)
        {
            return null;
        }

        Thing closestThing = null;
        float closestDistance = float.MaxValue;
        foreach (Building_FleshHopper cachedHopper in GetCachedHoppers(map))
        {
            Thing thing = GetStoredThings(cachedHopper).FirstOrDefault(storedThing => storedThing.def == thingDef && storedThing.stackCount > 0);
            if (thing == null)
            {
                continue;
            }

            float distance = cachedHopper.Position.DistanceToSquared(targetCell);
            if (distance >= closestDistance)
            {
                continue;
            }

            hopper = cachedHopper;
            closestThing = thing;
            closestDistance = distance;
        }

        return closestThing;
    }

    public static Building_FleshHopper FindClosestHopper(Map map, IntVec3 targetCell, Predicate<Building_FleshHopper> validator = null)
    {
        if (map == null)
        {
            return null;
        }

        Building_FleshHopper closest = null;
        float closestDistance = float.MaxValue;
        foreach (Building_FleshHopper hopper in GetCachedHoppers(map))
        {
            if (validator != null && !validator(hopper))
            {
                continue;
            }

            float distance = hopper.Position.DistanceToSquared(targetCell);
            if (distance >= closestDistance)
            {
                continue;
            }

            closest = hopper;
            closestDistance = distance;
        }

        return closest;
    }

    public static bool HasAvailableHopper(Map map)
    {
        return FindClosestHopper(map, IntVec3.Invalid) != null;
    }

    public static IEnumerable<Thing> GetStoredThings(Building_FleshHopper hopper)
    {
        if (!IsValidHopper(hopper, hopper?.Map))
        {
            yield break;
        }

        List<Thing> things = hopper.Position.GetThingList(hopper.Map);
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (thing == null ||
                thing == hopper ||
                thing.def.category == ThingCategory.Building ||
                thing.def.IsBlueprint ||
                thing.def.IsFrame)
            {
                continue;
            }

            yield return thing;
        }
    }

    public static float GetNutritionValue(Thing thing)
    {
        if (thing == null ||
            thing.Destroyed ||
            thing is Pawn ||
            thing.def.category == ThingCategory.Building ||
            thing.def.IsBlueprint ||
            thing.def.IsFrame)
        {
            return 0f;
        }

        float nutrition = thing.GetStatValue(StatDefOf.Nutrition, true, -1);
        if (nutrition <= 0f)
        {
            return 0f;
        }

        return nutrition * thing.stackCount;
    }

    public static bool TryPlaceThingOnClosestHopper(Thing sourceHive, Thing thing, out Thing placedThing)
    {
        placedThing = null;
        if (sourceHive?.Map == null || thing == null)
        {
            return false;
        }

        Building_FleshHopper hopper = FindClosestHopper(sourceHive.Map, sourceHive.Position);
        if (hopper == null)
        {
            return false;
        }

        return TryPlaceThingOnHopper(hopper, thing, out placedThing);
    }

    public static bool TryPlaceThingOnHopper(Building_FleshHopper hopper, Thing thing, out Thing placedThing)
    {
        placedThing = null;
        if (!IsValidHopper(hopper, hopper?.Map) || thing == null)
        {
            return false;
        }

        return GenPlace.TryPlaceThing(thing, hopper.Position, hopper.Map, ThingPlaceMode.Direct, out placedThing);
    }

    private static bool IsValidHopper(Building_FleshHopper hopper, Map map)
    {
        return hopper != null && !hopper.Destroyed && hopper.Spawned && hopper.Map == map;
    }
}
