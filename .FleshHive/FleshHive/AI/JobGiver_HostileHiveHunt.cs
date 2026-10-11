using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_HostileHiveHunt : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        UnitGroup? group = HCFGameUtility.GetUnitComp(pawn)?.group;
        HostileFleshHiveColony? colony = pawn.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(group?.hive!);
        if (colony == null || group != colony.HuntGroup || !colony.Hunting || colony.Store == null)
            return null;
        if (pawn.health.summaryHealth.SummaryHealthPercent < 0.6f
            || (pawn.GetNeed<Need_Maintenance>()?.CurLevelPercentage ?? 1f) < 0.35f)
        {
            Thing? container = group.GetContainer(pawn, true);
            if (container != null && pawn.CanReach(container, PathEndMode.Touch, Danger.Deadly))
                return JobMaker.MakeJob(HCFDefOf.HCF_EnterHive, container);
        }
        if (colony.Corpse is { Destroyed: false } corpse)
        {
            if (corpse.Spawned && pawn.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly))
            {
                Job haul = JobMaker.MakeJob(HostileFleshHiveDefOf.FH_HostileHiveHaul, corpse, colony.Store);
                haul.count = 1;
                return haul;
            }
            return WaitOrApproach(pawn, colony.Store.Position, 6f);
        }
        Pawn? prey = colony.Prey;
        if (prey is not { Spawned: true } || prey.Dead)
            return WaitOrApproach(pawn, colony.Store.Position, 6f);
        if (!colony.Gathered)
            return WaitOrApproach(pawn, prey.Position, 7f);
        if (prey.Downed)
        {
            return pawn.CanReserveAndReach(prey, PathEndMode.Touch, Danger.Deadly)
                ? JobMaker.MakeJob(FleshHiveDefOf.FH_Job_HuntExecution, prey) : WaitOrApproach(pawn, prey.Position, 3f);
        }
        Job attack = JobMaker.MakeJob(JobDefOf.AttackMelee, prey);
        attack.expiryInterval = 600;
        attack.checkOverrideOnExpire = true;
        return attack;
    }

    private Job WaitOrApproach(Pawn pawn, IntVec3 point, float radius)
    {
        Job job;
        if (!pawn.Position.InHorDistOf(point, radius + 1f)
            && CellFinder.TryFindRandomReachableNearbyCell(point, pawn.Map, radius,
                TraverseParms.For(pawn), cell => cell.Standable(pawn.Map) && !cell.GetThingList(pawn.Map).OfType<Pawn>().Any(),
                null, out IntVec3 destination))
            job = JobMaker.MakeJob(JobDefOf.Goto, destination);
        else
            job = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        job.expiryInterval = 120;
        job.checkOverrideOnExpire = true;
        return job;
    }
}
