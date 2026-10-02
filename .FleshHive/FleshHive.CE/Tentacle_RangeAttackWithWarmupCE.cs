using Verse;

namespace FleshHive.CE;

public class Tentacle_RangeAttackWithWarmupCE : Tentacle_RangeAttackWithWarmup
{
    public Tentacle_RangeAttackWithWarmupCE()
    {
    }

    public Tentacle_RangeAttackWithWarmupCE(TentacleProperties props) : base(props)
    {
    }

    protected override void LaunchProjectile(Pawn pawn, Thing target)
    {
        new Verb_ShootFleshTentacleCE().Fire(pawn, target, Prop.projectile, Prop.range);
    }
}
