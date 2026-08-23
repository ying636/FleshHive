using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompProperties_FleshHiveContainer : CompPropertiesHiveContainer
{
    public CompProperties_FleshHiveContainer()
    {
        compClass = typeof(CompFleshHiveContainer);
    }
}

public class CompFleshHiveContainer : CompHiveContainer
{
    public override void AddUnit(Pawn unit)
    {
        base.AddUnit(unit);
        parent.Map?.GetComponent<MapComponent_FleshHive>()?.SyncFleshBeastUpgradeHediffs(unit);
    }

    public override void Heal(Pawn unit, float point)
    {
        if (unit?.health?.hediffSet?.HasHediff(FleshHiveDefOf.FH_Hediff_Upgrade_FastHealing) == true)
        {
            point *= 1.5f;
        }

        base.Heal(unit, point);
    }
}
