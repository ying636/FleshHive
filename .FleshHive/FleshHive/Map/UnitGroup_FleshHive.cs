using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class UnitGroup_FleshHive : UnitGroup_FleshHunting
{
    public override bool Controllable => base.Controllable && !FleshHiveHungerUtility.IsHungry(hive);

    public override bool CanReturnHive => base.CanReturnHive && !FleshHiveHungerUtility.IsHungry(hive);

    public override void AcceptUnit(Pawn unit)
    {
        if (unit == null)
        {
            return;
        }

        if (unit.TryGetComp<UnitComp>()?.group == this && this.units.Contains(unit))
        {
            return;
        }

        base.AcceptUnit(unit);
        Map?.GetComponent<MapComponent_FleshHive>()?.EnforceHiveGroupCapacity();
    }

    public override AcceptReason CanAccept(Pawn unit)
    {
        if (FleshHiveHungerUtility.IsHungry(hive))
        {
            return AcceptReason.False("FH_GroupReject_Hungry".Translate());
        }

        AcceptReason baseReason = base.CanAccept(unit);
        if (!baseReason.Accepted)
        {
            return baseReason;
        }

        return Map?.GetComponent<MapComponent_FleshHive>()?.CanAcceptIntoHiveGroup(this, unit) == false
            ? AcceptReason.False("FH_GroupReject_HivePopulationCapacity".Translate())
            : AcceptReason.True;
    }
}
