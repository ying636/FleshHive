using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class CompPropertiesHiveGroup_MotherBeast : CompPropertiesHiveGroup_NodeUnit
{
    public CompPropertiesHiveGroup_MotherBeast()
    {
        compClass = typeof(CompHiveGroup_MotherBeast);
    }
}

public class CompHiveGroup_MotherBeast : CompHiveGroup_NodeUnit
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo is Gizmo_Group { group: UnitGroup_FleshHive } groupGizmo
                ? new Gizmo_Group_FleshHive(groupGizmo.group)
                : gizmo;
        }
    }

    public void SetAttackMode()
    {
        foreach (UnitGroup group in groups)
        {
            if (group == null)
            {
                continue;
            }

            group.SetTarget(new TargetInfo(parent), false);
            group.SetMode(HCFDefOf.HCF_GroupWorkMode_Attack, false);
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        if (parent is Pawn pawn && pawn.kindDef == FleshHiveDefOf.FH_Fissionmeld)
        {
            groups.Clear();
        }

        base.PostDestroy(mode, previousMap);
    }
}
