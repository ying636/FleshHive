using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class LordToil_FuriousmeldAssault : LordToil
{
    public LordToil_FuriousmeldAssault(Pawn furiousmeld)
    {
        this.furiousmeld = furiousmeld;
    }

    public override void UpdateAllDuties()
    {
        Pawn leader = furiousmeld ?? FindLeader();
        if (leader == null)
        {
            return;
        }

        IntVec3 sapperDestination = leader.mindState.duty?.def == FleshHiveDefOf.FH_FuriousmeldSapper
            ? leader.mindState.duty.focus.Cell
            : IntVec3.Invalid;
        if (!sapperDestination.IsValid)
        {
            sapperDestination = GenAI.RandomRaidDest(leader.PositionHeld, Map);
        }

        foreach (Pawn pawn in lord.ownedPawns)
        {
            pawn.mindState.duty = pawn == leader
                ? new PawnDuty(FleshHiveDefOf.FH_FuriousmeldSapper, sapperDestination)
                : new PawnDuty(FleshHiveDefOf.FH_FuriousmeldEscort, leader, EscortRadius);
        }
    }

    public override void Notify_ReachedDutyLocation(Pawn pawn)
    {
        if (pawn == furiousmeld && pawn.mindState.duty?.def == FleshHiveDefOf.FH_FuriousmeldSapper)
        {
            pawn.mindState.duty.focus = LocalTargetInfo.Invalid;
        }

        UpdateAllDuties();
    }

    private Pawn FindLeader()
    {
        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (pawn.kindDef == FleshHiveDefOf.FH_Furiousmeld)
            {
                furiousmeld = pawn;
                return pawn;
            }
        }

        return null;
    }

    private Pawn furiousmeld;

    private const float EscortRadius = 9f;
}
