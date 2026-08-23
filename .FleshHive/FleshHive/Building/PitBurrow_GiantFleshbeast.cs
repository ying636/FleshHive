using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class PitBurrow_GiantFleshbeast : PitBurrow
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        List<Pawn> pawns = emergingFleshbeasts?.ToList();
        Pawn leader = pawns?.FirstOrDefault();
        assaultColony = false;
        base.SpawnSetup(map, respawningAfterLoad);

        if (!respawningAfterLoad && leader != null && pawns != null)
        {
            LordMaker.MakeNewLord(
                Faction.OfEntities,
                new LordJob_GiantFleshbeastAssault(leader),
                map,
                pawns);
        }
    }
}
