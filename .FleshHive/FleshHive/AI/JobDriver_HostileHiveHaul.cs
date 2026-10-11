using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_HostileHiveHaul : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDespawnedOrNull(TargetIndex.B);
        this.FailOn(() => TargetThingB is not Building_HostileFleshStore store || !store.CanReceive
            || store.Faction != pawn.Faction
            || pawn.Map.GetComponent<MapComponent_HostileFleshHive>().GetColony(store)?.Lord.Threatened != false);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return Toils_General.Do(() =>
        {
            Building_HostileFleshStore store = (Building_HostileFleshStore)TargetThingB;
            if (pawn.carryTracker.CarriedThing is not Corpse corpse || !store.Receive(corpse))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
        });
    }
}
