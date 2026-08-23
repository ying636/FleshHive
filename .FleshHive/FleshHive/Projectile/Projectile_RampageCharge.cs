using HiveCreatureFramework;
using Verse;

namespace FleshHive;

public class Projectile_RampageCharge : Projectile_Charge
{
    protected override bool CanDamageTarget(Thing target)
    {
        return !IsCurrentlyFlyingThing(target) && base.CanDamageTarget(target);
    }

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (IsCurrentlyFlyingThing(hitThing))
        {
            return;
        }

        base.Impact(hitThing, blockedByShield);
    }

    private static bool IsCurrentlyFlyingThing(Thing thing)
    {
        return thing is Pawn pawn && pawn.Flying;
    }
}
