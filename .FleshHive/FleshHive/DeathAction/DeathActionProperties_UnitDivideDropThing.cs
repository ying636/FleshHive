using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class DeathActionProperties_UnitDivideDropThing : DeathActionProperties_UnitDivide
{
    public DeathActionProperties_UnitDivideDropThing()
    {
        workerClass = typeof(DeathActionWorker_UnitDivideDropThing);
    }

    public int dropCount = 1;

    public ThingDef dropThing;

    public int enemyDropCount;

    public ThingDef enemyDropThing;
}

public class DeathActionWorker_UnitDivideDropThing : DeathActionWorker_UnitDivide
{
    public new DeathActionProperties_UnitDivideDropThing Props => (DeathActionProperties_UnitDivideDropThing)props;

    public override void PawnDied(Corpse corpse, Lord prevLord)
    {
        SpawnDrop(corpse);
        base.PawnDied(corpse, prevLord);
    }

    private void SpawnDrop(Corpse corpse)
    {
        if (corpse?.MapHeld == null)
        {
            return;
        }

        SpawnThing(corpse, Props.dropThing, Props.dropCount);
        Pawn pawn = corpse.InnerPawn;
        if (pawn?.Faction != null
            && pawn.Faction.HostileTo(Faction.OfPlayer))
        {
            SpawnThing(corpse, Props.enemyDropThing, Props.enemyDropCount);
        }
    }

    private void SpawnThing(Corpse corpse, ThingDef thingDef, int count)
    {
        if (thingDef == null || count <= 0)
        {
            return;
        }

        Thing thing = ThingMaker.MakeThing(thingDef);
        thing.stackCount = count;
        GenPlace.TryPlaceThing(thing, corpse.PositionHeld, corpse.MapHeld, ThingPlaceMode.Near);
    }
}
