using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompHostileHiveSupport : ThingComp
{
    public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        base.PostPostApplyDamage(dinfo, totalDamageDealt);
        if (totalDamageDealt > 0f && parent.Spawned)
            parent.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(parent)?.Lord
                .Notify(TriggerType.BuildingDamaged, parent);
    }
}
