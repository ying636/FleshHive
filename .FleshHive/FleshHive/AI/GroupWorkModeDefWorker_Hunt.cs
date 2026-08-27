using HiveCreatureFramework;
using Verse.AI.Group;

namespace FleshHive;

public class GroupWorkModeDefWorker_Hunt : GroupWorkModeDefWorker
{
    public override bool CanGroupSupport(UnitGroup group)
    {
        return group is UnitGroup_FleshHive && group is not UnitGroup_TemporaryFleshHive;
    }

    public override LordToil CreateLordToil(UnitGroup group)
    {
        return new LordToil_GroupHunt(group);
    }
}
