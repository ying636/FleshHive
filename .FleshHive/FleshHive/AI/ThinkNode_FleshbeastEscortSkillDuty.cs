using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class ThinkNode_FleshbeastEscortSkillDuty : ThinkNode_UnitSkillDuty
{
    public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
    {
        ThinkResult result = base.TryIssueJobPackage(pawn, jobParams);
        Job? job = result.Job;
        PawnDuty? duty = pawn.mindState?.duty;
        if (job == null || duty?.def == null)
        {
            return ThinkResult.NoJob;
        }

        if (duty.def != FleshHiveDefOf.FH_FuriousmeldEscort)
        {
            return result;
        }

        if (duty.focus.Thing is not Pawn escortee)
        {
            return ThinkResult.NoJob;
        }

        LocalTargetInfo target = job.targetA;
        if (!target.IsValid || target.Thing == escortee)
        {
            return result;
        }

        float radius = duty.radius > 0f ? duty.radius * 2f : 18f;
        return target.Cell.InHorDistOf(escortee.Position, radius) ? result : ThinkResult.NoJob;
    }
}
