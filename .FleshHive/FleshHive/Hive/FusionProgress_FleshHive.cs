using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class FusionProgress_FleshHive : FusionProgress
{
    public override void Cancel(CompProgressHolder comp)
    {
        List<Pawn> pawns = inners.OfType<Pawn>().ToList();
        base.Cancel(comp);

        MapComponent_Unit unitMap = comp.parent.Map?.GetComponent<MapComponent_Unit>();
        if (unitMap == null)
        {
            return;
        }

        foreach (Pawn pawn in pawns)
        {
            unitMap.FusionRequirements.Remove(pawn);
            unitMap.UnitsInSpecialState.Remove(pawn);
        }
    }
}
