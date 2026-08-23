using HiveCreatureFramework;
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
        if (Props.dropThing == null || Props.dropCount <= 0 || corpse.MapHeld == null)
        {
            return;
        }

        Thing thing = ThingMaker.MakeThing(Props.dropThing);
        thing.stackCount = Props.dropCount;
        GenPlace.TryPlaceThing(thing, corpse.PositionHeld, corpse.MapHeld, ThingPlaceMode.Near);
    }
}
