using HiveCreatureFramework;
using RimWorld.Planet;
using Verse;

namespace FleshHive;

public class FleshHiveUnitComp : UnitComp
{
    public override UnitGroup GetCaravanGroup(Caravan caravan, Map previousMap, IReadOnlyList<UnitGroup> nodeGroups)
    {
        UnitGroup? target = base.GetCaravanGroup(caravan, previousMap, nodeGroups);
        if (target != null)
        {
            return target;
        }
        target = group as UnitGroup_TemporaryFleshHive
            ?? previousMap?.GetComponent<MapComponent_FleshHive>()?.group
            ?? caravan.PawnsListForReading.Select(pawn => HCFGameUtility.GetUnitComp(pawn)?.group)
                .OfType<UnitGroup_TemporaryFleshHive>().FirstOrDefault();
        if (target == null)
        {
            target = GameComponent_UnitGroup.Instance.MakeNewGroup(typeof(UnitGroup_TemporaryFleshHive));
            target.name = "TemporaryFleshGroup".Translate();
            target.tags.Add(FleshHiveTags.Flesh);
            target.unitLimit = int.MaxValue;
        }
        return target;
    }
}
