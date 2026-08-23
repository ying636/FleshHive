using HiveCreatureFramework;
using Verse.AI.Group;

namespace FleshHive;

public class GroupWorkModeDefWorker_Hunt : GroupWorkModeDefWorker
{
    public override bool CanGroupSupport(UnitGroup group)
    {
        return group is IFleshHiveHuntingGroup;
    }

    public override LordToil CreateLordToil(UnitGroup group)
    {
        return new LordToil_GroupHunt(group);
    }
}
