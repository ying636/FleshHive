using RimWorld;
using Verse;

namespace FleshHive;

public class Tentacle_RangeAttackWithWarmup : Tentacle_RangeAttack
{
    public Tentacle_RangeAttackWithWarmup()
    {
    }

    public Tentacle_RangeAttackWithWarmup(TentacleProperties prop) : base(prop)
    {
    }

    public override void Tick()
    {
        base.Tick();
        if (warmupTicks <= 0)
        {
            return;
        }

        if (!AutoAttackEnabled)
        {
            CancelWarmup();
            return;
        }

        Pawn pawn = this.Comp?.Pawn;
        if (pawn == null)
        {
            CancelWarmup();
            return;
        }

        if (warmupTarget == null || warmupTarget.Destroyed || !warmupTarget.Spawned || warmupTarget.MapHeld != pawn.MapHeld || !warmupTarget.HostileTo(pawn))
        {
            CancelWarmup();
            return;
        }

        warmupTicks--;
        this.targetAngle = 90 - (warmupTarget.Position - pawn.Position).AngleFlat;
        if (warmupTicks <= 0)
        {
            LaunchProjectile(pawn, warmupTarget);
            this.cooldown = this.Prop.cooldown * 60f;
            warmupTarget = null;
            this.targetAngle = -1f;
        }
    }

    public override void RareTick()
    {
        if (warmupTarget == null)
        {
            base.RareTick();
        }
    }

    public override void Attack(Thing target)
    {
        if (target == null || warmupTarget != null)
        {
            return;
        }

        warmupTarget = target;
        warmupTicks = Math.Max(1, (int)this.Prop.rotatingTime);
        this.rotateTime = warmupTicks;
        this.targetAngle = 90 - (target.Position - this.Comp.Pawn.Position).AngleFlat;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref warmupTicks, "warmupTicks");
        Scribe_References.Look(ref warmupTarget, "warmupTarget");
    }

    protected override void NotifyAutoAttackDisabled()
    {
        CancelWarmup();
    }

    private void CancelWarmup()
    {
        warmupTicks = 0;
        warmupTarget = null;
        this.rotateTime = 0;
        this.targetAngle = -1f;
    }

    private int warmupTicks;
    private Thing warmupTarget;
}
