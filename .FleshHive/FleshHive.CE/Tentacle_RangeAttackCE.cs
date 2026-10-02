using Verse;

namespace FleshHive.CE;

public class Tentacle_RangeAttackCE : Tentacle_RangeAttack
{
    public Tentacle_RangeAttackCE()
    {
    }

    public Tentacle_RangeAttackCE(TentacleProperties props) : base(props)
    {
    }

    protected override void LaunchProjectile(Pawn pawn, Thing target)
    {
        new Verb_ShootFleshTentacleCE().Fire(pawn, target, Prop.projectile, Prop.range);
    }
}
