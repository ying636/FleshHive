using RimWorld;
using Verse;

namespace FleshHive;

public class Tentacle_Attackable : Tentacle
{
    public Tentacle_Attackable()
    {
    }

    public Tentacle_Attackable(TentacleProperties Prop)
    {
        this.prop = Prop;
    }

    public override void Tick()
    {
        base.Tick();
        if (this.cooldown > 0)
        {
            this.cooldown--;
        }
    }

    public override void RareTick(bool allow)
    {
        if (this.cooldown > 0)
        {
            return;
        }
        if (allow)
        {
            FindAttackTargetAndAttack();
        }
    }

    public virtual void FindAttackTargetAndAttack()
    {
        if (this.Comp.Pawn is { Spawned: true } pawn)
        {
            for (int i = 0; i < GenAdj.AdjacentCellsAndInside.Length; i++)
            {
                IntVec3 c = pawn.Position + GenAdj.AdjacentCellsAndInside[i];
                if (!c.InBounds(pawn.Map))
                {
                    continue;
                }

                if (c.GetFirstPawn(pawn.Map) is { } target && target.HostileTo(pawn))
                {
                    this.Attack(target);
                    return;
                }
            }
        }
    }

    public override void Attack(Thing t)
    {
        Pawn pawn = this.Comp.Pawn;
        this.rotateTime = this.Prop.rotatingTime;
        DamageInfo damageInfo = new DamageInfo(this.Prop.damageDef, this.Prop.damageAmount,
            this.Prop.armorPenetration, -1f, pawn, null, pawn.def, DamageInfo.SourceCategory.ThingOrUnknown,
            t, !pawn.Drafted);
        damageInfo.SetWeaponHediff(this.Comp.parent.def);
        damageInfo.SetAngle((t.Position - pawn.Position).ToVector3());
        t.TakeDamage(damageInfo);
        this.targetAngle = 90 - (t.Position - pawn.Position).AngleFlat;
        this.cooldown = this.Prop.cooldown * 60f;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.cooldown, "cooldown");
    }

    public float cooldown;
}
