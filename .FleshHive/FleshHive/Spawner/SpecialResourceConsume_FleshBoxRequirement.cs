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

        List<Thing> storedThings = FleshBoxUtility.GetCachedBoxes(map)
            .SelectMany(FleshBoxUtility.GetStoredThings)
            .ToList();
        foreach (ThingDefCountClass requirement in requirements)
        {
            int availableCount = storedThings
                .Where(thing => thing.def == requirement.thingDef)
                .Sum(thing => thing.stackCount);
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
