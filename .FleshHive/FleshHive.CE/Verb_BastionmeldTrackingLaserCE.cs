using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class Verb_BastionmeldTrackingLaserCE : Verb_BastionmeldTrackingLaser
{
    private new VerbProperties_BastionmeldTrackingLaserCE Props => (VerbProperties_BastionmeldTrackingLaserCE)verbProps;

    public override void WarmupComplete()
    {
        ticksUntilDamage = 0;
        clippedLength = float.PositiveInfinity;
        base.WarmupComplete();
    }

    public override void BurstingTick()
    {
        base.BurstingTick();
        if (state != VerbState.Bursting || beamMote is not Mote_BeamIncineratorCE mote)
        {
            return;
        }
        if (--ticksUntilDamage > 0)
        {
            Vector3 visualStart = mote.BeamStart.Yto0();
            Vector3 visualDirection = mote.BeamEnd.Yto0() - visualStart;
            if (visualDirection.magnitude > clippedLength)
            {
                mote.ClipEnd(visualStart + visualDirection.normalized * clippedLength);
            }
            return;
        }
        ticksUntilDamage = Mathf.Max(1, Props.damageIntervalTicks);
        Map map = Caster.Map;
        Vector3 start = mote.BeamStart;
        Vector3 end = mote.BeamEnd;
        start.y = new CollisionVertical(Caster).shotHeight;
        end.y = currentTarget.HasThing ? new CollisionVertical(currentTarget.Thing).shotHeight : 0.5f;
        float fullLength = Vector3.Distance(start.Yto0(), end.Yto0());
        Projectile_TrackingBeamCE projectile = (Projectile_TrackingBeamCE)GenSpawn.Spawn(Props.ceProjectile, Caster.Position, map);
        try
        {
            end = projectile.Trace(Caster, start, end, currentTarget);
            float traceLength = Vector3.Distance(start.Yto0(), end.Yto0());
            clippedLength = traceLength < fullLength - 0.01f ? traceLength : float.PositiveInfinity;
            mote.ClipEnd(end.Yto0());
            ApplyBeamDamage(projectile, start.Yto0(), end.Yto0(), map);
        }
        finally
        {
            if (!projectile.Destroyed)
            {
                projectile.Destroy();
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksUntilDamage, "ceTicksUntilDamage");
        Scribe_Values.Look(ref clippedLength, "ceClippedLength", float.PositiveInfinity);
    }

    private void ApplyBeamDamage(Projectile_TrackingBeamCE projectile, Vector3 start, Vector3 end, Map map)
    {
        Vector3 direction = end - start;
        float lengthSquared = direction.sqrMagnitude;
        HashSet<Thing> victims = new();
        foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start.ToIntVec3(), end.ToIntVec3()).Append(end.ToIntVec3()))
        {
            if (!cell.InBounds(map))
            {
                continue;
            }
            if (!cell.InHorDistOf(Caster.Position, 3f) && Rand.Chance(Props.ceFireChancePerDamageTick))
            {
                FireUtility.TryStartFireIn(cell, map, Props.fireSize, Caster);
            }
            foreach (Thing victim in GenRadial.RadialDistinctThingsAround(cell, map, Props.beamHitRadius, true).ToArray())
            {
                if (victim == Caster || victim.Destroyed || !victim.Spawned || victim is ProjectileCE or Mote or Filth
                    || !victims.Add(victim) || victim is Pawn && !Props.damagePawnsAlongBeam
                    || victim is not Pawn && !Props.damageThingsAlongBeam
                    || !victim.HostileTo(Caster) && (victim.Faction == Caster.Faction ? !Props.allowFriendlyFire : !Props.allowNeutralFire))
                {
                    continue;
                }
                Vector3 position = victim.DrawPos.Yto0();
                float progress = lengthSquared > 0.001f ? Vector3.Dot(position - start, direction) / lengthSquared : 0f;
                Vector3 closest = start + direction * Mathf.Clamp01(progress);
                if (victim != projectile.HitThing && (progress < 0f || progress > 1f
                    || (position - closest).sqrMagnitude > Props.beamHitRadius * Props.beamHitRadius))
                {
                    continue;
                }
                DamageInfo damage = projectile.DamageInfo;
                damage.SetAmount(Props.damageAmount * (victim is Building ? Props.buildingDamageMultiplier : 1f));
                damage.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                victim.TakeDamage(damage);
            }
        }
    }

    private int ticksUntilDamage;
    private float clippedLength = float.PositiveInfinity;
}
