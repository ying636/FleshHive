using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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
        Toil suppression = SuppressActivity();
        yield return suppression;
        yield return Toils_Jump.JumpIf(suppression, () => !job.playerForced
            && SuppressTarget.Map?.GetComponent<MapComponent_FleshHive>() is { } mapComp
            && mapComp.Activity > mapComp.ActivityLimit * (mapComp.AutoSuppressActivityThreshold - 0.05f)
            && !Mathf.Approximately(mapComp.Activity, mapComp.ActivityLimit * (mapComp.AutoSuppressActivityThreshold - 0.05f)));
    }

    private Toil SuppressActivity()
    {
        Toil toil = ToilMaker.MakeToil("SuppressActivity"); 
        toil.initAction = delegate
        {
            if (!job.playerForced && SuppressTarget.Map?.GetComponent<MapComponent_FleshHive>() is { } mapComp
                && (mapComp.Activity <= mapComp.ActivityLimit * (mapComp.AutoSuppressActivityThreshold - 0.05f)
                    || Mathf.Approximately(mapComp.Activity, mapComp.ActivityLimit * (mapComp.AutoSuppressActivityThreshold - 0.05f))))
            {
                ReadyForNextToil();
            }
        };
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
            if (ticksLeftThisToil > 0)
            {
                return;
            }

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
            suppression *= SuppressTarget.GetStatValue(FleshHiveDefOf.FH_Stat_SuppressionEffect);
            if (!job.playerForced)
            {
                float remaining = mapComp.Activity - mapComp.ActivityLimit * (mapComp.AutoSuppressActivityThreshold - 0.05f);
                suppression = Mathf.Min(suppression, Mathf.Max(0f, remaining));
            }
            mapComp.SuppressActivity(suppression);
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
