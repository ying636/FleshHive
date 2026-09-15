using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_FillFleshSack : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Thing target = job.targetA.Thing;
        FleshSack sack = (FleshSack)job.targetB.Thing;
        bool ok = pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
        ok &= pawn.Reserve(sack, job, 1, -1, null, errorOnFailed);
        return ok;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => job.targetB.Thing is not FleshSack sack || !sack.CanAcceptMore);
        this.FailOn(() =>
        {
            Thing target = job.targetA.Thing;
            if (target is Corpse corpse)
            {
                if (corpse.Destroyed || corpse.GetRotStage() != RotStage.Fresh)
                {
                    Designation designation = Map.designationManager.DesignationOn(corpse, FleshHiveDefOf.FH_MarkPrey);
                    if (designation != null)
                    {
                        Map.designationManager.RemoveDesignation(designation);
                    }
                    return true;
                }
                return false;
            }
            return target is not Pawn targetPawn || targetPawn.Dead || !targetPawn.Downed;
        });

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        Toil putInSack = new Toil();
        putInSack.initAction = delegate
        {
            FleshSack sack = (FleshSack)job.targetB.Thing;
            Thing carried = pawn.carryTracker.CarriedThing;
            if (carried == null || !sack.CanAcceptMore)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            bool inserted = carried switch
            {
                Pawn targetPawn => sack.InsertPawn(targetPawn),
                Corpse corpse => sack.InsertCorpse(corpse),
                _ => false
            };
            if (!inserted)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (carried is Pawn && carried.Faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter(
                    "FH_FleshSack_FriendlyTitle".Translate(carried.LabelShort),
                    "FH_FleshSack_FriendlyDesc".Translate(carried.LabelShort),
                    LetterDefOf.NegativeEvent,
                    sack
                );
            }
        };
        putInSack.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return putInSack;
    }
}
