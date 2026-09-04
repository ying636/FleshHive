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

    public ThingDef enemyDropThing;

    public int enemyDropCount;
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
        SpawnEnemyDrop(corpse, faction);
        FleshHiveFleshbeastSpawnUtility.SpawnRandomByPoints(Props.spawnOptions, Props.spawnPointsRange, faction, position, map, Props.spawnRadius);
        SpawnDormantFissionmeld(corpse, position, map, faction);
    }

    private void SpawnEnemyDrop(Corpse corpse, Faction faction)
    {
        if (faction == null || !faction.HostileTo(Faction.OfPlayer)
            || Props.enemyDropThing == null || Props.enemyDropCount <= 0)
        {
            return;
        }

        Thing thing = ThingMaker.MakeThing(Props.enemyDropThing);
        thing.stackCount = Props.enemyDropCount;
        GenPlace.TryPlaceThing(thing, corpse.PositionHeld, corpse.MapHeld, ThingPlaceMode.Near);
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
        CompFissionmeldDormant comp = dormant.TryGetComp<CompFissionmeldDormant>();
        if (comp != null)
        {
            CompFissionmeldState state = corpse.InnerPawn?.TryGetComp<CompFissionmeldState>();
            if (state != null && state.DormantHitPoints > 0)
            {
                dormant.HitPoints = System.Math.Min(state.DormantHitPoints, dormant.MaxHitPoints);
            }
            comp.StoreCorpse(corpse);
        }
        else
        {
            Log.Error("[FleshHive] Dormant fissionmeld is missing CompFissionmeldDormant; destroying the corpse to avoid leaving a duplicate.");
            corpse.Destroy(DestroyMode.Vanish);
        }
    }
}
