using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompHiveContainer_HostileFleshHive : CompHiveContainer
{
    public override bool CanAdd(UnitGroup group)
    {
        if (group is not HiveLordUnitGroup || !base.CanAdd(group))
            return false;
        if (group.hive == parent)
            return true;
        MapComponent_HiveLord manager = parent.Map.GetComponent<MapComponent_HiveLord>();
        HiveLord? owner = manager.GetLord(parent);
        return owner != null && manager.GetLord(group.hive) == owner;
    }

    public override float GetHealPoint()
    {
        return Props.healPointPerInterval;
    }

    public override void Heal(Pawn unit, float point)
    {
        float injuries = unit.health.hediffSet.hediffs.OfType<Hediff_Injury>().Sum(injury => injury.Severity);
        bool regeneration = unit.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().Any()
            || unit.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss) != null;
        if (injuries <= 0f && !regeneration)
            return;
        HiveResource? resource = Resource.resources.FirstOrDefault(resource => resource.def == Props.healResource);
        if (resource == null || Props.healPointPerResource <= 0f)
            return;
        float healing = Math.Min(point, resource.Amount * Props.healPointPerResource);
        if (!regeneration)
            healing = Math.Min(healing, injuries);
        if (healing <= 0f)
            return;
        resource.DecreaseResource(healing / Props.healPointPerResource);
        base.Heal(unit, healing);
    }
}
