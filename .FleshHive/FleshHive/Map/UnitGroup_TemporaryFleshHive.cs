using System.Collections.Generic;
using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class UnitGroup_TemporaryFleshHive : UnitGroup_FleshHive
{
    public override bool CanDrawTarget => false;
    public override void Make()
    {
        this.SetMode(HCFDefOf.HCF_GroupWorkMode_Attack);
    }

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

    public override IEnumerable<GroupWorkModeDef> GetModeDefs()
    {
        yield return HCFDefOf.HCF_GroupWorkMode_Attack;
    }
}
