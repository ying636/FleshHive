using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FleshHive;

public class LordToil_GiantFleshbeastAssault : LordToil
{
    public LordToil_GiantFleshbeastAssault(Pawn leader)
    {
        this.leader = leader;
    }

    public override void UpdateAllDuties()
    {
        bool leaderAvailable = leader != null
            && !leader.Dead
            && !leader.Destroyed
            && lord.ownedPawns.Contains(leader);
        foreach (Pawn pawn in lord.ownedPawns)
        {
            pawn.mindState.duty = leaderAvailable && pawn != leader
                ? new PawnDuty(FleshHiveDefOf.FH_FuriousmeldEscort, leader, EscortRadius)
                : new PawnDuty(DutyDefOf.FleshbeastAssault);
        }
    }

    public override void Notify_PawnLost(Pawn victim, PawnLostCondition cond)
    {
        if (victim == leader)
        {
            UpdateAllDuties();
        }
    }

    private Pawn leader;

    private const float EscortRadius = 9f;
}
