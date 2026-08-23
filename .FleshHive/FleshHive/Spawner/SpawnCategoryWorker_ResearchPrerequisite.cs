using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class SpawnCategoryWorker_ResearchPrerequisite : SpawnCategoryWorker
{
    public override bool CanShow(CompHiveSpawner spawner)
    {
        return ResearchFinished && base.CanShow(spawner);
    }

    public override IEnumerable<UnitDef> GetUnits(CompHiveSpawner spawner)
    {
        return ResearchFinished ? base.GetUnits(spawner) : Enumerable.Empty<UnitDef>();
    }

    public override IEnumerable<ItemDef> GetItems(CompHiveSpawner spawner)
    {
        return ResearchFinished ? base.GetItems(spawner) : Enumerable.Empty<ItemDef>();
    }

    private bool ResearchFinished => research?.IsFinished == true;

    public ResearchProjectDef research;
}
