using CombatExtended;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class Projectile_TrackingBeamCE : ProjectileCE
{
    public override float DamageAmount => def.projectile.GetDamageAmount(null);

    public override float PenetrationAmount => ((ProjectilePropertiesCE)def.projectile).armorPenetrationBlunt;

    public Thing? HitThing { get; private set; }

    public Vector3 Trace(Thing source, Vector3 start, Vector3 end, LocalTargetInfo target)
    {
        launcher = source;
        origin = new Vector2(start.x, start.z);
        OriginIV3 = start.ToIntVec3();
        intendedTarget = target;
        shotRotation = (end - start).AngleFlat();
        canTargetSelf = false;
        ignoreRoof = true;
        ExactPosition = start;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) * 2f));
        for (int i = 1; i <= steps; i++)
        {
            LastPos = ExactPosition;
            ExactPosition = Vector3.Lerp(start, end, (float)i / steps);
            FlightTicks++;
            if (CheckForCollisionBetween())
            {
                break;
            }
        }
        return ExactPosition;
    }

    public override void Impact(Thing hitThing)
    {
        HitThing = hitThing;
        landed = true;
    }
}
