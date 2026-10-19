using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Projectile_AcidSpitCE : ProjectileCE_Explosive
{
    public override void Impact(Thing hitThing)
    {
        Map map = Map;
        IntVec3 position = Position;
        base.Impact(hitThing);
        if (Destroyed && map != null && def.projectile.explosionEffect is { } effect
            && def.projectile.explosionEffectLifetimeTicks > 0)
        {
            map.effecterMaintainer.AddEffecterToMaintain(effect.Spawn(), position,
                def.projectile.explosionEffectLifetimeTicks);
        }
        if (Destroyed && map != null && def.projectile.filth is { } filth)
        {
            FilthMaker.TryMakeFilth(position, map, filth, def.projectile.filthCount.RandomInRange);
        }
    }
}
