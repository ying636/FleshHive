using CombatExtended;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;

namespace FleshHive.CE;

public class Projectile_FleshExplosionCE : ProjectileCE_Explosive
{
    public override void Impact(Thing hitThing)
    {
        Map map = Map;
        Vector3 impact = ExactPosition;
        if (hitThing is Pawn)
        {
            impact.x = hitThing.DrawPos.x;
            impact.z = hitThing.DrawPos.z;
        }
        IntVec3 position = impact.ToIntVec3();
        if (map == null || !position.InBounds(map))
        {
            Destroy();
            return;
        }
        ProjectilePropertiesCE properties = Props;
        if (!properties.detonateInSpace && position.GetTerrain(map).defName == "Space")
        {
            Destroy();
            return;
        }
        GenExplosionCE.DoExplosion(position, map, properties.explosionRadius, DamageDef, launcher,
            Mathf.FloorToInt(DamageAmount), properties.GetExplosionArmorPenetration(),
            explosionSound: properties.soundExplode, weapon: equipmentDef, projectile: def,
            postExplosionSpawnThingDef: properties.postExplosionSpawnThingDef,
            postExplosionSpawnChance: properties.postExplosionSpawnChance,
            postExplosionSpawnThingCount: properties.postExplosionSpawnThingCount,
            postExplosionGasType: properties.postExplosionGasType,
            applyDamageToExplosionCellsNeighbors: properties.applyDamageToExplosionCellsNeighbors,
            preExplosionSpawnThingDef: properties.preExplosionSpawnThingDef,
            preExplosionSpawnChance: properties.preExplosionSpawnChance,
            preExplosionSpawnThingCount: properties.preExplosionSpawnThingCount,
            chanceToStartFire: properties.explosionChanceToStartFire, damageFalloff: properties.explosionDamageFalloff,
            doVisualEffects: properties.doExplosionVFX,
            postExplosionSpawnThingDefWater: properties.postExplosionSpawnThingDefWater,
            screenShakeFactor: properties.screenShakeFactor,
            postExplosionSpawnSingleThingDef: properties.postExplosionSpawnSingleThingDef,
            preExplosionSpawnSingleThingDef: properties.preExplosionSpawnSingleThingDef, height: impact.y);
        if (properties.explosionEffect is { } effect)
        {
            Effecter effecter = effect.Spawn();
            effecter.Trigger(new TargetInfo(position, map), new TargetInfo(position, map));
            if (properties.explosionEffectLifetimeTicks > 0)
            {
                map.effecterMaintainer.AddEffecterToMaintain(effecter, position, properties.explosionEffectLifetimeTicks);
            }
            else
            {
                effecter.Cleanup();
            }
        }
        if (properties.filth is { } filth)
        {
            FilthMaker.TryMakeFilth(position, map, filth, properties.filthCount.RandomInRange);
        }
        float dangerRadius = properties.explosionRadius + (properties.applyDamageToExplosionCellsNeighbors ? 1.5f : 0f);
        if (impact.y < 3f && properties.suppressionFactor > 0f)
        {
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(position, map, dangerRadius + 3f, true))
            {
                if (thing is Pawn pawn)
                {
                    ApplySuppression(pawn);
                }
            }
        }
        if (properties.dangerFactor > 0f)
        {
            DangerTracker.Notify_DangerRadiusAt(position, dangerRadius, DamageAmount * properties.dangerFactor);
        }
        Destroy();
    }
}
