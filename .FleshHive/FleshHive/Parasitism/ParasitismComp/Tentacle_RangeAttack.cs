using RimWorld;
using Verse;

namespace FleshHive;

public class TentacleProperties_RangeAttack : TentacleProperties
{
    public ThingDef projectile;
    public float range;
}

public class Tentacle_RangeAttack : Tentacle_Attackable
{
    public Tentacle_RangeAttack()
    {
    }

    public Tentacle_RangeAttack(TentacleProperties Prop)
    {
        this.prop = Prop;
    }

    public new TentacleProperties_RangeAttack Prop => (TentacleProperties_RangeAttack)base.Prop;

    public override void Attack(Thing t)
    {
        Pawn pawn = this.Comp.parent.pawn;
        LaunchProjectile(pawn, t);
        this.rotateTime = this.Prop.rotatingTime;
        this.targetAngle = 90 - (t.Position - pawn.Position).AngleFlat;
        this.cooldown = this.Prop.cooldown * 60f;
    }

    protected virtual void LaunchProjectile(Pawn pawn, Thing target)
    {
        Projectile p = (Projectile)ThingMaker.MakeThing(this.Prop.projectile);
        GenSpawn.Spawn(p, (pawn.Position.ToVector3() + this.drawPosOffset).ToIntVec3(), pawn.Map);
        p.Launch(pawn, target, target, ProjectileHitFlags.All);
    }

    public override void FindAttackTargetAndAttack()
    {
        float range = this.Prop.range;
        Pawn pawn = this.Comp.parent.pawn;
        Map map = this.Comp.parent.pawn.Map;
        List<Pawn> targets = new List<Pawn>();
        if (range <= 16)
        {
            foreach (var intVec3 in GenRadial.RadialCellsAround(this.Comp.parent.pawn.Position, range, false))
            {
                if (!intVec3.InBounds(map))
                {
                    continue;
                }

                if (intVec3.GetFirstPawn(map) is { } target && target.HostileTo(pawn)
                    && GenSight.LineOfSight(pawn.Position, intVec3, map))
                {
                    targets.Add(target);
                }
            }
        }
        else
        {
            foreach (var p in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.HostileTo(p) && GenSight.LineOfSight(pawn.Position, p.Position, map) && p.Position.DistanceTo(pawn.Position) <= range)
                {
                    targets.Add(p);
                }
            }
        }
        if (targets.Any())
        {
            this.Attack(targets.RandomElement());
        }
    }
}
