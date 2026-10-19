using HiveCreatureFramework;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace FleshHive;

public class ThinkNode_FleshHiveAllowedArea : ThinkNode_Priority
{
    public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
    {
        ThinkResult result = base.TryIssueJobPackage(pawn, jobParams);
        Area? area = pawn.playerSettings?.AreaRestrictionInPawnCurrentMap;
        if (!pawn.Spawned || pawn.Faction != Faction.OfPlayer || pawn.Drafted || pawn.Downed
            || pawn.InMentalState || pawn.IsBurning() || pawn.IsFormingCaravan()
            || PawnUtility.PlayerForcedJobNowOrSoon(pawn) || result.Job?.playerForced == true
            || result.Tag == JobTag.DraftedOrder || result.Job?.def == JobDefOf.Flee
            || result.Job?.def == HCFDefOf.HCF_EnterHive || area == null || area.TrueCount == 0)
        {
            return result;
        }
        if (area[pawn.Position] && result.Job != null && !result.Job.AnyTargetOutsideArea(area))
        {
            return result;
        }
        if (!area[pawn.Position])
        {
            IntVec3 destination = IntVec3.Invalid;
            float distance = float.MaxValue;
            foreach (IntVec3 cell in area.ActiveCells)
            {
                float candidateDistance = cell.DistanceToSquared(pawn.Position);
                if (candidateDistance < distance && cell.Standable(pawn.Map)
                    && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    destination = cell;
                    distance = candidateDistance;
                }
            }
            if (destination.IsValid)
            {
                return new ThinkResult(JobMaker.MakeJob(JobDefOf.Goto, destination), this);
            }
        }
        return result.Job == null ? result : new ThinkResult(JobMaker.MakeJob(JobDefOf.Wait_Wander, 120), this);
    }
}
