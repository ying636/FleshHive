using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_FissionmeldDormancy : DeathActionProperties
{
    public DeathActionProperties_FissionmeldDormancy()
    {
        workerClass = typeof(DeathActionWorker_FissionmeldDormancy);
    }

    public ThingDef dormantThing;

    public List<PawnKindDef> spawnOptions;

    public IntRange spawnPointsRange = new IntRange(100, 300);

    public int spawnRadius = 5;
}

public class DeathActionWorker_FissionmeldDormancy : DeathActionWorker
{
    public DeathActionProperties_FissionmeldDormancy Props => (DeathActionProperties_FissionmeldDormancy)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        if (corpse?.MapHeld == null)
        {
            return;
        }

        Map map = corpse.MapHeld;
        IntVec3 position = corpse.PositionHeld;
        Faction faction = corpse.InnerPawn?.Faction;
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(Props.spawnOptions, Props.spawnPointsRange, faction, position, map, Props.spawnRadius);
        SpawnDormantFissionmeld(corpse, position, map, faction);
    }

    private void SpawnDormantFissionmeld(Corpse corpse, IntVec3 position, Map map, Faction faction)
    {
        if (Props.dormantThing == null)
        {
            return;
        }

        Thing dormant = ThingMaker.MakeThing(Props.dormantThing);
        GenSpawn.Spawn(dormant, position, map, WipeMode.VanishOrMoveAside);
        dormant.SetFaction(faction);
        if (!corpse.Destroyed)
        {
            corpse.Destroy(DestroyMode.Vanish);
        }
    }
}
