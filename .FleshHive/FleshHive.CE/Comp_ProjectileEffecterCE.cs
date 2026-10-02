using CombatExtended;
using Verse;

namespace FleshHive.CE;

public class Comp_ProjectileEffecterCE : ThingComp
{
    private CompProperties_ProjectileEffecterCE Props => (CompProperties_ProjectileEffecterCE)props;

    public override void CompTick()
    {
        base.CompTick();
        if (parent.Spawned && parent is ProjectileCE projectile)
        {
            effecter ??= Props.effecterDef.Spawn(projectile.Position, projectile.intendedTarget.Cell, parent.MapHeld);
            effecter?.EffectTick(parent, parent);
        }
        else
        {
            effecter?.Cleanup();
            effecter = null;
        }
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        effecter?.Cleanup();
        effecter = null;
    }

    private Effecter? effecter;
}
