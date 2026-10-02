using CombatExtended;
using HiveCreatureFramework;

namespace FleshHive.CE;

public class Verb_AbilityShootMoveAfterAttackCE : Verb_AbilityShootCE
{
    public override bool TryCastShot()
    {
        bool fired = base.TryCastShot();
        if (fired && CasterPawn != null && HCFGameUtility.GetUnitComp(CasterPawn) is { } comp)
        {
            comp.lastMoveTick = -1250;
        }
        return fired;
    }
}
