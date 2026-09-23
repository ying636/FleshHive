using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class CompPropertiesHiveGroup_FleshHive : CompPropertiesHiveGroup
{
    public CompPropertiesHiveGroup_FleshHive()
    {
        compClass = typeof(CompHiveGroup_FleshHive);
    }
}

public class CompHiveGroup_FleshHive : CompHiveGroup
{
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        RefreshUnitLimits();
    }

    public override UnitGroup MakeGroup()
    {
        UnitGroup group = base.MakeGroup();
        group.unitLimit = Props.defaultUnitLimit
            + (parent.TryGetComp<CompHiveGroupCapacityProvider>()?.CapacityUpgradeBonus ?? 0);
        return group;
    }

    public void RefreshUnitLimits()
    {
        int limit = Props.defaultUnitLimit
            + (parent.TryGetComp<CompHiveGroupCapacityProvider>()?.CapacityUpgradeBonus ?? 0);
        foreach (UnitGroup group in groups)
        {
            group.unitLimit = limit;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo is Gizmo_Group { group: UnitGroup_FleshHive } groupGizmo
                ? new Gizmo_Group_FleshHive(groupGizmo.group)
                : gizmo;
        }
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (parent.Faction?.IsPlayer == true
            && Find.Selector.SelectedObjects.Count() == 1
            && GameComponent_UnitGroup.Instance != null)
        {
            UnitGroup? group = groups.FirstOrDefault(group => group != null && group.Show);
            if (group != null)
            {
                GameComponent_UnitGroup.Instance.selectedGroups.Clear();
                GameComponent_UnitGroup.Instance.selectedGroups.Add(group);
                GameComponent_UnitGroup.Instance.selectedGroup = group;
            }
        }
    }
}
