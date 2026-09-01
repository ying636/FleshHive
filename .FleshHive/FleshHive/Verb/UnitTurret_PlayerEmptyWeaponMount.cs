using System;
using System.Reflection;
using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class UnitTurret_PlayerEmptyWeaponMount : UnitTurret_WeaponMount
{
    public override void MakeGun()
    {
        if (this.Thing.Faction == null
            || (this.Thing.Faction.IsPlayer == true
                && this.Props.turretDef != FleshHiveDefOf.FH_Gun_BastionmeldTrackingLaser))
        {
            return;
        }

        if (this.gun != null || this.Props.turretDef == null)
        {
            return;
        }

        this.gun = ThingMaker.MakeThing(this.Props.turretDef);
        this.UpdateWeaponVerbs();
        this.comp?.NotifyTurretVisualsDirty();
    }

    public override void Tick()
    {
        base.Tick();
        this.UpdateAimingChargeMote();
    }

    protected override void InterruptCurrentAttack()
    {
        base.InterruptCurrentAttack();
        this.DestroyAimingChargeMote();
    }

    protected override void TryStartAttack(LocalTargetInfo target)
    {
        if (this.AttackVerb?.verbProps.IsMeleeAttack != true)
        {
            base.TryStartAttack(target);
            return;
        }

        Verb verb = this.AttackVerb;
        if (verb is not Verb_MeleeAttack meleeVerb
            || verb.Caster is not Pawn pawn
            || !target.IsValid
            || !verb.CanHitTarget(target))
        {
            this.ResetCurrentTarget();
            return;
        }

        ApplyMeleeDamageDelegate(meleeVerb, target);
        pawn.Notify_UsedVerb(pawn, verb);
        pawn.health?.Notify_UsedVerb(verb, target);
        verb.EquipmentSource?.Notify_UsedWeapon(pawn);
        pawn.Drawer.Notify_MeleeAttackOn(target.Thing);
        this.lastAttackTargetTick = Find.TickManager.TicksGame;
        this.lastAttackedTarget = target;
        verb.castCompleteCallback?.Invoke();
        this.burstCooldownTicksLeft = Mathf.Max(1, verb.verbProps.AdjustedCooldown(verb, pawn).SecondsToTicks());
        this.ResetCurrentTarget();
    }

    private void UpdateAimingChargeMote()
    {
        Verb attackVerb = this.AttackVerb;
        if (this.burstWarmupTicksLeft <= 0
            || !this.currentTarget.IsValid
            || attackVerb?.verbProps.aimingChargeMote == null
            || !this.Thing.Spawned)
        {
            this.DestroyAimingChargeMote();
            return;
        }

        if (this.aimingChargeMote == null || this.aimingChargeMote.Destroyed)
        {
            this.aimingChargeMote = MoteMaker.MakeStaticMote(
                this.Thing.DrawPos,
                this.Thing.Map,
                attackVerb.verbProps.aimingChargeMote,
                1f,
                makeOffscreen: true);
            if (this.aimingChargeMote is Mote_BastionmeldCharge chargeMote)
            {
                chargeMote.UpdatePositionAndRotationAction = this.UpdateAimingChargeMoteDrawPosition;
            }
        }

        if (this.aimingChargeMote == null)
        {
            return;
        }

        Vector3 direction = this.currentTarget.CenterVector3 - this.Thing.DrawPos;
        direction.y = 0f;
        direction.Normalize();

        this.aimingChargeMote.paused = this.Thing is Pawn pawn && pawn.stances.stunner.Stunned;
        if (this.aimingChargeMote is not Mote_BastionmeldCharge)
        {
            this.aimingChargeMote.exactRotation = direction.AngleFlat();
            this.aimingChargeMote.exactPosition = this.GetAimingChargePosition(attackVerb, direction);
        }
        this.aimingChargeMote.Maintain();
    }

    private void UpdateAimingChargeMoteDrawPosition(Mote_BastionmeldCharge mote)
    {
        Verb attackVerb = this.AttackVerb;
        if (attackVerb == null || !this.currentTarget.IsValid || !this.Thing.Spawned)
        {
            return;
        }

        Vector3 direction = this.currentTarget.CenterVector3 - this.Thing.DrawPos;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        direction.Normalize();
        mote.exactRotation = direction.AngleFlat();
        mote.exactPosition = this.GetAimingChargePosition(attackVerb, direction);
    }

    private Vector3 GetAimingChargePosition(Verb attackVerb, Vector3 direction)
    {
        Vector3 position = this.Thing.DrawPos;
        if (attackVerb.verbProps is not VerbProperties_TrackingLaser trackingLaserProps
            || trackingLaserProps.startDrawData == null)
        {
            return position + direction * attackVerb.verbProps.aimingChargeMoteOffset;
        }

        Rot4 drawRotation = this.GetTrackingLaserDrawRotation(trackingLaserProps);
        return position
               + trackingLaserProps.startDrawData.OffsetForRot(drawRotation)
               + direction * (attackVerb.verbProps.beamStartOffset + AimingChargeOutwardOffset);
    }

    private Rot4 GetTrackingLaserDrawRotation(VerbProperties_TrackingLaser trackingLaserProps)
    {
        if (this.Thing is not Pawn pawn)
        {
            return this.Thing.Rotation;
        }

        if (pawn.GetPosture() != PawnPosture.Standing)
        {
            return pawn.Drawer.renderer.LayingFacing();
        }

        if (trackingLaserProps.realTimeFaceTarget)
        {
            return pawn.Rotation;
        }

        return pawn.Drafted ? Rot4.South : pawn.Rotation;
    }

    private void DestroyAimingChargeMote()
    {
        if (this.aimingChargeMote != null && !this.aimingChargeMote.Destroyed)
        {
            this.aimingChargeMote.Destroy(DestroyMode.Vanish);
        }

        this.aimingChargeMote = null;
    }

    private Mote aimingChargeMote;
    private static readonly Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult> ApplyMeleeDamageDelegate =
        (Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>)typeof(Verb_MeleeAttack)
            .GetMethod("ApplyMeleeDamageToTarget", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<Verb_MeleeAttack, LocalTargetInfo, DamageWorker.DamageResult>));
    private const float AimingChargeOutwardOffset = 0.15f;
}
