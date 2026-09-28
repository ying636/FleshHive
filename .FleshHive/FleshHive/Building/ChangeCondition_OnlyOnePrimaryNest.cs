using System.Linq;
using HiveCreatureFramework;
using HiveCreatureFramework.Evolution;
using Verse;

namespace FleshHive;

public class ChangeCondition_OnlyOnePrimaryNest : ChangeCondition
{
    public override AcceptReason CanChange(Thing hive, CompHiveEvolution comp)
    {
        if (HasExistingPrimaryNest(hive))
        {
            return AcceptReason.False("FH_FleshPrimaryNest_OnlyOne".Translate());
        }

        if (comp.Progress?.progresses.OfType<HiveEvolutionProgress>()
                .Any(progress => progress.def?.resultThing == FleshHiveDefOf.FH_FleshPrimaryNest) == true)
        {
            return AcceptReason.False("FH_FleshHive_EvolutionInProgress".Translate());
        }

        if (hive.Map != null && hive.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshHive)
                .Any(otherHive => otherHive != hive
                    && otherHive.Faction == hive.Faction
                    && otherHive.TryGetComp<CompProgressHolder>()?.progresses.OfType<HiveEvolutionProgress>()
                        .Any(progress => progress.def?.resultThing == FleshHiveDefOf.FH_FleshPrimaryNest) == true))
        {
            return AcceptReason.False("FH_FleshPrimaryNest_OnlyOne".Translate());
        }

        return AcceptReason.True;
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
