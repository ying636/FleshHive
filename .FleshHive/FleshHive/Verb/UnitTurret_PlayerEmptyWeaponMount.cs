using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class UnitTurret_PlayerEmptyWeaponMount : UnitTurret_WeaponMount
{
    public override void MakeGun()
    {
        if (this.Thing.Faction?.IsPlayer == true)
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
        }

        if (this.aimingChargeMote == null)
        {
            return;
        }

        Vector3 direction = this.currentTarget.CenterVector3 - this.Thing.DrawPos;
        direction.y = 0f;
        direction.Normalize();

        this.aimingChargeMote.paused = this.Thing is Pawn pawn && pawn.stances.stunner.Stunned;
        this.aimingChargeMote.exactRotation = direction.AngleFlat();
        this.aimingChargeMote.exactPosition = this.GetAimingChargePosition(attackVerb, direction);
        this.aimingChargeMote.Maintain();
    }

    private Vector3 GetAimingChargePosition(Verb attackVerb, Vector3 direction)
    {
        Vector3 position = this.Thing.Position.ToVector3Shifted();
        if (attackVerb.verbProps is not VerbProperties_TrackingLaser trackingLaserProps
            || trackingLaserProps.startDrawData == null)
        {
            return position + direction * attackVerb.verbProps.aimingChargeMoteOffset;
        }

        Rot4 drawRotation = this.GetTrackingLaserDrawRotation(trackingLaserProps);
        return position
               + trackingLaserProps.startDrawData.OffsetForRot(drawRotation)
               + direction * attackVerb.verbProps.beamStartOffset;
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
}
