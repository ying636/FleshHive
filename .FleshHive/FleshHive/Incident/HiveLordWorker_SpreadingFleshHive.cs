using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class HiveLordWorker_SpreadingFleshHive : HiveLordWorker
{
    public override bool ShouldProduce(HiveLord lord, Thing hive)
    {
        HostileFleshHiveColony colony = lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        float reserve = lord.Population + lord.QueuedUnits < 9 || lord.hives.Count == def.maxHives
            ? colony.Settings.reserveNutrition : colony.Settings.reserveNutrition + colony.ExpansionCost;
        return colony.Nutrition >= reserve + 3f;
    }

    public override bool ShouldExpand(HiveLord lord)
    {
        return lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord).CanExpand;
    }

    public override bool ShouldAttack(HiveLord lord)
    {
        HostileFleshHiveColony colony = lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        return colony.CanRaid && colony.Store != null && colony.Nutrition >= colony.Settings.reserveNutrition + 20f;
    }

    public override bool ShouldRecall(HiveLord lord)
    {
        HostileFleshHiveColony colony = lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        return colony.Nutrition < 20f || colony.HealthyDefenders < def.minDefenders;
    }

    public override float ScoreExpansionCell(HiveLord lord, Thing source, ThingDef building, IntVec3 cell)
    {
        return lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord).ScoreHiveCell(building, cell);
    }

    public override float ScoreExpansionSource(HiveLord lord, Thing hive)
    {
        HostileFleshHiveColony colony = lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        float cost = hive.TryGetComp<CompHiveResource>().resources.Sum(resource => resource.GetLimit() * def.expansionResourceFraction);
        return colony.Nutrition >= colony.Settings.reserveNutrition + cost
            ? base.ScoreExpansionSource(lord, hive) : float.NegativeInfinity;
    }

    public override UnitDef SelectUnit(HiveLord lord, Thing hive, List<UnitDef> candidates)
    {
        if (lord.hives.Count < 2 || lord.Population + lord.QueuedUnits < 12)
        {
            candidates = candidates.Where(unit => unit.kind.race
                .GetCompProperties<UnitCompProperties>().groupCost == 1).ToList();
        }
        HostileFleshHiveColony colony = lord.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(lord);
        float reserve = lord.Population + lord.QueuedUnits < 9 || lord.hives.Count == def.maxHives
            ? colony.Settings.reserveNutrition : colony.Settings.reserveNutrition + colony.ExpansionCost;
        candidates = candidates.Where(unit => colony.Nutrition >= reserve + unit.costs
            .Where(cost => cost.resource == HostileFleshHiveDefOf.FH_Resource_HostileNutrition).Sum(cost => cost.amount)).ToList();
        return base.SelectUnit(lord, hive, candidates);
    }
}
