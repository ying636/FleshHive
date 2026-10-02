using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Projectile_LiquidCE : BulletCE
{
    public override void Impact(Thing hitThing)
    {
        Map map = Map;
        IntVec3 position = Position;
        DamageInfo damage = DamageInfo;
        HashSet<Thing> victims = new();
        if (hitThing != null)
        {
            victims.Add(hitThing);
        }
        base.Impact(avoidFriendlyFire && hitThing is Pawn && !hitThing.HostileTo(launcher) ? null : hitThing);
        for (int i = -1; i < def.projectile.numExtraHitCells; i++)
        {
            IntVec3 cell = i < 0 ? position : position + GenAdj.AdjacentCellsAndInside[i];
            if (!cell.InBounds(map))
            {
                continue;
            }
            if (def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0
                && Rand.Chance(def.projectile.filthChance) && !cell.Filled(map))
            {
                FilthMaker.TryMakeFilth(cell, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
            }
            foreach (Thing victim in cell.GetThingList(map).ToArray())
            {
                if (victim is Mote or Filth || victim == this || !victims.Add(victim)
                    || avoidFriendlyFire && victim is Pawn && !victim.HostileTo(launcher))
                {
                    continue;
                }
                victim.TakeDamage(damage);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref avoidFriendlyFire, "avoidFriendlyFire");
    }

    public bool avoidFriendlyFire;
}
