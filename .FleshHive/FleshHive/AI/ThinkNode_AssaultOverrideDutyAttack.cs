using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class ThinkNode_AssaultOverrideDutyAttack : ThinkNode
{
    public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
    {
        PawnDuty duty = pawn.mindState?.duty;
        if (duty?.def == null || !ShouldOverride(duty.def))
        {
            return ThinkResult.NoJob;
        }

        Lord lord = pawn.GetLord();
        if (lord == null)
        {
            return ThinkResult.NoJob;
        }

        DutyDef overrideDuty = pawn.TryGetComp<UnitComp>()?.Props.overrideDuty_Attack;
        if (overrideDuty?.thinkNode == null)
        {
            return ThinkResult.NoJob;
        }

        ThinkResult result = overrideDuty.thinkNode.TryIssueJobPackage(pawn, jobParams);
        result = lord.Notify_DutyResult(result, pawn, jobParams);
        if (result.Job != null)
        {
            result.Job.lord = lord;
            result.Job.source = duty.source;
            result.Job.dutyTag = duty.tag;
        }
        return result;
    }

    private static bool ShouldOverride(DutyDef dutyDef)
    {
        return dutyDef == DutyDefOf.AssaultColony
            || dutyDef == DutyDefOf.FleshbeastAssault;
    }
}
