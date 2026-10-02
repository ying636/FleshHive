using CombatExtended;
using CombatExtended.Compatibility;
using UnityEngine;
using RimWorld;
using Verse;

namespace FleshHive.CE;

[StaticConstructorOnStartup]
public static class ScarletFieldCE
{
    static ScarletFieldCE()
    {
        BlockerRegistry.RegisterCheckForCollisionBetweenCallback(Intercept);
        BlockerRegistry.RegisterCheckForCollisionCallback((projectile, cell, launcher) => Intercept(projectile, projectile.LastPos, projectile.ExactPosition));
        BlockerRegistry.RegisterImpactSomethingCallback((projectile, launcher) => Intercept(projectile, projectile.LastPos, projectile.ExactPosition));
        BlockerRegistry.RegisterBeforeCollideWithCallback((projectile, target) => Intercept(projectile, projectile.LastPos, projectile.ExactPosition));
        BlockerRegistry.RegisterShieldZonesCallback(ShieldZones);
        BlockerRegistry.RegisterUnsuppresableFromCallback(Unsuppressable);
    }

    public static bool Intercept(ProjectileCE projectile, Vector3 from, Vector3 to)
    {
        if (projectile.Destroyed || !projectile.Spawned || projectile.launcher == null)
        {
            return false;
        }
        object? nearest = null;
        Pawn? nearestOwner = null;
        float nearestProgress = float.MaxValue;
        Vector3 impact = to;
        foreach (var (pawn, field, radius) in ActiveFields(projectile.Map))
        {
            if (pawn.Faction != null
                && !(projectile.launcher.Spawned ? projectile.launcher.HostileTo(pawn.Faction)
                    : projectile.launcher.Faction?.HostileTo(pawn.Faction) == true))
            {
                continue;
            }
            Vector3 center = pawn.Position.ToVector3Shifted();
            Vector3 origin = new(projectile.origin.x, 0f, projectile.origin.y);
            if ((origin - center).Yto0().sqrMagnitude <= radius * radius
                || !TryIntersect(from, to, center, radius, out float progress) || progress >= nearestProgress)
            {
                continue;
            }
            nearest = field;
            nearestOwner = pawn;
            nearestProgress = progress;
            impact = Vector3.Lerp(from, to, progress);
        }
        if (nearest == null || nearestOwner == null || !TwistedFleshUtility.ConsumeTwistedFlesh(nearestOwner, 1))
        {
            return false;
        }
        projectile.InterceptProjectile(nearest, impact, true);
        if (TwistedFleshUtility.GetCurrentTwistedFlesh(nearestOwner) < 1)
        {
            if (nearest is HediffComp_ScarletField hediffField)
            {
                hediffField.Activate();
            }
            else if (nearest is CompScarletField innateField)
            {
                innateField.Activate();
            }
        }
        return true;
    }

    private static bool TryIntersect(Vector3 from, Vector3 to, Vector3 center, float radius, out float progress)
    {
        Vector3 offset = (from - center).Yto0();
        Vector3 direction = (to - from).Yto0();
        float a = direction.sqrMagnitude;
        float b = Vector3.Dot(offset, direction);
        float c = offset.sqrMagnitude - radius * radius;
        progress = 0f;
        if (c <= 0f || a <= 0.000001f || b >= 0f)
        {
            return false;
        }
        float discriminant = b * b - a * c;
        if (discriminant < 0f)
        {
            return false;
        }
        progress = (-b - Mathf.Sqrt(discriminant)) / a;
        return progress >= 0f && progress <= 1f;
    }

    private static IEnumerable<IEnumerable<IntVec3>> ShieldZones(Thing thing)
    {
        if (thing.Map == null)
        {
            yield break;
        }
        foreach (var (pawn, field, radius) in ActiveFields(thing.Map))
        {
            if (!thing.HostileTo(pawn))
            {
                yield return GenRadial.RadialCellsAround(pawn.Position, radius, true)
                    .Where(cell => cell.InBounds(thing.Map));
            }
        }
    }

    private static bool Unsuppressable(Pawn pawn, IntVec3 origin)
    {
        if (!pawn.Spawned)
        {
            return false;
        }
        foreach (var (owner, field, radius) in ActiveFields(pawn.Map))
        {
            if (!pawn.HostileTo(owner)
                && pawn.Position.InHorDistOf(owner.Position, radius)
                && !origin.InHorDistOf(owner.Position, radius))
            {
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<(Pawn owner, object field, float radius)> ActiveFields(Map map)
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            CompScarletField? innate = pawn.TryGetComp<CompScarletField>();
            if (innate?.Active == true)
            {
                yield return (pawn, innate, innate.AreaShieldRadius);
            }
            HediffComp_ScarletField? parasitic = HediffComp_ScarletField.FindOnPawn(pawn);
            if (parasitic?.Active == true)
            {
                yield return (pawn, parasitic, parasitic.Props.areaShieldRadius);
            }
        }
    }
}
