using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class CompProperties_InfectHarbingerTree : CompProperties
{
    public CompProperties_InfectHarbingerTree()
    {
        compClass = typeof(CompInfectHarbingerTree);
    }
}

public class CompInfectHarbingerTree : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (!parent.Spawned || parent.def != ThingDefOf.Plant_TreeHarbinger)
        {
            yield break;
        }
        // if (!FleshHiveResearchUtility.IsFinished(FleshHiveDefOf.FH_Research_BasicFleshHive))
        if (FleshHiveDefOf.FH_Research_BasicFleshHive?.IsFinished != true)
        {
            yield break;
        }
        if (parent.Map.GetComponent<MapComponent_FleshHive>()?.HasFleshHive != true)
        {
            yield break;
        }

        yield return new Command_Action
        {
            defaultLabel = "FH_InfectHarbingerTree".Translate(),
            defaultDesc = "FH_InfectHarbingerTreeDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("Things/Plant/FH_FleshTree/FH_FleshTree_4"),
            action = ToggleDesignation
        };
    }

    private void ToggleDesignation()
    {
        Designation designation = parent.Map.designationManager.DesignationOn(parent, FleshHiveDefOf.FH_InfectHarbingerTree);
        if (designation != null)
        {
            parent.Map.designationManager.RemoveDesignation(designation);
            return;
        }
        parent.Map.designationManager.AddDesignation(new Designation(parent, FleshHiveDefOf.FH_InfectHarbingerTree));
    }
}
