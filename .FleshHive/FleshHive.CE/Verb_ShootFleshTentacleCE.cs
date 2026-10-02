using CombatExtended;
using Verse;

namespace FleshHive.CE;

public class Verb_ShootFleshTentacleCE : Verb_ShootCE
{
    public bool Fire(Pawn pawn, LocalTargetInfo target, ThingDef projectile, float range, bool avoidFriendlyFire = false)
    {
        caster = pawn;
        verbTracker = pawn.VerbTracker;
        verbProps = new VerbPropertiesCE
        {
            verbClass = typeof(Verb_ShootFleshTentacleCE),
            defaultProjectile = projectile,
            range = range,
            warmupTime = 0f,
            burstShotCount = 1,
            ai_IsWeapon = false,
            recoilAmount = 0f
        };
        currentTarget = target;
        preventFriendlyFire = avoidFriendlyFire;
        nonInterruptingSelfCast = true;
        return TryCastShot();
    }

    protected override ProjectileCE SpawnProjectile()
    {
        ProjectileCE projectile = base.SpawnProjectile();
        if (projectile is Projectile_LiquidCE liquid)
        {
            liquid.avoidFriendlyFire = preventFriendlyFire;
        }
        return projectile;
    }
}
