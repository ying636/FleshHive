using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class SpecialResourceConsume_FleshBoxRequirement : SpecialResourceConsume
{
    public override string Label => requirements.NullOrEmpty()
        ? string.Empty
        : string.Join(", ", requirements.Select(requirement => requirement.Label.ToString()));

    public override AcceptanceReport Satisfied(Thing hive)
    {
        return Satisfied(hive?.Map);
    }

    public AcceptanceReport Satisfied(Map map)
    {
        if (requirements.NullOrEmpty())
        {
            return true;
        }
        if (map == null)
        {
            return false;
        }

        if (requirements.Count == 1)
        {
            ThingDefCountClass requirement = requirements[0];
            int availableCount = FleshBoxUtility.GetCachedBoxes(map)
                .SelectMany(FleshBoxUtility.GetStoredThings)
                .Where(thing => thing.def == requirement.thingDef)
                .Sum(thing => thing.stackCount);
            return availableCount < requirement.count ? "LackRequiredItems".Translate(requirement.Label) : true;
        }

        Dictionary<ThingDef, long> availableCounts = new();
        foreach (ThingDefCountClass requirement in requirements)
        {
            if (requirement.thingDef != null)
            {
                availableCounts[requirement.thingDef] = 0;
            }
        }
        foreach (Thing thing in FleshBoxUtility.GetCachedBoxes(map).SelectMany(FleshBoxUtility.GetStoredThings))
        {
            if (!availableCounts.TryGetValue(thing.def, out long count))
            {
                continue;
            }

            availableCounts[thing.def] = count + thing.stackCount;
        }
        foreach (ThingDefCountClass requirement in requirements)
        {
            int availableCount = requirement.thingDef != null && availableCounts.TryGetValue(requirement.thingDef, out long count)
                ? checked((int)count) : 0;
            if (availableCount < requirement.count)
            {
                return "LackRequiredItems".Translate(requirement.Label);
            }
        }
        return true;
    }

    public override void Consume(Thing hive)
    {
        if (!TryConsume(hive?.Map, hive?.Position ?? IntVec3.Invalid))
        {
            Log.Error("[FleshHive] Failed to consume flesh vesicle requirements.");
        }
    }

    public bool TryConsume(Map map, IntVec3 sourcePosition)
    {
        if (!Satisfied(map).Accepted)
        {
            return false;
        }

        ConsumeValidated(map, sourcePosition);
        return true;
    }

    private void ConsumeValidated(Map map, IntVec3 sourcePosition)
    {
        IntVec3 origin = sourcePosition.IsValid ? sourcePosition : map.Center;
        List<Building_FleshBox> boxes = FleshBoxUtility.GetCachedBoxes(map)
            .OrderBy(box => box.Position.DistanceToSquared(origin))
            .ToList();
        foreach (ThingDefCountClass requirement in requirements)
        {
            int remainingCount = requirement.count;
            foreach (Building_FleshBox box in boxes)
            {
                List<Thing> stacks = FleshBoxUtility.GetStoredThings(box)
                    .Where(thing => thing.def == requirement.thingDef)
                    .ToList();
                foreach (Thing stack in stacks)
                {
                    int consumeCount = Math.Min(stack.stackCount, remainingCount);
                    if (consumeCount >= stack.stackCount)
                    {
                        stack.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Thing consumed = stack.SplitOff(consumeCount);
                        consumed.Destroy(DestroyMode.Vanish);
                    }

                    remainingCount -= consumeCount;
                    if (remainingCount <= 0)
                    {
                        break;
                    }
                }
                if (remainingCount <= 0)
                {
                    break;
                }
            }

            if (remainingCount > 0)
            {
                Log.Error($"[FleshHive] Failed to consume {requirement.Label}; {remainingCount} remained after validation.");
            }
        }
    }

    public List<ThingDefCountClass> requirements = new();
}
