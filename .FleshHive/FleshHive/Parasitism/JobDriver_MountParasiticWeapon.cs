using Verse;
using Verse.AI;
using Verse.Sound;

namespace FleshHive;

public class JobDriver_MountParasiticWeapon : JobDriver
{
    private Thing TargetWeapon => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
        Toil mount = ToilMaker.MakeToil(nameof(MakeNewToils));
        mount.initAction = delegate
        {
            ThingWithComps weapon = TargetWeapon as ThingWithComps;
            HediffComp_ParasitismWeaponMounts comp = HediffComp_ParasitismWeaponMounts.GetFirstWithEmptyMount(pawn);
            if (weapon == null || comp == null || !HediffComp_ParasitismWeaponMounts.CanMountWeapon(weapon))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (weapon.def.stackLimit > 1 && weapon.stackCount > 1)
            {
                weapon = (ThingWithComps)weapon.SplitOff(1);
            }
            else
            {
                weapon.DeSpawn();
            }

            if (!comp.MountWeapon(weapon))
            {
                GenPlace.TryPlaceThing(weapon, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            weapon.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
        };
        mount.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return mount;
    }
}
