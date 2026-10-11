using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class IncidentDef_SpreadingFleshHive : IncidentDef
{
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (hive == null || hive.category != ThingCategory.Building || !hive.CanHaveFaction
            || !typeof(Building_Hive).IsAssignableFrom(hive.thingClass)
            || hive.GetCompProperties<CompPropertiesHiveResource>() == null
            || hive.GetCompProperties<CompPropertiesHiveSpawner>() == null
            || !hive.comps.Any(comp => typeof(CompProgressHolder).IsAssignableFrom(comp.compClass)))
        {
            yield return $"{defName}: hive must be a faction-capable HCF hive with resource, spawner and progress components.";
        }
        if (hiveLord is not HiveLordDef_HostileFleshHive || initialUnits.NullOrEmpty()
            || initialUnits.Any(unit => unit?.kind == null
                || !typeof(Unit).IsAssignableFrom(unit.kind.race.thingClass)
                || unit.kind.race.GetCompProperties<UnitCompProperties>() is not { tags.Count: > 0 }))
        {
            yield return $"{defName}: hiveLord and HCF initialUnits are required.";
        }
        if (minInitialUnits < 1 || maxInitialUnits < minInitialUnits
            || hiveLord != null && (maxInitialUnits > hiveLord.maxUnits
                || !initialUnits.NullOrEmpty() && initialUnits.Any(unit => unit?.kind?.race != null
                    && unit.kind.race.GetCompProperties<UnitCompProperties>() is { } properties
                    && maxInitialUnits * properties.groupCost > hiveLord.groupCapacity))
            || initialPointsFraction <= 0f || initialPointsFraction > 1f
            || minColonyDistance < 1f || minHiveDistance < 1f || cellSearchAttempts < 1)
        {
            yield return $"{defName}: invalid initial population, point scaling or placement settings.";
        }
    }

    public ThingDef hive = null!;
    public HiveLordDef hiveLord = null!;
    public List<UnitDef> initialUnits = new();
    public int minInitialUnits = 3;
    public int maxInitialUnits = 8;
    public float initialPointsFraction = 0.35f;
    public float minColonyDistance = 35f;
    public float minHiveDistance = 45f;
    public int cellSearchAttempts = 800;
}
