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
                GameComponent_UnitGroup.Instance.selectedGroup = group;
            }
        }
    }
}
