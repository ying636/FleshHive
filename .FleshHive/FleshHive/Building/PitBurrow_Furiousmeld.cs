using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FleshHive;

public class PitBurrow_Furiousmeld : PitBurrow
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        List<Pawn> pawns = emergingFleshbeasts?.ToList();
        Pawn leader = pawns?.FirstOrDefault(pawn => pawn.kindDef == FleshHiveDefOf.FH_Furiousmeld);
        assaultColony = false;
        base.SpawnSetup(map, respawningAfterLoad);

        if (!respawningAfterLoad && pawns != null)
        {
            foreach (Pawn pawn in pawns)
            {
                FleshParasiteUtility.TryApplyDefaultParasites(pawn);
            }
        }

        if (!respawningAfterLoad && leader != null && pawns != null)
        {
            LordMaker.MakeNewLord(Faction.OfEntities, new LordJob_FuriousmeldAssault(leader), map, pawns);
        }
    }
}
