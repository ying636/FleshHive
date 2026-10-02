using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class UnitTurret_PlayerEmptyWeaponMountCE : UnitTurret_PlayerEmptyWeaponMount
{
    public override void MakeGun()
    {
        bool hadWeapon = HasWeapon;
        base.MakeGun();
        if (!hadWeapon && HasWeapon && Thing.Faction?.IsPlayer == false)
        {
            MountedWeapon.TryGetComp<CompAmmoUser>()?.ResetAmmoCount();
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        CompAmmoUser? ammo = MountedWeapon?.TryGetComp<CompAmmoUser>();
        if (ammo != null)
        {
            foreach (Gizmo gizmo in ammo.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (Thing is Pawn pawn && pawn.Faction == Faction.OfPlayer
                && !pawn.IsColonistPlayerControlled && !pawn.IsColonyMech)
            {
                yield return new GizmoAmmoStatus { compAmmo = ammo };
                yield return new Command_Reload
                {
                    compAmmo = ammo,
                    action = ammo.TryStartReload,
                    defaultLabel = ammo.HasMagazine ? "CE_ReloadLabel".Translate() : "",
                    defaultDesc = "CE_ReloadDesc".Translate(),
                    icon = ammo.CurrentAmmo == null ? ContentFinder<Texture2D>.Get("UI/Buttons/Reload") : ammo.SelectedAmmo.IconTexture(),
                    tutorTag = ammo.HasMagazine ? "CE_Reload" : "CE_ReloadNoMag"
                };
            }
        }
    }

    protected override void TryStartAttack(LocalTargetInfo target)
    {
        if (AttackVerb is not Verb_MeleeAttackCE)
        {
            base.TryStartAttack(target);
            return;
        }
        if (TryStartCastWithoutPawnStance(target))
        {
            lastAttackTargetTick = Find.TickManager.TicksGame;
            lastAttackedTarget = target;
            burstCooldownTicksLeft = AttackVerb.verbProps.AdjustedCooldownTicks(AttackVerb, Thing as Pawn);
        }
        ResetCurrentTarget();
    }
}
