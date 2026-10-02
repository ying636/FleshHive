using CombatExtended;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive.CE;

public class JobDriver_MountedWeaponReloadCE : CombatExtended.JobDriver_Reload
{
    private ThingWithComps? Weapon => job.targetB.Thing as ThingWithComps;

    public override IEnumerable<Toil> MakeNewToils()
    {
        ThingWithComps? weapon = Weapon;
        if (weapon == null || !IsMounted(weapon))
        {
            foreach (Toil toil in base.MakeNewToils())
            {
                yield return toil;
            }
            yield break;
        }
        CompAmmoUser? ammo = weapon.TryGetComp<CompAmmoUser>();
        if (ammo == null || job.targetA.Pawn != pawn)
        {
            Log.Error($"[FleshHive] Invalid mounted weapon reload: {pawn}/{weapon}.");
            EndJobWith(JobCondition.Incompletable);
            yield break;
        }
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOn(() => !IsMounted(weapon) || !ammo.HasAndUsesAmmoOrMagazine
            || ammo.UseAmmo && !ammo.TryFindAmmoInInventory(out _));
        float reloadTime = CE_Utility.GetPrimaryVerbPropsCE(weapon)?.useEquipmentStatValues == true
            ? weapon.GetStatValue(CE_StatDefOf.ReloadTime) : ammo.ReloadTime;
        int duration = Mathf.Max(1, Mathf.CeilToInt(reloadTime.SecondsToTicks()
            * weapon.GetStatValue(CE_StatDefOf.CE_RangedWeapon_ReloadFactor) / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed)));
        Toil wait = Toils_General.Wait(duration);
        wait.initAction = () => pawn.pather.StopDead();
        yield return wait.WithProgressBarToilDelay(TargetIndex.A);
        Toil reload = ToilMaker.MakeToil();
        reload.defaultCompleteMode = ToilCompleteMode.Instant;
        reload.initAction = () =>
        {
            ammo.DropCasing(ammo.Props.magazineSize);
            if (!ammo.UseAmmo || ammo.TryFindAmmoInInventory(out _))
            {
                Thing? magazine = null;
                if (ammo.UseAmmo)
                {
                    ammo.TryFindAmmoInInventory(out magazine);
                }
                ammo.LoadAmmo(magazine, true);
                while (!ammo.Props.reloadOneAtATime && !ammo.FullMagazine && ammo.TryFindAmmoInInventory(out magazine))
                {
                    ammo.LoadAmmo(magazine);
                }
            }
        };
        yield return reload;
        yield return Toils_Jump.JumpIf(wait, () => ammo.Props.reloadOneAtATime && !ammo.FullMagazine
            && (!ammo.UseAmmo || ammo.TryFindAmmoInInventory(out _)));
    }

    private bool IsMounted(ThingWithComps weapon)
    {
        return HediffComp_ParasitismWeaponMounts.GetAll(pawn)
            .Any(comp => comp.WeaponMounts.Any(mount => mount.MountedWeapon == weapon))
            || pawn.TryGetComp<CompUnitMultiTurret>()?.Turrets.Any(turret => turret.MountedWeapon == weapon) == true;
    }
}
