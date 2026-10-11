using HiveCreatureFramework;
using Verse.AI.Group;

namespace FleshHive;

public class GroupWorkModeDefWorker_HostileHunt : GroupWorkModeDefWorker
{
    public override bool CanGroupSupport(UnitGroup group)
    {
        return group is HostileFleshHiveHuntGroup;
    }

    public override LordToil CreateLordToil(UnitGroup group)
    {
        return new LordToil_HostileHiveHunt();
    }
}
