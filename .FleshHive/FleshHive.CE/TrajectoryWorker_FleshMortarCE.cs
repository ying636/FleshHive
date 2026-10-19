using System.Collections.Generic;
using CombatExtended;
using UnityEngine;

namespace FleshHive.CE;

public class TrajectoryWorker_FleshMortarCE : LerpedTrajectoryWorker
{
    public override float GetFlightTime(float shotAngle, float shotSpeed, float gravityPerWidth, float shotHeight)
    {
        return Mathf.Max(0.001f, DistanceTraveled(shotHeight, shotSpeed, shotAngle, gravityPerWidth)
            / Mathf.Max(0.001f, shotSpeed * 0.6f));
    }

    public override Vector3 MoveForward(ProjectileCE projectile)
    {
        return PositionAtTick(projectile, projectile.FlightTicks);
    }

    public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int tickCount)
    {
        int count = Mathf.Min(tickCount, projectile.ticksToImpact);
        for (int i = 0; i < count; i++)
        {
            yield return PositionAtTick(projectile, projectile.FlightTicks + i);
        }
    }

    private Vector3 PositionAtTick(ProjectileCE projectile, int tick)
    {
        float fraction = Mathf.Clamp01(tick / projectile.startingTicksToImpact);
        Vector2 position = Vector2.Lerp(projectile.origin, projectile.Destination, fraction);
        float distance = Vector2.Distance(projectile.origin, projectile.Destination);
        float height = Mathf.Min(projectile.def.projectile.arcHeightFactor, distance * 0.2f)
            * 4f * fraction * (1f - fraction) / 0.84f;
        return new Vector3(position.x, Mathf.Lerp(projectile.shotHeight, 0f, fraction) + height, position.y);
    }
}
