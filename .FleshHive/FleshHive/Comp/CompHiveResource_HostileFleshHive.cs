using HiveCreatureFramework;

namespace FleshHive;

public class CompHiveResource_HostileFleshHive : CompHiveResource
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
}
