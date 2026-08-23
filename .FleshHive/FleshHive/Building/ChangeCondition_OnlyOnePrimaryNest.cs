using System.Linq;
using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class ChangeCondition_OnlyOnePrimaryNest : ChangeCondition
{
    public override AcceptReason CanChange(Thing hive, CompHiveEvolution comp)
    {
        if (!HasExistingPrimaryNest(hive))
        {
            return AcceptReason.True;
        }

        return AcceptReason.False("FH_FleshPrimaryNest_OnlyOne".Translate());
    }

    private static bool HasExistingPrimaryNest(Thing hive)
    {
        if (hive.Map == null)
        {
            return false;
        }

        return hive.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshPrimaryNest)
            .Any(thing => thing.Faction == hive.Faction);
    }
}
