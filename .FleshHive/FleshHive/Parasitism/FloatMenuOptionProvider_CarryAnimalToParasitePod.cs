using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public sealed class FloatMenuOptionProvider_CarryAnimalToParasitePod : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        return pawn != null && pawn.Faction?.IsPlayer == true && !pawn.DeadOrDowned;
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Pawn animal || animal.Dead || !animal.Downed || !animal.RaceProps.Animal)
        {
            yield break;
        }

        Pawn carrier = context.FirstSelectedPawn;
        if (carrier == null || carrier.DeadOrDowned || carrier.Map != animal.Map)
        {
            yield break;
        }

        List<FleshParasitePod> pods = carrier.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshParasiteVat)
            .OfType<FleshParasitePod>()
            .Where(pod => pod.Spawned && pod.Faction == Faction.OfPlayer && pod.curQuest == null && !pod.start
                && pod.targetUI == null && !pod.target.Any && !pod.flesh.Any
                && carrier.CanReserveAndReach(pod, PathEndMode.Touch, Danger.Deadly))
            .OrderBy(pod => pod.Position.DistanceToSquared(animal.Position))
            .ToList();

        foreach (FleshParasitePod pod in pods)
        {
            if (!carrier.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Deadly))
            {
                yield break;
            }

            FleshParasitePod selectedPod = pod;
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                "FH_Parasitism_CarryAnimalToPod".Translate(selectedPod.LabelCap), () =>
                {
                    if (carrier.Map != animal.Map || !animal.Spawned || animal.Dead || !animal.Downed
                        || !carrier.CanReserveAndReach(animal, PathEndMode.Touch, Danger.Deadly)
                        || !carrier.CanReserveAndReach(selectedPod, PathEndMode.Touch, Danger.Deadly)
                        || !selectedPod.TryQueueTargetPawn(animal))
                    {
                        return;
                    }

                    Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_PutPawnInParasitePod, animal, selectedPod);
                    job.count = 1;
                    carrier.jobs.TryTakeOrderedJob(job, JobTag.MiscWork);
                }, MenuOptionPriority.High), carrier, animal, "ReservedBy");
        }
    }
}
