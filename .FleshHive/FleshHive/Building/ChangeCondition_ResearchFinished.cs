using HiveCreatureFramework;
using RimWorld;
using Verse;

namespace FleshHive;

public class ChangeCondition_ResearchFinished : ChangeCondition
{
    public override AcceptReason CanChange(Thing hive, CompHiveEvolution comp)
    {
        if (research?.IsFinished == true)
        {
            return AcceptReason.True;
        }

        return AcceptReason.False("MissingRequiredResearch".Translate(research?.LabelCap ?? string.Empty));
    }

    public ResearchProjectDef research;
}
