using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class FleshHiveUnitComp : UnitComp
{
    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        if (group is HostileFleshHiveHuntGroup && parent.Spawned
            && dinfo.Instigator is Pawn { Faction: null } attacker
            && parent.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(group.hive) is { } colony
            && colony.HuntGroup == group && colony.Prey == attacker)
            return;
        base.PostPostApplyDamage(dinfo, totalDamageDealt);
    }
}
