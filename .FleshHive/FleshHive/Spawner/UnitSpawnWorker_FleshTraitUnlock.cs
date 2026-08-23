using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class UnitSpawnWorker_FleshTraitUnlock : UnitSpawnWorker
{
    public override bool IsUnlocked(CompHiveSpawner comp)
    {
        if (base.IsUnlocked(comp))
        {
            return true;
        }

        if (def?.kind == null || !def.IsResearchFinished || !def.prerequisites.NullOrEmpty())
        {
            return false;
        }

        return GameComponent_UnitGroup.Instance?.fusionDatas?.Any(data =>
            data?.unlocked == true
            && data.def?.results?.Any(result =>
                result?.result is FusionResult_PawnKind pawnResult
                && pawnResult.kind == def.kind) == true) == true;
    }
}
