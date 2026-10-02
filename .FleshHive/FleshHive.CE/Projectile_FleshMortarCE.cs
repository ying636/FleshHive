using CombatExtended;
using RimWorld;
using Verse;

namespace FleshHive.CE;

public class Projectile_FleshMortarCE : ProjectileCE_Explosive
{
    public override void Impact(Thing hitThing)
    {
        Map map = Map;
        IntVec3 position = Position;
        Thing source = launcher;
        base.Impact(hitThing);
        SpawnFleshbeasts(map, position, source);
    }

    public override void Tick()
    {
        Map map = Map;
        IntVec3 position = Position;
        Thing source = launcher;
        bool detonating = ticksToDetonation == 1;
        base.Tick();
        if (detonating)
        {
            SpawnFleshbeasts(map, position, source);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref spawnedFleshbeasts, "spawnedFleshbeasts");
    }

    private void SpawnFleshbeasts(Map map, IntVec3 position, Thing source)
    {
        if (spawnedFleshbeasts || !Destroyed || source is not Pawn pawn)
        {
            return;
        }

        spawnedFleshbeasts = true;
        FleshHiveFleshbeastSpawnUtility.SpawnRandomBySize(FleshBeastSize.Small, 3, pawn.Faction,
            position, map, 5, pawn, false);
        FleshbeastUtility.MeatSplatter(5, position, map, FleshbeastUtility.ExplosionSizeFor(pawn));
        FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_TwistedFlesh, 1, FilthSourceFlags.None, true);
    }

    private bool spawnedFleshbeasts;
}
