using Verse;
using Verse.AI;

namespace FleshHive;

public class Building_FleshDigester : Building, IAttackTarget
{
    Thing IAttackTarget.Thing => this;

    public LocalTargetInfo TargetCurrentlyAimingAt => GetComp<CompFleshDigester>().CurrentTarget;

    public float TargetPriorityFactor => 1f;

    public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
    {
        return !Spawned;
    }
}
