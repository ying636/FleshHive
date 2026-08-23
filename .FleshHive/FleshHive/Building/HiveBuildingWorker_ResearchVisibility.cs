using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class HiveBuildingWorker_ResearchVisibility : HiveBuildingWorker_FleshBlueprint
{
    public override bool CanShow(Map map)
    {
        return base.CanShow(map) && IsVisibleForCurrentResearch();
    }

    private bool IsVisibleForCurrentResearch()
    {
        if (research == null)
        {
            return true;
        }

        return research.IsFinished == showWhenFinished;
    }

    public ResearchProjectDef research;
    public bool showWhenFinished = true;
    public ThingDef replacementBuilding;
}
