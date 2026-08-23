using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompProperties_FleshHiveGlobalProcessSpeed : CompProperties
{
    public CompProperties_FleshHiveGlobalProcessSpeed()
    {
        compClass = typeof(CompFleshHiveGlobalProcessSpeed);
    }
}

public class CompFleshHiveGlobalProcessSpeed : ThingComp
{
    public override float GetStatFactor(StatDef stat)
    {
        float result = base.GetStatFactor(stat);
        if (stat != HCFDefOf.HCF_Stat_HiveProcessSpeed || parent.Map == null)
        {
            return result;
        }

        return result * MapComponent_FleshHive.GetCellDivisionSpeedFactor(parent.Map);
    }
}
