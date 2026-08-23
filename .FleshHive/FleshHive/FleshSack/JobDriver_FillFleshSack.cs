using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_FillFleshSack : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Pawn targetPawn = (Pawn)job.targetA.Thing;
        FleshSack sack = (FleshSack)job.targetB.Thing;
        bool ok = pawn.Reserve(targetPawn, job, 1, -1, null, errorOnFailed);
        ok &= pawn.Reserve(sack, job, 1, -1, null, errorOnFailed);
        return ok;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => job.targetB.Thing is not FleshSack sack || !sack.CanAcceptMore);
        this.FailOn(() => job.targetA.Thing is not Pawn target || target.Dead || !target.Downed);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

        Toil putInSack = new Toil();
        putInSack.initAction = delegate
        {
            FleshSack sack = (FleshSack)job.targetB.Thing;
            Pawn carried = pawn.carryTracker.CarriedThing as Pawn;
            if (carried == null || !sack.CanAcceptMore)
            {
                return;
            }
            if (!sack.InsertPawn(carried))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (carried.Faction == Faction.OfPlayer)
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
