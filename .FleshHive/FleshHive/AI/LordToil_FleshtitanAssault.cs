using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class LordToil_FleshtitanAssault : LordToil
{
    public LordToil_FleshtitanAssault(Pawn fleshtitan)
    {
        this.fleshtitan = fleshtitan;
    }

    public override void UpdateAllDuties()
    {
        Pawn leader = fleshtitan ?? FindLeader();
        if (leader == null)
        {
            return;
        }

        IntVec3 sapperDestination = leader.mindState.duty?.def == FleshHiveDefOf.FH_FleshtitanSapper
            ? leader.mindState.duty.focus.Cell
            : IntVec3.Invalid;
        if (!sapperDestination.IsValid)
        {
            sapperDestination = GenAI.RandomRaidDest(leader.PositionHeld, Map);
        }

        foreach (Pawn pawn in lord.ownedPawns)
        {
            pawn.mindState.duty = pawn == leader
                ? new PawnDuty(FleshHiveDefOf.FH_FleshtitanSapper, sapperDestination)
                : new PawnDuty(FleshHiveDefOf.FH_FuriousmeldEscort, leader, EscortRadius);
        }
    }

    public override void Notify_ReachedDutyLocation(Pawn pawn)
    {
        if (pawn == fleshtitan && pawn.mindState.duty?.def == FleshHiveDefOf.FH_FleshtitanSapper)
        {
            pawn.mindState.duty.focus = LocalTargetInfo.Invalid;
        }

        UpdateAllDuties();
    }

    private Pawn FindLeader()
    {
        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (pawn.kindDef == FleshHiveDefOf.FH_Fleshtitan)
            {
                fleshtitan = pawn;
                return pawn;
            }
        }

        return null!;
    }

    private const float EscortRadius = 9f;

    private Pawn fleshtitan = null!;
}
