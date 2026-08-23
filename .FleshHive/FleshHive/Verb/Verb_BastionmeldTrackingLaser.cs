using HiveCreatureFramework;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive;

public class Verb_BastionmeldTrackingLaser : Verb_TrackingLaser
{
    private VerbProperties_BastionmeldTrackingLaser FHProps => (VerbProperties_BastionmeldTrackingLaser)this.verbProps;

    public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
    {
        bool started = base.TryStartCastOn(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        if (started)
        {
            this.targetMote = this.MakeTargetMote(this.currentTarget);
        }

        return started;
    }

    public override void WarmupComplete()
    {
        this.targetMote = this.MakeTargetMote(this.currentTarget);
        base.WarmupComplete();
    }

    public override void BurstingTick()
    {
        base.BurstingTick();
        if (this.state == VerbState.Bursting)
        {
            this.MaintainTargetMote();
        }
        else
        {
            this.DestroyTargetMote();
        }
    }

    public override void Reset()
    {
        base.Reset();
        this.DestroyTargetMote();
    }

    private Mote MakeTargetMote(LocalTargetInfo target)
    {
        this.DestroyTargetMote();
        ThingDef moteDef = this.FHProps.persistentTargetMote;
        if (moteDef == null || this.Caster == null || this.Caster.Map == null || !target.IsValid)
        {
            return null;
        }

        Mote mote = MoteMaker.MakeStaticMote(this.GetTargetPosition(target), this.Caster.Map, moteDef, 1f, makeOffscreen: true);
        if (mote != null)
        {
            mote.exactRotation = this.GetTargetAngle(target);
        }

        return mote;
    }

    private void MaintainTargetMote()
    {
        if (this.targetMote == null || this.targetMote.Destroyed)
        {
            this.targetMote = this.MakeTargetMote(this.currentTarget);
        }

        if (this.targetMote == null || this.targetMote.Destroyed || !this.currentTarget.IsValid)
        {
            return;
        }

        this.targetMote.exactPosition = this.GetTargetPosition(this.currentTarget);
        this.targetMote.exactRotation = this.GetTargetAngle(this.currentTarget);
        this.targetMote.Maintain();
    }

    private Vector3 GetTargetPosition(LocalTargetInfo target)
    {
        if (target.Thing != null && target.Thing.Spawned)
        {
            return target.Thing.DrawPos;
        }

        return target.Cell.ToVector3Shifted();
    }

    private float GetTargetAngle(LocalTargetInfo target)
    {
        Vector3 direction = this.GetTargetPosition(target) - this.Caster.DrawPos;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.ToAngleFlat() : 0f;
    }

    private void DestroyTargetMote()
    {
        if (this.targetMote != null && !this.targetMote.Destroyed)
        {
            this.targetMote.Destroy(DestroyMode.Vanish);
        }

        this.targetMote = null;
    }

    private Mote targetMote;
}
