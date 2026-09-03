using RimWorld;
using Verse;

namespace FleshHive;

public class Projectile_Explosive_ParasitismSpit : Projectile_Explosive
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = this.Map;
        IntVec3 position = this.Position;
        Pawn launcher = (Pawn)this.Launcher;
        base.Impact(hitThing, blockedByShield);

        FleshHiveFleshbeastSpawnUtility.SpawnRandomBySize(FleshBeastSize.Small, 3, launcher.Faction, position, map, 5, launcher, false);
        FleshbeastUtility.MeatSplatter(5, position, map, FleshbeastUtility.ExplosionSizeFor(launcher));
        FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_TwistedFlesh, 1, FilthSourceFlags.None, true);
    }
}
