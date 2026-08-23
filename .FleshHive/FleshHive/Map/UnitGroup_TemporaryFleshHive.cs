using System.Collections.Generic;
using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class UnitGroup_TemporaryFleshHive : UnitGroup
{
    public override bool CanDrawTarget => false;

    public override void AcceptUnit(Pawn unit)
    {
        base.AcceptUnit(unit);
        if (unit != null)
        {
            unit.forceNoDeathNotification = true;
        }
    }

    public override void RemoveUnit(Pawn unit)
    {
        base.RemoveUnit(unit);
        if (unit != null)
        {
            unit.forceNoDeathNotification = false;
        }
    }

    public override IEnumerable<GroupWorkMode> GetModes()
    {
        yield return GroupWorkMode.Attack;
    }
}
