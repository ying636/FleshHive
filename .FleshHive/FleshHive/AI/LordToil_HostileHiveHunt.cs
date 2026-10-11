using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class LordToil_HostileHiveHunt : LordToil
{
    public override void UpdateAllDuties()
    {
        foreach (Pawn pawn in lord.ownedPawns)
            pawn.mindState.duty = new PawnDuty(HostileFleshHiveDefOf.FH_HostileHiveHunt);
    }
}
