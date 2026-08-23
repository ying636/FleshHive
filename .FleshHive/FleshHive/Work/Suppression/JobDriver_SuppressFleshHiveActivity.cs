using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_SuppressFleshHiveActivity : JobDriver
{
    private Thing SuppressTarget => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(SuppressTarget, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => SuppressTarget.TryGetComp<CompSuppressible>() == null);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return SuppressActivity();
    }

    private Toil SuppressActivity()
    {
        Toil toil = ToilMaker.MakeToil("SuppressActivity"); 
        toil.tickIntervalAction = delegate(int delta)
        {
            MapComponent_FleshHive mapComp = SuppressTarget.Map?.GetComponent<MapComponent_FleshHive>();
            if (mapComp == null || SuppressTarget.TryGetComp<CompSuppressible>() == null)
            {
                toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                return;
            }

            if (!FleshHiveActivitySuppressionUtility.TryGetSuppressionRate(toil.actor, out _))
            {
                toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f);
        };
        toil.AddFinishAction(delegate
        {
            MapComponent_FleshHive mapComp = SuppressTarget.Map?.GetComponent<MapComponent_FleshHive>();
            CompSuppressible suppressible = SuppressTarget.TryGetComp<CompSuppressible>();
            if (mapComp == null || suppressible == null)
            {
                toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                return;
            }

            if (!FleshHiveActivitySuppressionUtility.TryGetSuppressionRate(toil.actor, out float suppressionRate))
            {
                toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }

            float suppression = suppressionRate / SuppressionTicksPerUnit * SuppressionWorkTicks * SuppressionCompletionMultiplier;
            mapComp.SuppressActivity(suppression * suppressible.SuppressionFactor);
        });
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        toil.WithProgressBarToilDelay(TargetIndex.A);
        toil.activeSkill = () => SkillDefOf.Intellectual;
        toil.socialMode = RandomSocialMode.Off;
        toil.defaultCompleteMode = ToilCompleteMode.Delay;
        toil.defaultDuration = SuppressionWorkTicks;
        return toil;
    }

    private const int SuppressionWorkTicks = 250;
    private const float SuppressionTicksPerUnit = 2500f;
    private const float SuppressionCompletionMultiplier = 10f;
}

