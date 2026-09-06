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
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (respawningAfterLoad || parent is not Pawn pawn || pawn.Faction == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
        {
            return;
        }

        SetFollowMode(pawn);
    }

    public void SetAttackMode()
    {
        foreach (UnitGroup group in groups)
        {
            if (group == null)
            {
                continue;
            }

            group.SetMode(HCFDefOf.HCF_GroupWorkMode_Attack, false);
        }
    }

    private void SetFollowMode(Pawn pawn)
    {
        foreach (UnitGroup group in groups)
        {
            if (group == null)
            {
                continue;
            }

            group.SetMode(HCFDefOf.HCF_GroupWorkMode_Follow, false);
            group.SetTarget(new TargetInfo(pawn), false);
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
